using System;

namespace Ombi.Core.Services
{
    public interface IActiveDownloadEtaEstimator
    {
        TimeSpan? Observe(string source, string downloadId, decimal sizeLeft, DateTime observedAtUtc);
        void Forget(string source, string downloadId);
    }
}
