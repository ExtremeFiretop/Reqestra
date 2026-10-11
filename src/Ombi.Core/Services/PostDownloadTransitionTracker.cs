using System;
using System.Collections.Generic;

namespace Ombi.Core.Services
{
    public class PostDownloadTransitionTracker : IPostDownloadTransitionTracker
    {
        private static readonly TimeSpan EvidenceLifetime = TimeSpan.FromMinutes(30);
        private readonly object _sync = new object();
        private readonly Dictionary<string, DateTime> _completedTransfers =
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
                // post-processing from them, and discard any earlier transfer-complete evidence.
                if (!IsHealthy(trackedDownloadStatus))
                {
                    _completedTransfers.Remove(key);
                    return false;
                }

                if (IsDownloading(status))
                {
                    if (RoundsToOneHundredPercent(size, sizeLeft))
                    {
                        _completedTransfers[key] = observedAtUtc;
                    }
                    else
                    {
                        // The same id is actively transferring again. Clear stale completion
                        // evidence so a later manual pause cannot inherit an old completed state.
                        _completedTransfers.Remove(key);
                    }

                    return false;
                }

                if (IsExplicitTerminalTrackedState(trackedDownloadState) || IsQueued(status))
                {
                    _completedTransfers.Remove(key);
                    return false;
                }

                if (!IsPausedOrCompleted(status) || !IsDownloadingTrackedState(trackedDownloadState))
                {
                    return false;
                }

                return _completedTransfers.TryGetValue(key, out var completedAtUtc) &&
                       observedAtUtc - completedAtUtc <= EvidenceLifetime;
            }
        }

        private void PruneExpiredEvidence(DateTime observedAtUtc)
        {
            var expired = new List<string>();
            foreach (var pair in _completedTransfers)
            {
                if (observedAtUtc - pair.Value > EvidenceLifetime)
                {
                    expired.Add(pair.Key);
                }
            }

            foreach (var key in expired)
            {
                _completedTransfers.Remove(key);
            }
        }

        private static bool RoundsToOneHundredPercent(decimal size, decimal sizeLeft)
        {
            if (size <= 0)
            {
                return false;
            }

            var remaining = Math.Max(0m, Math.Min(size, sizeLeft));
            var rawProgress = 100m * (size - remaining) / size;
            var displayedProgress = Math.Max(0, Math.Min(100,
                (int)Math.Round(rawProgress, MidpointRounding.AwayFromZero)));
            return displayedProgress == 100;
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
    }
}
