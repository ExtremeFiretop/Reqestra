using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ombi.Schedule.Jobs.Plex
{
    /// <summary>
    /// Serializes full and recently-added Plex content syncs with destructive Plex
    /// media database refreshes. This lock is process-local and does not coordinate
    /// multiple Reqestra instances.
    /// </summary>
    public static class PlexContentSyncLock
    {
        private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);

        public static async Task<IDisposable> AcquireAsync()
        {
            await Gate.WaitAsync();
            return new Releaser();
        }

        private sealed class Releaser : IDisposable
        {
            private int _released;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _released, 1) == 0)
                {
                    Gate.Release();
                }
            }
        }
    }
}
