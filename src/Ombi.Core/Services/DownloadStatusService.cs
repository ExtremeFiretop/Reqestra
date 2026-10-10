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
                ResetDownloadStatus(request);
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

            var regularQueue = new Dictionary<int, List<RadarrQueueRecord>>();
            if (regularCandidates.Count > 0)
            {
                try
                {
                    regularQueue = await GetRadarrQueueByTmdbId(await _radarrSettings.GetSettingsAsync(), "Radarr");
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not load Radarr settings while calculating request download status");
                }
            }

            var fourKQueue = new Dictionary<int, List<RadarrQueueRecord>>();
            if (fourKCandidates.Count > 0)
            {
                try
                {
                    fourKQueue = await GetRadarrQueueByTmdbId(await _radarr4KSettings.GetSettingsAsync(), "Radarr 4K");
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not load Radarr 4K settings while calculating request download status");
                }
            }

            foreach (var request in requestList)
            {
                var matches = new List<RadarrQueueRecord>();

                if (IsRegularMovieDownloadCandidate(request) &&
                    regularQueue.TryGetValue(request.TheMovieDbId, out var regularMatches))
                {
                    matches.AddRange(regularMatches);
                }

                if (IsFourKMovieDownloadCandidate(request) &&
                    fourKQueue.TryGetValue(request.TheMovieDbId, out var fourKMatches))
                {
                    matches.AddRange(fourKMatches);
                }

                if (matches.Count == 0)
                {
                    continue;
                }

                request.Downloading = true;
                ApplyDownloadMetrics(
                    matches.Select(x => new QueueMetrics(
                        x.Size,
                        x.Sizeleft,
                        GetRemainingTime(x.Timeleft, x.EstimatedCompletionTime))),
                    out var progress,
                    out var etaMinutes);
                request.DownloadProgress = progress;
                request.DownloadEtaMinutes = etaMinutes;
            }
        }

        public async Task PopulateTvDownloadStatus(IEnumerable<ChildRequests> requests)
        {
            var requestList = requests?.Where(x => x != null).ToList() ?? new List<ChildRequests>();
            foreach (var request in requestList)
            {
                ResetDownloadStatus(request);
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

            var queueByTmdbId = queue
                .Where(x => (x.Series?.tmdbId ?? 0) > 0)
                .GroupBy(x => x.Series.tmdbId)
                .ToDictionary(x => x.Key, x => x.ToList());

            foreach (var request in candidates)
            {
                var tmdbId = request.ParentRequest.ExternalProviderId;
                if (!queueByTmdbId.TryGetValue(tmdbId, out var seriesQueue))
                {
                    continue;
                }

                var requestedEpisodes = request.SeasonRequests?
                    .Where(x => x != null)
                    .SelectMany(season => season.Episodes?
                        .Where(episode => episode != null && episode.Requested && episode.Approved && !episode.Available)
                        .Select(episode => (SeasonNumber: season.SeasonNumber, EpisodeNumber: episode.EpisodeNumber))
                        ?? Enumerable.Empty<(int SeasonNumber, int EpisodeNumber)>())
                    .ToHashSet() ?? new HashSet<(int SeasonNumber, int EpisodeNumber)>();

                if (requestedEpisodes.Count == 0)
                {
                    continue;
                }

                var matchingQueueItems = seriesQueue
                    .Where(item => IsMatchingTvQueueItem(item, requestedEpisodes))
                    .ToList();

                if (matchingQueueItems.Count == 0)
                {
                    continue;
                }

                request.Downloading = true;

                // Sonarr can expose one row per episode for a season pack while every row points
                // to the same underlying download. Deduplicate by DownloadId before aggregating so
                // a single pack is not counted repeatedly in the size-weighted progress value.
                var seenDownloadIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var metrics = new List<QueueMetrics>();
                foreach (var item in matchingQueueItems)
                {
                    if (!string.IsNullOrWhiteSpace(item.DownloadId) && !seenDownloadIds.Add(item.DownloadId))
                    {
                        continue;
                    }

                    metrics.Add(new QueueMetrics(
                        item.Size,
                        item.Sizeleft,
                        GetRemainingTime(item.Timeleft, item.EstimatedCompletionTime)));
                }

                ApplyDownloadMetrics(metrics, out var progress, out var etaMinutes);
                request.DownloadProgress = progress;
                request.DownloadEtaMinutes = etaMinutes;
            }
        }

        private static bool IsMatchingTvQueueItem(
            SonarrQueueRecord item,
            HashSet<(int SeasonNumber, int EpisodeNumber)> requestedEpisodes)
        {
            if (item?.Episode != null && item.Episode.episodeNumber > 0)
            {
                return requestedEpisodes.Contains((item.Episode.seasonNumber, item.Episode.episodeNumber));
            }

            return item?.SeasonNumber.HasValue == true &&
                   requestedEpisodes.Any(x => x.SeasonNumber == item.SeasonNumber.Value);
        }

        private static void ResetDownloadStatus(MovieRequests request)
        {
            request.Downloading = false;
            request.DownloadProgress = null;
            request.DownloadEtaMinutes = null;
        }

        private static void ResetDownloadStatus(ChildRequests request)
        {
            request.Downloading = false;
            request.DownloadProgress = null;
            request.DownloadEtaMinutes = null;
        }

        private static void ApplyDownloadMetrics(
            IEnumerable<QueueMetrics> metrics,
            out int? progress,
            out int? etaMinutes)
        {
            var metricList = metrics?.ToList() ?? new List<QueueMetrics>();

            decimal totalSize = 0;
            decimal totalRemaining = 0;
            foreach (var metric in metricList.Where(x => x.Size > 0))
            {
                var remaining = Math.Max(0m, Math.Min(metric.Size, metric.SizeLeft));
                totalSize += metric.Size;
                totalRemaining += remaining;
            }

            progress = null;
            if (totalSize > 0)
            {
                var rawProgress = 100m * (totalSize - totalRemaining) / totalSize;
                progress = Math.Max(0, Math.Min(100,
                    (int)Math.Round(rawProgress, MidpointRounding.AwayFromZero)));
            }

            var longestRemaining = metricList
                .Where(x => x.TimeLeft.HasValue && x.TimeLeft.Value > TimeSpan.Zero)
                .Select(x => x.TimeLeft.Value)
                .DefaultIfEmpty()
                .Max();

            etaMinutes = longestRemaining > TimeSpan.Zero
                ? Math.Max(1, (int)Math.Ceiling(longestRemaining.TotalMinutes))
                : null;
        }

        private static TimeSpan? GetRemainingTime(TimeSpan? timeLeft, DateTime? estimatedCompletionTime)
        {
            if (timeLeft.HasValue && timeLeft.Value > TimeSpan.Zero)
            {
                return timeLeft;
            }

            if (!estimatedCompletionTime.HasValue)
            {
                return null;
            }

            var remaining = estimatedCompletionTime.Value.ToUniversalTime() - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : null;
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

        private async Task<Dictionary<int, List<RadarrQueueRecord>>> GetRadarrQueueByTmdbId(RadarrSettings settings, string sourceName)
        {
            var recordsByTmdbId = new Dictionary<int, List<RadarrQueueRecord>>();
            if (settings == null || !settings.Enabled)
            {
                return recordsByTmdbId;
            }

            try
            {
                var page = 1;
                while (true)
                {
                    using var timeout = new CancellationTokenSource(QueueRequestTimeout);
                    var result = await _radarrApi.GetQueue(settings.ApiKey, settings.FullUri, page, QueuePageSize, timeout.Token);
                    var records = result?.Records ?? new List<RadarrQueueRecord>();
                    foreach (var record in records.Where(x => x != null))
                    {
                        var tmdbId = record.Movie?.tmdbId ?? 0;
                        if (tmdbId <= 0)
                        {
                            continue;
                        }

                        if (!recordsByTmdbId.TryGetValue(tmdbId, out var matches))
                        {
                            matches = new List<RadarrQueueRecord>();
                            recordsByTmdbId[tmdbId] = matches;
                        }

                        matches.Add(record);
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

            return recordsByTmdbId;
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

        private sealed class QueueMetrics
        {
            public QueueMetrics(decimal size, decimal sizeLeft, TimeSpan? timeLeft)
            {
                Size = size;
                SizeLeft = sizeLeft;
                TimeLeft = timeLeft;
            }

            public decimal Size { get; }
            public decimal SizeLeft { get; }
            public TimeSpan? TimeLeft { get; }
        }
    }
}
