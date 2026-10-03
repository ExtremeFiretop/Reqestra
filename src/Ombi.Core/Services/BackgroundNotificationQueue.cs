using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Ombi.Core.Services
{
    public sealed class BackgroundNotificationQueue : IBackgroundNotificationQueue
    {
        public const int DefaultCapacity = 100;

        private readonly Channel<BackgroundNotificationWorkItem> _queue =
            Channel.CreateBounded<BackgroundNotificationWorkItem>(
                new BoundedChannelOptions(DefaultCapacity)
                {
                    SingleReader = true,
                    SingleWriter = false,
                    AllowSynchronousContinuations = false,
                    // TryQueue deliberately uses TryWrite. Wait mode makes TryWrite return false
                    // when the queue is full instead of silently dropping an accepted item.
                    FullMode = BoundedChannelFullMode.Wait
                });

        public bool TryQueue(string description, Func<IServiceProvider, Task> notification, Func<Exception, bool> shouldRetry = null)
        {
            ArgumentNullException.ThrowIfNull(notification);

            var workItem = new BackgroundNotificationWorkItem(
                string.IsNullOrWhiteSpace(description) ? "Background notification" : description,
                notification,
                shouldRetry);

            return _queue.Writer.TryWrite(workItem);
        }

        public IAsyncEnumerable<BackgroundNotificationWorkItem> ReadAllAsync()
        {
            return _queue.Reader.ReadAllAsync();
        }

        public void Complete()
        {
            _queue.Writer.TryComplete();
        }
    }
}
