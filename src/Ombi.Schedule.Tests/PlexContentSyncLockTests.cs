using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Ombi.Schedule.Jobs.Plex;

namespace Ombi.Schedule.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class PlexContentSyncLockTests
    {
        [Test]
        public async Task AcquireAsync_SerializesCallers()
        {
            IDisposable first = await PlexContentSyncLock.AcquireAsync();
            Task<IDisposable> second = null;
            IDisposable secondLease = null;

            try
            {
                second = PlexContentSyncLock.AcquireAsync();

                Assert.That(second.IsCompleted, Is.False);

                first.Dispose();
                first = null;

                secondLease = await second;
                Assert.That(secondLease, Is.Not.Null);
            }
            finally
            {
                secondLease?.Dispose();
                first?.Dispose();
            }
        }
    }
}
