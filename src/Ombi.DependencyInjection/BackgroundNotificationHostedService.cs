using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ombi.Core.Services;

namespace Ombi.DependencyInjection
{
    public sealed class BackgroundNotificationHostedService : BackgroundService
    {
        private static readonly TimeSpan[] RetryDelays =
        {
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(10)
        };

        private readonly IBackgroundNotificationQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BackgroundNotificationHostedService> _logger;

        public BackgroundNotificationHostedService(
            IBackgroundNotificationQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<BackgroundNotificationHostedService> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Stop accepting new notifications when the host begins shutting down, then drain
            // work already queued. BackgroundService.StopAsync will wait for this task until the
            // host's normal shutdown timeout expires.
            using var stoppingRegistration = stoppingToken.Register(_queue.Complete);

            await foreach (var workItem in _queue.ReadAllAsync())
            {
                await ExecuteWithRetryAsync(workItem);
            }
        }

        private async Task ExecuteWithRetryAsync(BackgroundNotificationWorkItem workItem)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    // Resolve scoped notification dependencies again for every attempt. A failed
                    // send must not leave a scoped service instance alive for a later retry.
                    using var scope = _scopeFactory.CreateScope();
                    await workItem.ExecuteAsync(scope.ServiceProvider);
                    return;
                }
                catch (Exception ex) when (attempt <= RetryDelays.Length && workItem.ShouldRetry(ex))
                {
                    var delay = RetryDelays[attempt - 1];
                    _logger.LogWarning(
                        ex,
                        "Background notification failed transiently: {Description}. Retrying in {DelaySeconds} seconds (attempt {NextAttempt}/{TotalAttempts})",
                        workItem.Description,
                        delay.TotalSeconds,
                        attempt + 1,
                        RetryDelays.Length + 1);

                    // Do not bind the retry delay to stoppingToken. Once shutdown begins the queue
                    // is completed, but already-accepted work is intentionally allowed to drain.
                    await Task.Delay(delay);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Background notification failed after {Attempts} attempt(s): {Description}",
                        attempt,
                        workItem.Description);
                    return;
                }
            }
        }
    }
}
