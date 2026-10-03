using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ombi.Core.Models.Search;
using Ombi.Core.Rule.Interfaces;
using Ombi.Core.Services;
using Ombi.Core.Settings;
using Ombi.Helpers;
using Ombi.Settings.Settings.Models;
using Ombi.Settings.Settings.Models.External;
using Ombi.Store.Entities;

namespace Ombi.Core.Rule.Rules.Search
{
    public abstract class MediaServerAvailabilityRule : BaseSearchRule, IRules<SearchViewModel>
    {
        private readonly IFeatureService _featureService;
        private readonly ISettingsService<RadarrSettings> _radarrSettings;
        private readonly ISettingsService<SonarrSettings> _sonarrSettings;

        protected ILogger Log { get; }
        private bool? _deferToRadarr;
        private bool? _deferToSonarr;

        protected MediaServerAvailabilityRule(
            ILogger log,
            IFeatureService featureService,
            ISettingsService<RadarrSettings> radarrSettings,
            ISettingsService<SonarrSettings> sonarrSettings)
        {
            Log = log;
            _featureService = featureService;
            _radarrSettings = radarrSettings;
            _sonarrSettings = sonarrSettings;
        }

        /// <summary>
        /// Looks up content in the media server by provider IDs.
        /// Returns the content item and which provider ID types matched.
        /// </summary>
        protected abstract Task<ContentLookupResult> FindContent(SearchViewModel obj);

        /// <summary>
        /// Gets all episodes from the media server repository.
        /// </summary>
        protected abstract IQueryable<IMediaServerEpisode> GetAllEpisodes();

        /// <summary>
        /// Sets the media server URL on the search view model (PlexUrl, EmbyUrl, or JellyfinUrl).
        /// </summary>
        protected abstract Task SetMediaServerUrl(SearchViewModel obj, string url);

        public async Task<RuleResult> Execute(SearchViewModel obj)
        {
            var lookup = await FindContent(obj);
            if (lookup?.Content == null)
            {
                return Success();
            }

            var item = lookup.Content;

            await SetMediaServerUrl(obj, item.Url);

            if (obj is SearchMovieViewModel movie)
            {
                await SetMovieAvailability(movie, item);
            }
            else
            {
                await SetTvShowAvailability(obj);
            }

            if (obj is SearchTvShowViewModel search)
            {
                await CheckEpisodeAvailability(search, lookup, item);
            }

            return Success();
        }

        private async Task SetMovieAvailability(SearchMovieViewModel movie, IMediaServerContent item)
        {
            if (await ShouldDeferToRadarr())
            {
                return;
            }

            var is4kEnabled = await _featureService.FeatureEnabled(FeatureNames.Movie4KRequests);

            if (item.Has4K && is4kEnabled)
            {
                movie.Available4K = true;
            }

            if (item.Quality.HasValue() || !item.Has4K || !is4kEnabled)
            {
                movie.Available = true;
                movie.Quality = item.Quality;
            }
        }

        private async Task SetTvShowAvailability(SearchViewModel obj)
        {
            if (!await ShouldDeferToSonarr())
            {
                obj.Available = true;
            }
        }

        private async Task CheckEpisodeAvailability(SearchTvShowViewModel search, ContentLookupResult lookup, IMediaServerContent item)
        {
            if (await ShouldDeferToSonarr())
            {
                return;
            }

            if (!search.SeasonRequests.Any())
            {
                return;
            }

            try
            {
                // FindContent has already resolved one specific media-server content row.
                // Scope episode availability to that exact row rather than re-querying by the
                // provider ID that happened to find it. Provider IDs are not guaranteed to be
                // unique in a stale or migrated media-server cache, and querying by them can
                // merge episodes from multiple series into one availability result.
                var matchingEpisodes = GetAllEpisodes().Where(x => x.Series.Id == item.Id);

                var availableEpisodes = await matchingEpisodes
                    .Select(x => new { x.SeasonNumber, x.EpisodeNumber })
                    .ToListAsync();
                var availableEpisodeKeys = availableEpisodes
                    .Select(x => (x.SeasonNumber, x.EpisodeNumber))
                    .ToHashSet();

                foreach (var season in search.SeasonRequests)
                {
                    var mediaServerSeasonNumber = lookup.SeasonNumberMap.TryGetValue(season.SeasonNumber, out var mappedSeasonNumber)
                        ? mappedSeasonNumber
                        : season.SeasonNumber;

                    foreach (var episode in season.Episodes)
                    {
                        if (availableEpisodeKeys.Contains((mediaServerSeasonNumber, episode.EpisodeNumber)))
                        {
                            episode.Available = true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError(e, "Exception thrown when attempting to check if something is available");
            }

            AvailabilityRuleHelper.CheckForUnairedEpisodes(search);
        }

        private async Task<bool> ShouldDeferToRadarr()
        {
            if (_deferToRadarr.HasValue)
            {
                return _deferToRadarr.Value;
            }

            if (_radarrSettings == null)
            {
                _deferToRadarr = false;
                return false;
            }

            var settings = await _radarrSettings.GetSettingsAsync();
            _deferToRadarr = settings != null && settings.Enabled &&
                   settings.ScanForAvailability && settings.PrioritizeArrAvailability;
            return _deferToRadarr.Value;
        }

        private async Task<bool> ShouldDeferToSonarr()
        {
            if (_deferToSonarr.HasValue)
            {
                return _deferToSonarr.Value;
            }

            if (_sonarrSettings == null)
            {
                _deferToSonarr = false;
                return false;
            }

            var settings = await _sonarrSettings.GetSettingsAsync();
            _deferToSonarr = settings != null && settings.Enabled &&
                   settings.ScanForAvailability && settings.PrioritizeArrAvailability;
            return _deferToSonarr.Value;
        }

        /// <summary>
        /// Shared content lookup by IMDB, TheMovieDb, and TvDb IDs.
        /// Used by media servers that expose separate GetByImdbId/GetByTheMovieDbId/GetByTvDbId methods.
        /// </summary>
        protected static async Task<ContentLookupResult> FindContentByProviderIds(
            SearchViewModel obj,
            Func<string, Task<IMediaServerContent>> getByImdbId,
            Func<string, Task<IMediaServerContent>> getByTheMovieDbId,
            Func<string, Task<IMediaServerContent>> getByTvDbId,
            bool lookupById = false)
        {
            var result = new ContentLookupResult();
            IMediaServerContent item = null;

            // TheMovieDb (and potentially other providers) use separate ID namespaces for
            // movies and TV shows, so the same ID can refer to both a movie and a series.
            // Only accept content whose type matches the thing we are searching for, otherwise
            // a movie can be wrongly marked as available because a series shares its ID (and vice versa).
            var expectedType = obj is SearchMovieViewModel ? MediaType.Movie : MediaType.Series;
            bool Matches(IMediaServerContent content) => content != null && content.Type == expectedType;

            if (obj.ImdbId.HasValue())
            {
                var match = await getByImdbId(obj.ImdbId);
                if (Matches(match))
                {
                    item = match;
                    result.UseImdb = true;
                }
            }

            if (item == null)
            {
                if (lookupById && obj.Id > 0)
                {
                    var match = await getByTheMovieDbId(obj.Id.ToString());
                    if (Matches(match))
                    {
                        item = match;
                        obj.TheMovieDbId = obj.Id.ToString();
                        result.UseTheMovieDb = true;
                    }
                }

                if (item == null && obj.TheMovieDbId.HasValue())
                {
                    var match = await getByTheMovieDbId(obj.TheMovieDbId);
                    if (Matches(match))
                    {
                        item = match;
                        result.UseTheMovieDb = true;
                    }
                }

                if (item == null && obj.TheTvDbId.HasValue())
                {
                    var match = await getByTvDbId(obj.TheTvDbId);
                    if (Matches(match))
                    {
                        item = match;
                        result.UseTvDb = true;
                    }
                }
            }

            result.Content = item;
            return result;
        }
    }

    /// <summary>
    /// Result of looking up content in a media server, including which provider IDs were used for matching.
    /// </summary>
    public class ContentLookupResult
    {
        public IMediaServerContent Content { get; set; }
        public bool UseImdb { get; set; }
        public bool UseTheMovieDb { get; set; }
        public bool UseTvDb { get; set; }
        public bool UseContentId { get; set; }
        public Dictionary<int, int> SeasonNumberMap { get; } = new Dictionary<int, int>();
    }
}
