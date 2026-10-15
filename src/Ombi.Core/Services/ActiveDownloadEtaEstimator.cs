using System;
using System.Collections.Generic;

namespace Ombi.Core.Services
{
    public class ActiveDownloadEtaEstimator : IActiveDownloadEtaEstimator
    {
        private static readonly TimeSpan MaxSampleAge = TimeSpan.FromMinutes(5);
        private readonly object _sync = new object();
        private readonly Dictionary<string, DownloadSample> _samples =
            new Dictionary<string, DownloadSample>(StringComparer.OrdinalIgnoreCase);

        public TimeSpan? Observe(string source, string downloadId, decimal sizeLeft, DateTime observedAtUtc)
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(downloadId) || sizeLeft < 0)
            {
                return null;
            }

            var key = BuildKey(source, downloadId);
            var current = new DownloadSample(sizeLeft, observedAtUtc);

            lock (_sync)
            {
                PruneExpiredSamples(observedAtUtc);

                if (!_samples.TryGetValue(key, out var previous))
                {
                    _samples[key] = current;
                    return null;
                }

                var elapsed = observedAtUtc - previous.ObservedAtUtc;
                if (elapsed <= TimeSpan.Zero || elapsed > MaxSampleAge || sizeLeft > previous.SizeLeft)
                {
                    _samples[key] = current;
                    return null;
                }

                // Preserve the older progress sample while no progress is observed. This avoids
                // manufacturing a new transfer rate from a duplicate queue observation while still
                // allowing the UI to retain the last ETA that was derived from real byte progress.
                if (sizeLeft == previous.SizeLeft)
                {
                    return previous.EstimatedTimeRemaining;
                }

                var downloaded = previous.SizeLeft - sizeLeft;
                var unitsPerSecond = downloaded / (decimal)elapsed.TotalSeconds;
                if (unitsPerSecond <= 0)
                {
                    _samples[key] = current;
                    return null;
                }

                var secondsRemaining = sizeLeft / unitsPerSecond;
                if (secondsRemaining <= 0 || secondsRemaining > (decimal)TimeSpan.MaxValue.TotalSeconds)
                {
                    _samples[key] = current;
                    return null;
                }

                var estimatedTimeRemaining = TimeSpan.FromSeconds((double)secondsRemaining);
                _samples[key] = new DownloadSample(sizeLeft, observedAtUtc, estimatedTimeRemaining);
                return estimatedTimeRemaining;
            }
        }

        public void Forget(string source, string downloadId)
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(downloadId))
            {
                return;
            }

            lock (_sync)
            {
                _samples.Remove(BuildKey(source, downloadId));
            }
        }

        private void PruneExpiredSamples(DateTime observedAtUtc)
        {
            var expiredKeys = new List<string>();
            foreach (var pair in _samples)
            {
                if (observedAtUtc - pair.Value.ObservedAtUtc > MaxSampleAge)
                {
                    expiredKeys.Add(pair.Key);
                }
            }

            foreach (var key in expiredKeys)
            {
                _samples.Remove(key);
            }
        }

        private static string BuildKey(string source, string downloadId)
        {
            return source + "\n" + downloadId;
        }

        private sealed class DownloadSample
        {
            public DownloadSample(
                decimal sizeLeft,
                DateTime observedAtUtc,
                TimeSpan? estimatedTimeRemaining = null)
            {
                SizeLeft = sizeLeft;
                ObservedAtUtc = observedAtUtc;
                EstimatedTimeRemaining = estimatedTimeRemaining;
            }

            public decimal SizeLeft { get; }
            public DateTime ObservedAtUtc { get; }
            public TimeSpan? EstimatedTimeRemaining { get; }
        }
    }
}
