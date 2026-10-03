using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ombi.Core.Services
{
    public interface IBackgroundNotificationQueue
    {
        bool TryQueue(
            string description,
            Func<IServiceProvider, Task> notification,
            Func<Exception, bool> shouldRetry = null);

        IAsyncEnumerable<BackgroundNotificationWorkItem> ReadAllAsync();
        void Complete();
    }

    public sealed class BackgroundNotificationWorkItem
    {
        public BackgroundNotificationWorkItem(
            string description,
            Func<IServiceProvider, Task> executeAsync,
            Func<Exception, bool> shouldRetry = null)
        {
            Description = description;
            ExecuteAsync = executeAsync;
            ShouldRetry = shouldRetry ?? (_ => false);
        }

        public string Description { get; }
        public Func<IServiceProvider, Task> ExecuteAsync { get; }
        public Func<Exception, bool> ShouldRetry { get; }
    }
}
