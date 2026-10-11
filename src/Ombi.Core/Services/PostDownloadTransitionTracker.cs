using System;
using System.Collections.Generic;

namespace Ombi.Core.Services
{
    public class PostDownloadTransitionTracker : IPostDownloadTransitionTracker
    {
        private const decimal NearCompletionPercent = 95m;
        private static readonly TimeSpan ActiveObservationLifetime = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan EvidenceLifetime = TimeSpan.FromMinutes(30);
        private readonly object _sync = new object();
        private readonly Dictionary<string, ActiveTransferObservation> _recentActiveTransfers =
            new Dictionary<string, ActiveTransferObservation>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DateTime> _postDownloadTransfers =
            new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        public bool ObserveAndIsPostDownloadProcessing(
            string source,
            string downloadId,
            string status,
            string trackedDownloadState,
            string trackedDownloadStatus,
            decimal size,
            decimal sizeLeft,
            DateTime observedAtUtc)
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(downloadId))
            {
                return false;
            }

            var key = BuildKey(source, downloadId);
            lock (_sync)
            {
                PruneExpiredEvidence(observedAtUtc);

                // Warning/error queue rows can represent a blocked or failed import. Never infer
                // post-processing from them, and discard any earlier transition evidence.
                if (!IsHealthy(trackedDownloadStatus))
                {
                    ClearEvidence(key);
                    return false;
                }

                if (IsDownloading(status))
                {
                    var progress = GetProgressPercent(size, sizeLeft);
                    if (progress.HasValue)
                    {
                        _recentActiveTransfers[key] = new ActiveTransferObservation(
                            progress.Value,
                            observedAtUtc);
                    }
                    else
                    {
                        _recentActiveTransfers.Remove(key);
                    }

                    // The same id is actively transferring again, so it cannot still be in a
                    // previously inferred post-processing phase.
                    _postDownloadTransfers.Remove(key);
                    return false;
                }

                if (IsExplicitTerminalTrackedState(trackedDownloadState) || IsQueued(status))
                {
                    ClearEvidence(key);
                    return false;
                }

                if (!IsPausedOrCompleted(status) || !IsDownloadingTrackedState(trackedDownloadState))
                {
                    return false;
                }

                if (_postDownloadTransfers.TryGetValue(key, out var postDownloadAtUtc) &&
                    observedAtUtc - postDownloadAtUtc <= EvidenceLifetime)
                {
                    return true;
                }

                if (!_recentActiveTransfers.TryGetValue(key, out var activeObservation) ||
                    observedAtUtc - activeObservation.ObservedAtUtc > ActiveObservationLifetime)
                {
                    return false;
                }

                // Reqestra may never observe an exact 100% downloading row: NZBGet can finish the
                // transfer and start unpacking between polls, while Arr immediately reports the same
                // download id as paused. Accept that handoff when either the last active observation
                // or the current paused row is already near completion.
                var currentProgress = GetProgressPercent(size, sizeLeft);
                if (activeObservation.ProgressPercent < NearCompletionPercent &&
                    (!currentProgress.HasValue || currentProgress.Value < NearCompletionPercent))
                {
                    return false;
                }

                _recentActiveTransfers.Remove(key);
                _postDownloadTransfers[key] = observedAtUtc;
                return true;
            }
        }

        private void PruneExpiredEvidence(DateTime observedAtUtc)
        {
            var expiredActive = new List<string>();
            foreach (var pair in _recentActiveTransfers)
            {
                if (observedAtUtc - pair.Value.ObservedAtUtc > ActiveObservationLifetime)
                {
                    expiredActive.Add(pair.Key);
                }
            }

            foreach (var key in expiredActive)
            {
                _recentActiveTransfers.Remove(key);
            }

            var expiredPostDownload = new List<string>();
            foreach (var pair in _postDownloadTransfers)
            {
                if (observedAtUtc - pair.Value > EvidenceLifetime)
                {
                    expiredPostDownload.Add(pair.Key);
                }
            }

            foreach (var key in expiredPostDownload)
            {
                _postDownloadTransfers.Remove(key);
            }
        }

        private void ClearEvidence(string key)
        {
            _recentActiveTransfers.Remove(key);
            _postDownloadTransfers.Remove(key);
        }

        private static decimal? GetProgressPercent(decimal size, decimal sizeLeft)
        {
            if (size <= 0)
            {
                return null;
            }

            var remaining = Math.Max(0m, Math.Min(size, sizeLeft));
            return 100m * (size - remaining) / size;
        }

        private static bool IsHealthy(string trackedDownloadStatus)
        {
            return string.IsNullOrWhiteSpace(trackedDownloadStatus) ||
                   string.Equals(trackedDownloadStatus, "ok", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDownloading(string status)
        {
            return string.Equals(status, "downloading", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPausedOrCompleted(string status)
        {
            return string.Equals(status, "paused", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "completed", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsQueued(string status)
        {
            return string.Equals(status, "queued", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDownloadingTrackedState(string trackedDownloadState)
        {
            // Older Arr responses may omit trackedDownloadState while status still reflects the
            // download-client row. Treat an omitted value like Arr's UI default of "downloading".
            return string.IsNullOrWhiteSpace(trackedDownloadState) ||
                   string.Equals(trackedDownloadState, "downloading", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsExplicitTerminalTrackedState(string trackedDownloadState)
        {
            return string.Equals(trackedDownloadState, "imported", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(trackedDownloadState, "failed", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(trackedDownloadState, "failedPending", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(trackedDownloadState, "ignored", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildKey(string source, string downloadId)
        {
            return source + "\n" + downloadId;
        }

        private sealed class ActiveTransferObservation
        {
            public ActiveTransferObservation(decimal progressPercent, DateTime observedAtUtc)
            {
                ProgressPercent = progressPercent;
                ObservedAtUtc = observedAtUtc;
            }

            public decimal ProgressPercent { get; }
            public DateTime ObservedAtUtc { get; }
        }
    }
}
