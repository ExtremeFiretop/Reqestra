using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ombi.Api.External.ExternalApis.TheMovieDb;
using Ombi.Api.External.ExternalApis.TheMovieDb.Models;
using Ombi.Core.Models.Requests;
using Ombi.Store.Entities;
using Ombi.Store.Entities.Requests;
using Ombi.Store.Repository.Requests;
using System.Threading;
using Ombi.Helpers;

namespace Ombi.Core.Helpers
{
    public class TvShowRequestBuilderV2
    {

        public TvShowRequestBuilderV2(IMovieDbApi movApi)
        {
            MovieDbApi = movApi;
        }

        private IMovieDbApi MovieDbApi { get; }

        public ChildRequests ChildRequest { get; set; }
        public List<SeasonsViewModel> TvRequests { get; protected set; }
        public string PosterPath { get; protected set; }
        public string BackdropPath { get; protected set; }
        public DateTime FirstAir { get; protected set; }
        public TvRequests NewRequest { get; protected set; }
        protected TvInfo TheMovieDbRecord { get; set; }

        public async Task<TvShowRequestBuilderV2> GetShowInfo(int id, string langCode = "en")
        {
            TheMovieDbRecord = await MovieDbApi.GetTVInfo(id.ToString(), langCode);
            if (TheMovieDbRecord == null)
            {
                // The API layer returns null/default for transient TMDB failures (for example a
                // 500 response). Let the request engine report a normal request error instead of
                // throwing a NullReferenceException and aborting callers such as watchlist import.
                return null;
            }

            // Sonarr requires a TVDB ID. TMDB's appended external_ids payload can occasionally
            // be absent/incomplete even though the dedicated external_ids endpoint has the mapping.
            // Refresh it before persisting the Ombi request so new requests do not enter the retry
            // queue with TvDbId = 0.
            if (!int.TryParse(TheMovieDbRecord.ExternalIds?.TvDbId, out var tvdbId) || tvdbId <= 0)
            {
                var externalIds = await MovieDbApi.GetTvExternals(id);
                if (externalIds != null)
                {
                    TheMovieDbRecord.ExternalIds ??= new ExternalIds();

                    if (externalIds.tvdb_id > 0)
                    {
                        TheMovieDbRecord.ExternalIds.TvDbId = externalIds.tvdb_id.ToString();
                    }

                    // Keep any stable IMDb identity even when TMDB still has no TVDB mapping.
                    // Duplicate/content rules can use it to recognize provider-id aliases.
                    if (string.IsNullOrEmpty(TheMovieDbRecord.ExternalIds.ImdbId) &&
                        !string.IsNullOrEmpty(externalIds.imdb_id))
                    {
                        TheMovieDbRecord.ExternalIds.ImdbId = externalIds.imdb_id;
                    }
                }
            }

            // Remove 'Specials Season'
            TheMovieDbRecord.seasons ??= new List<Season>();
            var firstSeason = TheMovieDbRecord.seasons.OrderBy(x => x.season_number).FirstOrDefault();
            if (firstSeason?.season_number == 0)
            {
                TheMovieDbRecord.seasons.Remove(firstSeason);
            }

            BackdropPath = TheMovieDbRecord.Images?.Backdrops?.OrderBy(x => x.VoteCount).ThenBy(x => x.VoteAverage).FirstOrDefault()?.FilePath; ;

            DateTime.TryParse(TheMovieDbRecord.first_air_date, out var dt);

            FirstAir = dt;

            // For some reason the poster path is always http
            PosterPath = TheMovieDbRecord.Images?.Posters?.OrderBy(x => x.VoteCount).ThenBy(x => x.VoteAverage).FirstOrDefault()?.FilePath;

            return this;
        }

        public TvShowRequestBuilderV2 CreateChild(TvRequestViewModelV2 model, string userId, RequestSource source)
        {
            var animationGenre = TheMovieDbRecord.genres?.Any(s => s.name.Equals("Animation", StringComparison.InvariantCultureIgnoreCase)) ?? false;
            var animeKeyword = TheMovieDbRecord.Keywords?.KeywordsValue?.Any(s => s.Name.Equals("Anime", StringComparison.InvariantCultureIgnoreCase)) ?? false;
            int.TryParse(TheMovieDbRecord.ExternalIds?.TvDbId, out var tvDbId);
            ChildRequest = new ChildRequests
            {
                RequestType = RequestType.TvShow,
                RequestTheMovieDbId = model.TheMovieDbId,
                RequestTvDbId = tvDbId,
                RequestImdbId = TheMovieDbRecord.ExternalIds?.ImdbId ?? string.Empty,
                RequestedDate = DateTime.UtcNow,
                Approved = false,
                RequestedUserId = userId,
                SeasonRequests = new List<SeasonRequests>(),
                Title = TheMovieDbRecord.name,
                ReleaseYear = FirstAir,
                RequestedByAlias = model.RequestedByAlias,
                SeriesType = animationGenre && animeKeyword ? SeriesType.Anime : SeriesType.Standard,
                Source = source
            };

            return this;
        }

        public TvShowRequestBuilderV2 CreateTvList(TvRequestViewModelV2 tv)
        {
            TvRequests = new List<SeasonsViewModel>();
            // Only have the TV requests we actually requested and not everything
            foreach (var season in tv.Seasons)
            {
                if (season.Episodes.Any())
                {
                    TvRequests.Add(season);
                }
            }

            return this;
        }


        public async Task<TvShowRequestBuilderV2> BuildEpisodes(TvRequestViewModelV2 tv)
        {
            var allEpisodes = new List<Episode>();
            var seasonNumbers = GetSeasonNumbersToLoad(tv);

            foreach (var seasonNumber in seasonNumbers)
            {
                var seasonEpisodes = await MovieDbApi.GetSeasonEpisodes(TheMovieDbRecord.id, seasonNumber, CancellationToken.None);
                if (seasonEpisodes?.episodes == null)
                {
                    continue;
                }

                allEpisodes.AddRange(seasonEpisodes.episodes);
            }

            if (!allEpisodes.Any())
            {
                return this;
            }

            if (tv.RequestAll)
            {
                foreach (var ep in allEpisodes)
                {
                    var season = ChildRequest.SeasonRequests.FirstOrDefault(x => x.SeasonNumber == ep.season_number);
                    if (season == null)
                    {
                        ChildRequest.SeasonRequests.Add(new SeasonRequests
                        {
                            Episodes = new List<EpisodeRequests>{
                                new EpisodeRequests
                                {
                                    EpisodeNumber = ep.episode_number,
                                    AirDate = FormatDate(ep.air_date),
                                    Title = ep.name,
                                }
                            },
                            SeasonNumber = ep.season_number,
                        });
                    }
                    else
                    {
                        season.Episodes.Add(new EpisodeRequests
                        {
                            EpisodeNumber = ep.episode_number,
                            AirDate = FormatDate(ep.air_date),
                            Title = ep.name,
                        });
                    }
                }

            }
            else if (tv.LatestSeason)
            {
                var latest = allEpisodes.OrderByDescending(x => x.season_number).FirstOrDefault();
                var episodesRequests = new List<EpisodeRequests>();
                foreach (var ep in allEpisodes)
                {
                    if (ep.season_number == latest.season_number)
                    {
                        episodesRequests.Add(new EpisodeRequests
                        {
                            EpisodeNumber = ep.episode_number,
                            AirDate = FormatDate(ep.air_date),
                            Title = ep.name,
                        });
                    }
                }
                ChildRequest.SeasonRequests.Add(new SeasonRequests
                {
                    Episodes = episodesRequests,
                    SeasonNumber = latest.season_number,
                });
            }
            else if (tv.FirstSeason)
            {
                var first = allEpisodes.OrderBy(x => x.season_number).FirstOrDefault();
                if (first.season_number == 0)
                {
                    first = allEpisodes.OrderBy(x => x.season_number).Skip(1).FirstOrDefault();
                }
                var episodesRequests = new List<EpisodeRequests>();
                foreach (var ep in allEpisodes)
                {
                    if (ep.season_number == first.season_number)
                    {
                        episodesRequests.Add(new EpisodeRequests
                        {
                            EpisodeNumber = ep.episode_number,
                            AirDate = FormatDate(ep.air_date),
                            Title = ep.name,
                        });
                    }
                }
                ChildRequest.SeasonRequests.Add(new SeasonRequests
                {
                    Episodes = episodesRequests,
                    SeasonNumber = first.season_number,
                });
            }
            else
            {
                // It's a custom request
                var seasonRequests = new List<SeasonRequests>();
                foreach (var ep in allEpisodes)
                {
                    var existingSeasonRequest = seasonRequests.FirstOrDefault(x => x.SeasonNumber == ep.season_number);
                    if (existingSeasonRequest != null)
                    {
                        var requestedSeason = tv.Seasons.FirstOrDefault(x => x.SeasonNumber == ep.season_number);
                        var requestedEpisode = requestedSeason?.Episodes?.Any(x => x.EpisodeNumber == ep.episode_number) ?? false;
                        if (requestedSeason != null && requestedEpisode)
                        {
                            // We already have this, let's just add the episodes to it
                            existingSeasonRequest.Episodes.Add(new EpisodeRequests
                            {
                                EpisodeNumber = ep.episode_number,
                                AirDate = FormatDate(ep.air_date),
                                Title = ep.name,
                            });
                        }
                    }
                    else
                    {
                        var newRequest = new SeasonRequests { SeasonNumber = ep.season_number };
                        var requestedSeason = tv.Seasons.FirstOrDefault(x => x.SeasonNumber == ep.season_number);
                        var requestedEpisode = requestedSeason?.Episodes?.Any(x => x.EpisodeNumber == ep.episode_number) ?? false;
                        if (requestedSeason != null && requestedEpisode)
                        {
                            newRequest.Episodes.Add(new EpisodeRequests
                            {
                                EpisodeNumber = ep.episode_number,
                                AirDate = FormatDate(ep.air_date),
                                Title = ep.name,
                            });
                            seasonRequests.Add(newRequest);
                        }
                    }
                }

                foreach (var s in seasonRequests)
                {
                    ChildRequest.SeasonRequests.Add(s);
                }
            }
            return this;
        }


        private IReadOnlyCollection<int> GetSeasonNumbersToLoad(TvRequestViewModelV2 tv)
        {
            var availableSeasons = TheMovieDbRecord.seasons ?? new List<Season>();

            if (tv.RequestAll)
            {
                return availableSeasons
                    .Select(x => x.season_number)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToArray();
            }

            if (tv.LatestSeason)
            {
                var latestSeason = availableSeasons
                    .Where(x => x.season_number > 0)
                    .OrderByDescending(x => x.season_number)
                    .FirstOrDefault();

                return latestSeason == null
                    ? Array.Empty<int>()
                    : new[] { latestSeason.season_number };
            }

            if (tv.FirstSeason)
            {
                var firstSeason = availableSeasons
                    .Where(x => x.season_number > 0)
                    .OrderBy(x => x.season_number)
                    .FirstOrDefault();

                return firstSeason == null
                    ? Array.Empty<int>()
                    : new[] { firstSeason.season_number };
            }

            return (tv.Seasons ?? new List<SeasonsViewModel>())
                .Where(x => x.Episodes?.Any() == true)
                .Select(x => x.SeasonNumber)
                .Distinct()
                .OrderBy(x => x)
                .ToArray();
        }

        public TvShowRequestBuilderV2 CreateNewRequest(TvRequestViewModelV2 tv, int rootPathOverride, int qualityOverride, int langProfile)
        {
            int.TryParse(TheMovieDbRecord.ExternalIds?.TvDbId, out var tvdbId);
            NewRequest = new TvRequests
            {
                Overview = TheMovieDbRecord.overview,
                PosterPath = PosterPath,
                Title = TheMovieDbRecord.name,
                ReleaseDate = FirstAir,
                ExternalProviderId = TheMovieDbRecord.id,
                Status = TheMovieDbRecord.status,
                ImdbId = TheMovieDbRecord.ExternalIds?.ImdbId ?? string.Empty,
                TvDbId = tvdbId,
                ChildRequests = new List<ChildRequests>(),
                TotalSeasons = tv.Seasons.Count(),
                Background = BackdropPath,
                RootFolder = rootPathOverride,
                QualityOverride = qualityOverride,
                LanguageProfile = langProfile
            };
            NewRequest.ChildRequests.Add(ChildRequest);

            return this;
        }

        private static DateTime FormatDate(string date)
        {
            if (date.HasValue())
            {
                if (DateTime.TryParse(date, out var d))
                {
                    return d;
                }
            }
            return DateTime.MinValue;
        }
    }
}