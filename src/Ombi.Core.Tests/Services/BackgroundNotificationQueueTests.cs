using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Ombi.Core.Services;

namespace Ombi.Core.Tests.Services
{
    [TestFixture]
    public class BackgroundNotificationQueueTests
    {
        [Test]
        public async Task TryQueue_MakesWorkItemAvailable()
        {
            var queue = new BackgroundNotificationQueue();
            var executed = false;

            var queued = queue.TryQueue("test notification", _ =>
            {
                executed = true;
                return Task.CompletedTask;
            });

            Assert.That(queued, Is.True);

            await using var enumerator = queue.ReadAllAsync().GetAsyncEnumerator();
            Assert.That(await enumerator.MoveNextAsync(), Is.True);
            Assert.That(enumerator.Current.Description, Is.EqualTo("test notification"));

            await enumerator.Current.ExecuteAsync(null);

            Assert.That(executed, Is.True);
            queue.Complete();
        }

        [Test]
        public void TryQueue_RejectsWorkWhenCapacityIsReached()
        {
            var queue = new BackgroundNotificationQueue();

            for (var i = 0; i < BackgroundNotificationQueue.DefaultCapacity; i++)
            {
                Assert.That(queue.TryQueue($"notification {i}", _ => Task.CompletedTask), Is.True);
            }

            Assert.That(queue.TryQueue("overflow notification", _ => Task.CompletedTask), Is.False);
            queue.Complete();
        }

        [Test]
        public async Task TryQueue_PreservesRetryPolicy()
        {
            var queue = new BackgroundNotificationQueue();
            var expectedException = new InvalidOperationException();

            Assert.That(
                queue.TryQueue(
                    "retryable notification",
                    _ => Task.CompletedTask,
                    ex => ReferenceEquals(ex, expectedException)),
                Is.True);

            await using var enumerator = queue.ReadAllAsync().GetAsyncEnumerator();
            Assert.That(await enumerator.MoveNextAsync(), Is.True);
            Assert.That(enumerator.Current.ShouldRetry(expectedException), Is.True);
            Assert.That(enumerator.Current.ShouldRetry(new InvalidOperationException()), Is.False);
            queue.Complete();
        }

        [Test]
        public void Complete_RejectsNewWork()
        {
            var queue = new BackgroundNotificationQueue();
            queue.Complete();

            var queued = queue.TryQueue("late notification", _ => Task.CompletedTask);

            Assert.That(queued, Is.False);
        }
    }
}
