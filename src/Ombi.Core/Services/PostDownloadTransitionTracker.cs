using System;

namespace Ombi.Core.Services
{
    public class PostDownloadTransitionTracker : IPostDownloadTransitionTracker
    {
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
            // Arr does not expose NZBGet's post-processing status directly. Use the current Arr queue
            // row as the source of truth instead of requiring Reqestra to observe a particular prior
            // progress sample. Direct download-client integration would be required to distinguish a
            // deliberate manual pause from NZBGet unpacking with complete certainty.
            if (!IsHealthy(trackedDownloadStatus) ||
                IsExplicitTerminalTrackedState(trackedDownloadState) ||
                IsQueued(status))
            {
                return false;
            }

            if (!IsDownloadingTrackedState(trackedDownloadState))
            {
                return false;
            }

            // NZBGet post-processing can appear in Arr as a healthy paused/completed row while Arr
            // continues to report the tracked download state as downloading.
            if (IsPausedOrCompleted(status))
            {
                return true;
            }

            // Some Radarr/NZBGet combinations keep status=downloading after network transfer has
            // finished. Treat an effectively 100% row as post-processing so it cannot remain stuck
            // at "Downloading · 100%" during unpacking.
            return IsDownloading(status) && IsEffectivelyTransferComplete(size, sizeLeft);
        }

        private static bool IsEffectivelyTransferComplete(decimal size, decimal sizeLeft)
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
    }
}
