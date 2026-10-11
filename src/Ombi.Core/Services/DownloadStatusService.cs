using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ombi.Api.External.ExternalApis.Radarr;
using Ombi.Api.External.ExternalApis.Radarr.Models;
using Ombi.Api.External.ExternalApis.Radarr.Models.V3;
using Ombi.Api.External.ExternalApis.Sonarr;
using Ombi.Api.External.ExternalApis.Sonarr.Models;
using Ombi.Api.External.ExternalApis.Sonarr.Models.V3;
using Ombi.Core.Senders;
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
        private readonly IActiveDownloadEtaEstimator _activeDownloadEtaEstimator;
        private readonly IPostDownloadTransitionTracker _postDownloadTransitionTracker;
        private readonly ILogger<DownloadStatusService> _logger;

        public DownloadStatusService(
            ISettingsService<RadarrSettings> radarrSettings,
            ISettingsService<Radarr4KSettings> radarr4KSettings,
            ISettingsService<SonarrSettings> sonarrSettings,
            IRadarrV3Api radarrApi,
            ISonarrV3Api sonarrApi,
            IActiveDownloadEtaEstimator activeDownloadEtaEstimator,
            IPostDownloadTransitionTracker postDownloadTransitionTracker,
            ILogger<DownloadStatusService> logger)
        {
            _radarrSettings = radarrSettings;
            _radarr4KSettings = radarr4KSettings;
            _sonarrSettings = sonarrSettings;
            _radarrApi = radarrApi;
            _sonarrApi = sonarrApi;
            _activeDownloadEtaEstimator = activeDownloadEtaEstimator;
            _postDownloadTransitionTracker = postDownloadTransitionTracker;
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
            var regularMovies = new Dictionary<int, MovieResponse>();
            if (regularCandidates.Count > 0)
            {
                try
                {
                    var settings = await _radarrSettings.GetSettingsAsync();
                    var queueTask = GetRadarrQueueByTmdbId(settings, "Radarr");
                    var moviesTask = GetRadarrMoviesByTmdbId(settings, "Radarr");
                    await Task.WhenAll(queueTask, moviesTask);
                    regularQueue = await queueTask;
                    regularMovies = await moviesTask;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not load Radarr settings while calculating request download status");
                }
            }

            var fourKQueue = new Dictionary<int, List<RadarrQueueRecord>>();
            var fourKMovies = new Dictionary<int, MovieResponse>();
            if (fourKCandidates.Count > 0)
            {
                try
                {
                    var settings = await _radarr4KSettings.GetSettingsAsync();
                    var queueTask = GetRadarrQueueByTmdbId(settings, "Radarr 4K");
                    var moviesTask = GetRadarrMoviesByTmdbId(settings, "Radarr 4K");
                    await Task.WhenAll(queueTask, moviesTask);
                    fourKQueue = await queueTask;
                    fourKMovies = await moviesTask;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not load Radarr 4K settings while calculating request download status");
                }
            }

            foreach (var request in requestList)
            {
                var metrics = new List<QueueMetrics>();
                List<RadarrQueueRecord> regularMatches = null;
                List<RadarrQueueRecord> fourKMatches = null;

                if (IsRegularMovieDownloadCandidate(request) &&
                    regularQueue.TryGetValue(request.TheMovieDbId, out regularMatches))
                {
                    metrics.AddRange(regularMatches.Select(x => CreateQueueMetrics("Radarr", x)));
                }

                if (IsFourKMovieDownloadCandidate(request) &&
                    fourKQueue.TryGetValue(request.TheMovieDbId, out fourKMatches))
                {
                    metrics.AddRange(fourKMatches.Select(x => CreateQueueMetrics("Radarr 4K", x)));
                }

                if (metrics.Count > 0)
                {
                    ApplyQueueState(request, regularMatches, fourKMatches);
                    ApplyDownloadMetrics(metrics, out var progress, out var etaMinutes);
                    request.DownloadProgress = progress;
                    request.DownloadEtaMinutes = etaMinutes;
                }

                if (request.Downloading || request.Importing || request.Queued)
                {
                    continue;
                }

                ApplyMovieArrState(request, regularMovies, fourKMovies);
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

            var queueTask = GetSonarrQueue(settings);
            var seriesTask = GetSonarrSeries(settings);
            await Task.WhenAll(queueTask, seriesTask);

            var queue = await queueTask;
            var sonarrSeries = await seriesTask;

            var queueByTmdbId = queue
                .Where(x => (x.Series?.tmdbId ?? 0) > 0)
                .GroupBy(x => x.Series.tmdbId)
                .ToDictionary(x => x.Key, x => x.ToList());

            var episodesBySeriesId = new Dictionary<int, List<Episode>>();
            foreach (var request in candidates)
            {
                var requestedEpisodes = GetRequestedTvEpisodes(request);
                if (requestedEpisodes.Count == 0)
                {
                    continue;
                }

                var tmdbId = request.ParentRequest.ExternalProviderId;
                var matchingQueueItems = queueByTmdbId.TryGetValue(tmdbId, out var seriesQueue)
                    ? seriesQueue.Where(item => IsMatchingTvQueueItem(item, requestedEpisodes)).ToList()
                    : new List<SonarrQueueRecord>();

                if (matchingQueueItems.Count > 0)
                {
                    ApplyQueueState(request, matchingQueueItems);
                    ApplyTvEpisodeQueueStates(request, matchingQueueItems);

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

                        metrics.Add(CreateQueueMetrics("Sonarr", item));
                    }

                    ApplyDownloadMetrics(metrics, out var progress, out var etaMinutes);
                    request.DownloadProgress = progress;
                    request.DownloadEtaMinutes = etaMinutes;
                }

                var series = FindMatchingSonarrSeries(request, sonarrSeries);
                if (series == null || series.id <= 0)
                {
                    continue;
                }

                if (!episodesBySeriesId.TryGetValue(series.id, out var sonarrEpisodes))
                {
                    sonarrEpisodes = await GetSonarrEpisodes(series.id, settings);
                    episodesBySeriesId[series.id] = sonarrEpisodes;
                }

                // Episode rows need their own Arr state even while other episodes in this child
                // request are queued, downloading, or importing. Keep the parent precedence
                // unchanged below.
                ApplyTvEpisodeArrState(request, series, sonarrEpisodes);

                if (request.Downloading || request.Importing || request.Queued)
                {
                    continue;
                }

                ApplyTvArrState(request, series, sonarrEpisodes);
            }
        }

        private static HashSet<(int SeasonNumber, int EpisodeNumber)> GetRequestedTvEpisodes(ChildRequests request)
        {
            return request.SeasonRequests?
                .Where(x => x != null)
                .SelectMany(season => season.Episodes?
                    .Where(episode => episode != null && episode.Requested && episode.Approved && !episode.Available)
                    .Select(episode => (SeasonNumber: season.SeasonNumber, EpisodeNumber: episode.EpisodeNumber))
                    ?? Enumerable.Empty<(int SeasonNumber, int EpisodeNumber)>())
                .ToHashSet() ?? new HashSet<(int SeasonNumber, int EpisodeNumber)>();
        }

        private static SonarrSeries FindMatchingSonarrSeries(
            ChildRequests request,
            IEnumerable<SonarrSeries> series)
        {
            var seriesList = series?.Where(x => x != null).ToList() ?? new List<SonarrSeries>();
            var tvdbId = request.ParentRequest?.TvDbId ?? 0;
            if (tvdbId > 0)
            {
                var tvdbMatch = seriesList.FirstOrDefault(x => x.tvdbId == tvdbId);
                if (tvdbMatch != null)
                {
                    return tvdbMatch;
                }
            }

            var tmdbId = request.ParentRequest?.ExternalProviderId ?? 0;
            return tmdbId > 0
                ? seriesList.FirstOrDefault(x => x.tmdbId == tmdbId)
                : null;
        }

        private static void ApplyMovieArrState(
            MovieRequests request,
            IReadOnlyDictionary<int, MovieResponse> regularMovies,
            IReadOnlyDictionary<int, MovieResponse> fourKMovies)
        {
            var expected = new List<MovieResponse>();
            var allTargetsResolved = true;

            if (IsRegularMovieDownloadCandidate(request))
            {
                if (regularMovies != null &&
                    regularMovies.TryGetValue(request.TheMovieDbId, out var regularMovie))
                {
                    expected.Add(regularMovie);
                }
                else
                {
                    allTargetsResolved = false;
                }
            }

            if (IsFourKMovieDownloadCandidate(request))
            {
                if (fourKMovies != null &&
                    fourKMovies.TryGetValue(request.TheMovieDbId, out var fourKMovie))
                {
                    expected.Add(fourKMovie);
                }
                else
                {
                    allTargetsResolved = false;
                }
            }

            if (!allTargetsResolved || expected.Count == 0)
            {
                return;
            }

            if (expected.All(x => x.hasFile))
            {
                request.Completed = true;
                return;
            }

            // A file that already exists is complete even if the user later changes monitoring.
            // Every still-missing target must be monitored before Reqestra can say Arr is searching.
            if (expected.Where(x => !x.hasFile).All(x => x.monitored))
            {
                request.Searching = true;
            }
        }

        private static void ApplyTvEpisodeArrState(
            ChildRequests request,
            SonarrSeries series,
            IReadOnlyCollection<Episode> sonarrEpisodes)
        {
            if (request?.SeasonRequests == null || series == null ||
                sonarrEpisodes == null || sonarrEpisodes.Count == 0)
            {
                return;
            }

            foreach (var requestedSeason in request.SeasonRequests.Where(x => x != null))
            {
                var requestedEpisodes = requestedSeason.Episodes?
                    .Where(x => x != null && x.Requested && x.Approved && !x.Available)
                    .ToList() ?? new List<Ombi.Store.Repository.Requests.EpisodeRequests>();
                if (requestedEpisodes.Count == 0)
                {
                    continue;
                }

                var targetSeasonNumber = requestedSeason.SeasonNumber;
                var fingerprintMatch = SonarrEpisodeFingerprintMatcher.FindSingleSeasonMatch(
                    requestedSeason,
                    sonarrEpisodes);
                if (fingerprintMatch != null)
                {
                    targetSeasonNumber = fingerprintMatch.SonarrSeasonNumber;
                }

                var seasonEpisodes = sonarrEpisodes
                    .Where(x => x != null &&
                                x.seasonNumber == targetSeasonNumber &&
                                x.episodeNumber > 0)
                    .ToList();

                foreach (var requestedEpisode in requestedEpisodes)
                {
                    var sonarrEpisode = seasonEpisodes.FirstOrDefault(
                        x => x.episodeNumber == requestedEpisode.EpisodeNumber);
                    if (sonarrEpisode == null)
                    {
                        continue;
                    }

                    // File presence is useful even when this episode is also in the queue (for
                    // example an upgrade). Queue state has higher RequestStatus precedence.
                    requestedEpisode.Completed = sonarrEpisode.hasFile;

                    if (!sonarrEpisode.hasFile &&
                        !requestedEpisode.Downloading &&
                        !requestedEpisode.Importing &&
                        !requestedEpisode.Queued &&
                        series.monitored &&
                        sonarrEpisode.monitored)
                    {
                        requestedEpisode.Searching = true;
                    }
                }
            }
        }
        private static void ApplyTvArrState(
            ChildRequests request,
            SonarrSeries series,
            IReadOnlyCollection<Episode> sonarrEpisodes)
        {
            if (series == null || sonarrEpisodes == null || sonarrEpisodes.Count == 0)
            {
                return;
            }

            var scopedEpisodes = new List<Episode>();
            foreach (var requestedSeason in request.SeasonRequests ?? new List<Ombi.Store.Repository.Requests.SeasonRequests>())
            {
                if (requestedSeason == null)
                {
                    continue;
                }

                var requested = requestedSeason.Episodes?
                    .Where(x => x != null && x.Requested && x.Approved && !x.Available)
                    .ToList() ?? new List<Ombi.Store.Repository.Requests.EpisodeRequests>();
                if (requested.Count == 0)
                {
                    continue;
                }

                var targetSeasonNumber = requestedSeason.SeasonNumber;
                var fingerprintMatch = SonarrEpisodeFingerprintMatcher.FindSingleSeasonMatch(
                    requestedSeason,
                    sonarrEpisodes);
                if (fingerprintMatch != null)
                {
                    targetSeasonNumber = fingerprintMatch.SonarrSeasonNumber;
                }

                var seasonEpisodes = sonarrEpisodes
                    .Where(x => x != null &&
                                x.seasonNumber == targetSeasonNumber &&
                                x.episodeNumber > 0)
                    .ToList();
                if (seasonEpisodes.Count == 0)
                {
                    return;
                }

                foreach (var requestedEpisode in requested)
                {
                    var sonarrEpisode = seasonEpisodes.FirstOrDefault(
                        x => x.episodeNumber == requestedEpisode.EpisodeNumber);
                    if (sonarrEpisode == null)
                    {
                        return;
                    }

                    // Missing media is only "Searching" when Sonarr is actually monitoring it.
                    // Existing files remain complete even if monitoring is later changed manually.
                    if (!sonarrEpisode.hasFile && !sonarrEpisode.monitored)
                    {
                        return;
                    }

                    scopedEpisodes.Add(sonarrEpisode);
                }

                var sonarrSeason = series.seasons?
                    .FirstOrDefault(x => x.seasonNumber == targetSeasonNumber);

                // A monitored season represents the whole-season/future-release workflow. Preserve
                // that behavior by extending the status scope to episodes that air after the
                // original request. Individual-episode requests leave the season unmonitored and
                // therefore remain scoped only to the explicitly requested episodes above.
                if (sonarrSeason?.monitored == true)
                {
                    var futureEpisodes = seasonEpisodes
                        .Where(x => x.airDateUtc != default &&
                                    x.airDateUtc > request.RequestedDate)
                        .ToList();

                    // If the season is meant to catch future releases, any future missing episode
                    // that Sonarr is not monitoring means the request has not reached a healthy
                    // downstream state and should remain Processing Request.
                    if (futureEpisodes.Any(x => !x.hasFile && !x.monitored))
                    {
                        return;
                    }

                    scopedEpisodes.AddRange(futureEpisodes.Where(x =>
                        scopedEpisodes.All(existing => existing.id != x.id)));
                }
            }

            if (scopedEpisodes.Count == 0)
            {
                return;
            }

            if (scopedEpisodes.All(x => x.hasFile))
            {
                request.Completed = true;
                return;
            }

            if (series.monitored && scopedEpisodes.Where(x => !x.hasFile).All(x => x.monitored))
            {
                request.Searching = true;
            }
        }

        private QueueMetrics CreateQueueMetrics(string source, RadarrQueueRecord item)
        {
            return new QueueMetrics(
                item.Size,
                item.Sizeleft,
                GetRemainingTime(
                    source,
                    item.DownloadId,
                    item.Status,
                    item.Sizeleft,
                    item.Timeleft,
                    item.EstimatedCompletionTime));
        }

        private QueueMetrics CreateQueueMetrics(string source, SonarrQueueRecord item)
        {
            return new QueueMetrics(
                item.Size,
                item.Sizeleft,
                GetRemainingTime(
                    source,
                    item.DownloadId,
                    item.Status,
                    item.Sizeleft,
                    item.Timeleft,
                    item.EstimatedCompletionTime));
        }

        private void ApplyQueueState(
            MovieRequests request,
            IEnumerable<RadarrQueueRecord> regularMatches,
            IEnumerable<RadarrQueueRecord> fourKMatches)
        {
            var states = (regularMatches ?? Enumerable.Empty<RadarrQueueRecord>())
                .Where(x => x != null)
                .Select(x => GetQueueState("Radarr", x))
                .Concat((fourKMatches ?? Enumerable.Empty<RadarrQueueRecord>())
                    .Where(x => x != null)
                    .Select(x => GetQueueState("Radarr 4K", x)))
                .ToList();

            ApplyQueueState(
                states,
                out var downloading,
                out var importing,
                out var queued);
            request.Downloading = downloading;
            request.Importing = importing;
            request.Queued = queued;
        }

        private void ApplyQueueState(
            ChildRequests request,
            IEnumerable<SonarrQueueRecord> matches)
        {
            ApplyQueueState(
                (matches ?? Enumerable.Empty<SonarrQueueRecord>())
                    .Where(x => x != null)
                    .Select(x => GetQueueState("Sonarr", x)),
                out var downloading,
                out var importing,
                out var queued);
            request.Downloading = downloading;
            request.Importing = importing;
            request.Queued = queued;
        }

        private void ApplyTvEpisodeQueueStates(
            ChildRequests request,
            IReadOnlyCollection<SonarrQueueRecord> matches)
        {
            if (request?.SeasonRequests == null || matches == null || matches.Count == 0)
            {
                return;
            }

            foreach (var season in request.SeasonRequests.Where(x => x != null))
            {
                foreach (var episode in season.Episodes?
                             .Where(x => x != null && x.Requested && x.Approved && !x.Available)
                         ?? Enumerable.Empty<Ombi.Store.Repository.Requests.EpisodeRequests>())
                {
                    var episodeMatches = matches
                        .Where(item => IsMatchingTvQueueItem(
                            item,
                            season.SeasonNumber,
                            episode.EpisodeNumber))
                        .ToList();
                    if (episodeMatches.Count == 0)
                    {
                        continue;
                    }

                    ApplyQueueState(
                        episodeMatches.Select(x => GetQueueState("Sonarr", x)),
                        out var downloading,
                        out var importing,
                        out var queued);
                    episode.Downloading = downloading;
                    episode.Importing = importing;
                    episode.Queued = queued;
                }
            }
        }

        private (string Status, string TrackedDownloadState, string TrackedDownloadStatus, bool PostDownloadProcessing) GetQueueState(
            string source,
            RadarrQueueRecord item)
        {
            return (
                item.Status,
                item.TrackedDownloadState,
                item.TrackedDownloadStatus,
                _postDownloadTransitionTracker.ObserveAndIsPostDownloadProcessing(
                    source,
                    item.DownloadId,
                    item.Status,
                    item.TrackedDownloadState,
                    item.TrackedDownloadStatus,
                    item.Size,
                    item.Sizeleft,
                    DateTime.UtcNow));
        }

        private (string Status, string TrackedDownloadState, string TrackedDownloadStatus, bool PostDownloadProcessing) GetQueueState(
            string source,
            SonarrQueueRecord item)
        {
            return (
                item.Status,
                item.TrackedDownloadState,
                item.TrackedDownloadStatus,
                _postDownloadTransitionTracker.ObserveAndIsPostDownloadProcessing(
                    source,
                    item.DownloadId,
                    item.Status,
                    item.TrackedDownloadState,
                    item.TrackedDownloadStatus,
                    item.Size,
                    item.Sizeleft,
                    DateTime.UtcNow));
        }

        private static void ApplyQueueState(
            IEnumerable<(string Status, string TrackedDownloadState, string TrackedDownloadStatus, bool PostDownloadProcessing)> states,
            out bool downloading,
            out bool importing,
            out bool queued)
        {
            var stateList = states?.ToList()
                ?? new List<(string Status, string TrackedDownloadState, string TrackedDownloadStatus, bool PostDownloadProcessing)>();

            downloading = stateList.Any(x => IsActivelyDownloading(x.Status));
            importing = !downloading && stateList.Any(x =>
                IsImporting(x.TrackedDownloadState, x.TrackedDownloadStatus) ||
                x.PostDownloadProcessing);
            queued = !downloading && !importing && stateList.Any(x => IsQueued(x.Status));

            // Preserve the pre-status behavior when an older Arr response does not provide any
            // queue or tracked-download state. Once Arr provides explicit state, only a real
            // downloading, importing, or queued item should receive one of those lifecycle states.
            if (!downloading && !importing && !queued && stateList.Count > 0 &&
                stateList.All(x =>
                    string.IsNullOrWhiteSpace(x.Status) &&
                    string.IsNullOrWhiteSpace(x.TrackedDownloadState)))
            {
                downloading = true;
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

        private static bool IsMatchingTvQueueItem(
            SonarrQueueRecord item,
            int seasonNumber,
            int episodeNumber)
        {
            if (item?.Episode != null && item.Episode.episodeNumber > 0)
            {
                return item.Episode.seasonNumber == seasonNumber &&
                       item.Episode.episodeNumber == episodeNumber;
            }

            return item?.SeasonNumber.HasValue == true &&
                   item.SeasonNumber.Value == seasonNumber;
        }
        private static void ResetDownloadStatus(MovieRequests request)
        {
            request.Downloading = false;
            request.Importing = false;
            request.Queued = false;
            request.Searching = false;
            request.Completed = false;
            request.DownloadProgress = null;
            request.DownloadEtaMinutes = null;
        }

        private static void ResetDownloadStatus(ChildRequests request)
        {
            request.Downloading = false;
            request.Importing = false;
            request.Queued = false;
            request.Searching = false;
            request.Completed = false;
            request.DownloadProgress = null;
            request.DownloadEtaMinutes = null;

            foreach (var episode in request.SeasonRequests?
                         .Where(x => x?.Episodes != null)
                         .SelectMany(x => x.Episodes)
                         .Where(x => x != null)
                     ?? Enumerable.Empty<Ombi.Store.Repository.Requests.EpisodeRequests>())
            {
                episode.Downloading = false;
                episode.Importing = false;
                episode.Queued = false;
                episode.Searching = false;
                episode.Completed = false;
            }
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

            // An active download may not have an observed ETA until a second fresh sample is
            // available. Do not let that missing sample suppress trustworthy cumulative ETAs from
            // queued items in the same request. If no item has a usable ETA yet, the calculation
            // below naturally leaves etaMinutes null.
            var longestRemaining = metricList
                .Where(x => x.TimeLeft.HasValue && x.TimeLeft.Value > TimeSpan.Zero)
                .Select(x => x.TimeLeft.Value)
                .DefaultIfEmpty()
                .Max();

            etaMinutes = longestRemaining > TimeSpan.Zero
                ? Math.Max(1, (int)Math.Ceiling(longestRemaining.TotalMinutes))
                : null;
        }

        private TimeSpan? GetRemainingTime(
            string source,
            string downloadId,
            string status,
            decimal sizeLeft,
            TimeSpan? timeLeft,
            DateTime? estimatedCompletionTime)
        {
            if (IsActivelyDownloading(status) && !string.IsNullOrWhiteSpace(downloadId))
            {
                // Arr's NZBGet integration calculates TimeLeft from physical queue position.
                // That can wildly overstate ETA when NZBGet priority lets a later item download
                // ahead of earlier queued work. Derive the active item's ETA from observed
                // SizeLeft progress instead and do not fall back to the queue-position ETA
                // until a trustworthy sample exists.
                return _activeDownloadEtaEstimator.Observe(
                    source,
                    downloadId,
                    sizeLeft,
                    DateTime.UtcNow);
            }

            if (!string.IsNullOrWhiteSpace(downloadId))
            {
                _activeDownloadEtaEstimator.Forget(source, downloadId);
            }

            return GetArrRemainingTime(timeLeft, estimatedCompletionTime);
        }

        private static TimeSpan? GetArrRemainingTime(TimeSpan? timeLeft, DateTime? estimatedCompletionTime)
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

        private static bool IsActivelyDownloading(string status)
        {
            return string.Equals(status, "downloading", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsImporting(string trackedDownloadState, string trackedDownloadStatus)
        {
            if (string.Equals(trackedDownloadStatus, "error", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return string.Equals(trackedDownloadState, "importPending", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(trackedDownloadState, "importing", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsQueued(string status)
        {
            return string.Equals(status, "queued", StringComparison.OrdinalIgnoreCase);
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

        private async Task<Dictionary<int, MovieResponse>> GetRadarrMoviesByTmdbId(
            RadarrSettings settings,
            string sourceName)
        {
            var moviesByTmdbId = new Dictionary<int, MovieResponse>();
            if (settings == null || !settings.Enabled)
            {
                return moviesByTmdbId;
            }

            try
            {
                var movies = await _radarrApi.GetMovies(settings.ApiKey, settings.FullUri)
                    ?? new List<MovieResponse>();
                foreach (var movie in movies.Where(x => x != null && x.tmdbId > 0))
                {
                    moviesByTmdbId[movie.tmdbId] = movie;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(
                    ex,
                    "Could not read {DownloadSource} movies while calculating request lifecycle status",
                    sourceName);
            }

            return moviesByTmdbId;
        }

        private async Task<List<SonarrSeries>> GetSonarrSeries(SonarrSettings settings)
        {
            try
            {
                return (await _sonarrApi.GetSeries(settings.ApiKey, settings.FullUri))?
                    .Where(x => x != null)
                    .ToList() ?? new List<SonarrSeries>();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(
                    ex,
                    "Could not read Sonarr series while calculating request lifecycle status");
                return new List<SonarrSeries>();
            }
        }

        private async Task<List<Episode>> GetSonarrEpisodes(int seriesId, SonarrSettings settings)
        {
            try
            {
                return (await _sonarrApi.GetEpisodes(seriesId, settings.ApiKey, settings.FullUri))?
                    .Where(x => x != null)
                    .ToList() ?? new List<Episode>();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(
                    ex,
                    "Could not read Sonarr episodes for series {SeriesId} while calculating request lifecycle status",
                    seriesId);
                return new List<Episode>();
            }
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
