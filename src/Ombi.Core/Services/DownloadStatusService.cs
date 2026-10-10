using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ombi.Api.External.ExternalApis.Radarr;
using Ombi.Api.External.ExternalApis.Radarr.Models.V3;
using Ombi.Api.External.ExternalApis.Sonarr;
using Ombi.Api.External.ExternalApis.Sonarr.Models.V3;
using Ombi.Core.Settings;
using Ombi.Settings.Settings.Models.External;
using Ombi.Store.Entities.Requests;

namespace Ombi.Core.Services
{
    public class DownloadStatusService : IDownloadStatusService
    {
        private const int QueuePageSize = 1000;
        private static readonly TimeSpan QueueRequestTimeout = TimeSpan.FromSeconds(5);

        private readonly ISettingsService<RadarrSettings> _radarrSettings;
        private readonly ISettingsService<Radarr4KSettings> _radarr4KSettings;
        private readonly ISettingsService<SonarrSettings> _sonarrSettings;
        private readonly IRadarrV3Api _radarrApi;
        private readonly ISonarrV3Api _sonarrApi;
        private readonly ILogger<DownloadStatusService> _logger;

        public DownloadStatusService(
            ISettingsService<RadarrSettings> radarrSettings,
            ISettingsService<Radarr4KSettings> radarr4KSettings,
            ISettingsService<SonarrSettings> sonarrSettings,
            IRadarrV3Api radarrApi,
            ISonarrV3Api sonarrApi,
            ILogger<DownloadStatusService> logger)
        {
            _radarrSettings = radarrSettings;
            _radarr4KSettings = radarr4KSettings;
            _sonarrSettings = sonarrSettings;
            _radarrApi = radarrApi;
            _sonarrApi = sonarrApi;
            _logger = logger;
        }

        public async Task PopulateMovieDownloadStatus(IEnumerable<MovieRequests> requests)
        {
            var requestList = requests?.Where(x => x != null).ToList() ?? new List<MovieRequests>();
            foreach (var request in requestList)
            {
                request.Downloading = false;
            }

            var regularCandidates = requestList
                .Where(IsRegularMovieDownloadCandidate)
                .ToList();
            var fourKCandidates = requestList
                .Where(IsFourKMovieDownloadCandidate)
                .ToList();

            if (regularCandidates.Count == 0 && fourKCandidates.Count == 0)
            {
                return;
            }

            var regularDownloading = new HashSet<int>();
            if (regularCandidates.Count > 0)
            {
                try
                {
                    regularDownloading = await GetRadarrDownloadingTmdbIds(await _radarrSettings.GetSettingsAsync(), "Radarr");
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not load Radarr settings while calculating request download status");
                }
            }

            var fourKDownloading = new HashSet<int>();
            if (fourKCandidates.Count > 0)
            {
                try
                {
                    fourKDownloading = await GetRadarrDownloadingTmdbIds(await _radarr4KSettings.GetSettingsAsync(), "Radarr 4K");
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not load Radarr 4K settings while calculating request download status");
                }
            }

            foreach (var request in requestList)
            {
                request.Downloading =
                    (IsRegularMovieDownloadCandidate(request) && regularDownloading.Contains(request.TheMovieDbId)) ||
                    (IsFourKMovieDownloadCandidate(request) && fourKDownloading.Contains(request.TheMovieDbId));
            }
        }

        public async Task PopulateTvDownloadStatus(IEnumerable<ChildRequests> requests)
        {
            var requestList = requests?.Where(x => x != null).ToList() ?? new List<ChildRequests>();
            foreach (var request in requestList)
            {
                request.Downloading = false;
            }

            var candidates = requestList
                .Where(x => x.Approved && !x.Available && !(x.Denied ?? false) && x.ParentRequest?.ExternalProviderId > 0)
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            SonarrSettings settings;
            try
            {
                settings = await _sonarrSettings.GetSettingsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not load Sonarr settings while calculating request download status");
                return;
            }

            if (settings == null || !settings.Enabled)
            {
                return;
            }

            var queue = await GetSonarrQueue(settings);
            if (queue.Count == 0)
            {
                return;
            }

            var episodeKeys = new HashSet<(int TmdbId, int SeasonNumber, int EpisodeNumber)>();
            var seasonKeys = new HashSet<(int TmdbId, int SeasonNumber)>();

            foreach (var item in queue)
            {
                var tmdbId = item.Series?.tmdbId ?? 0;
                if (tmdbId <= 0)
                {
                    continue;
                }

                if (item.Episode != null && item.Episode.episodeNumber > 0)
                {
                    episodeKeys.Add((tmdbId, item.Episode.seasonNumber, item.Episode.episodeNumber));
                    continue;
                }

                if (item.SeasonNumber.HasValue)
                {
                    seasonKeys.Add((tmdbId, item.SeasonNumber.Value));
                }
            }

            foreach (var request in candidates)
            {
                var tmdbId = request.ParentRequest.ExternalProviderId;
                var requestedEpisodes = request.SeasonRequests?
                    .Where(x => x != null)
                    .SelectMany(season => season.Episodes?
                        .Where(episode => episode != null && episode.Requested && episode.Approved && !episode.Available)
                        .Select(episode => (SeasonNumber: season.SeasonNumber, EpisodeNumber: episode.EpisodeNumber))
                        ?? Enumerable.Empty<(int SeasonNumber, int EpisodeNumber)>())
                    .ToList() ?? new List<(int SeasonNumber, int EpisodeNumber)>();

                request.Downloading = requestedEpisodes.Any(episode =>
                    episodeKeys.Contains((tmdbId, episode.SeasonNumber, episode.EpisodeNumber)) ||
                    seasonKeys.Contains((tmdbId, episode.SeasonNumber)));
            }
        }

        private static bool IsRegularMovieDownloadCandidate(MovieRequests request)
        {
            return request.TheMovieDbId > 0 &&
                   request.RequestedDate != default &&
                   request.Approved &&
                   !request.Available &&
                   !(request.Denied ?? false);
        }

        private static bool IsFourKMovieDownloadCandidate(MovieRequests request)
        {
            return request.TheMovieDbId > 0 &&
                   request.Has4KRequest &&
                   request.Approved4K &&
                   !request.Available4K &&
                   !(request.Denied4K ?? false);
        }

        private async Task<HashSet<int>> GetRadarrDownloadingTmdbIds(RadarrSettings settings, string sourceName)
        {
            var ids = new HashSet<int>();
            if (settings == null || !settings.Enabled)
            {
                return ids;
            }

            try
            {
                var page = 1;
                while (true)
                {
                    using var timeout = new CancellationTokenSource(QueueRequestTimeout);
                    var result = await _radarrApi.GetQueue(settings.ApiKey, settings.FullUri, page, QueuePageSize, timeout.Token);
                    var records = result?.Records ?? new List<RadarrQueueRecord>();
                    foreach (var record in records)
                    {
                        var tmdbId = record?.Movie?.tmdbId ?? 0;
                        if (tmdbId > 0)
                        {
                            ids.Add(tmdbId);
                        }
                    }

                    if (result == null || records.Count == 0 || page * QueuePageSize >= result.TotalRecords)
                    {
                        break;
                    }

                    page++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not read {DownloadSource} queue while calculating request download status", sourceName);
            }

            return ids;
        }

        private async Task<List<SonarrQueueRecord>> GetSonarrQueue(SonarrSettings settings)
        {
            var records = new List<SonarrQueueRecord>();
            try
            {
                var page = 1;
                while (true)
                {
                    using var timeout = new CancellationTokenSource(QueueRequestTimeout);
                    var result = await _sonarrApi.GetQueue(settings.ApiKey, settings.FullUri, page, QueuePageSize, timeout.Token);
                    var pageRecords = result?.Records ?? new List<SonarrQueueRecord>();
                    records.AddRange(pageRecords.Where(x => x != null));

                    if (result == null || pageRecords.Count == 0 || page * QueuePageSize >= result.TotalRecords)
                    {
                        break;
                    }

                    page++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not read Sonarr queue while calculating request download status");
            }

            return records;
        }
    }
}
