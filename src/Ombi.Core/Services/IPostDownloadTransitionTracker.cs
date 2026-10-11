using System;

namespace Ombi.Core.Services
{
    public interface IPostDownloadTransitionTracker
    {
        bool ObserveAndIsPostDownloadProcessing(
            string source,
            string downloadId,
            string status,
            string trackedDownloadState,
            string trackedDownloadStatus,
            decimal size,
            decimal sizeLeft,
            DateTime observedAtUtc);
    }
}
