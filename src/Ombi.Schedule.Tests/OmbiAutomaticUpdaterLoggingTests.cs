using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Ombi.Core.Processor;
using Ombi.Core.Settings;
using Ombi.Hubs;
using Ombi.Schedule.Jobs.Ombi;
using Ombi.Settings.Settings.Models;
using Ombi.Store.Repository;
using Ombi.Updater;

namespace Ombi.Schedule.Tests
{
    [TestFixture]
    public class OmbiAutomaticUpdaterLoggingTests
    {
        [Test]
        public async Task Execute_WhenGitHubErrorContainsHtml_LogsBoundedSafeDetails()
        {
            const string embeddedPayload = "BEGIN_SENSITIVE_HTML_PAYLOAD";
            var html = "<!DOCTYPE html><html><img src=\"data:image/png;base64," +
                       new string('X', 100_000) + embeddedPayload + "\"></html>";
            await AssertSafeTransientUpdateLog(new HttpRequestException(html), embeddedPayload);
        }

        [Test]
        public async Task Execute_WhenTransientFailureIsWrapped_DoesNotLogOuterOrInnerMessages()
        {
            const string embeddedPayload = "SECRET_GITHUB_RESPONSE";
            var exception = new InvalidOperationException("Outer error: " + embeddedPayload,
                new HttpRequestException("<html>" + embeddedPayload + "</html>"));
            await AssertSafeTransientUpdateLog(exception, embeddedPayload);
        }

        private static async Task AssertSafeTransientUpdateLog(Exception failure, string sensitiveMarker)
        {
            var logger = new Mock<ILogger<OmbiAutomaticUpdater>>();
            var processor = new Mock<IChangeLogProcessor>();
            processor.Setup(x => x.Process()).ThrowsAsync(failure);
            var settings = new Mock<ISettingsService<UpdateSettings>>();
            settings.Setup(x => x.GetSettingsAsync())
                .ReturnsAsync(new UpdateSettings { AutoUpdateEnabled = true });
            var subject = new OmbiAutomaticUpdater(logger.Object, processor.Object, settings.Object,
                Mock.Of<IProcessProvider>(), Mock.Of<IApplicationConfigRepository>(),
                Mock.Of<INotificationHubService>());

            await subject.Execute(null);
            processor.Verify(x => x.Process(), Times.Once);

            var logs = logger.Invocations
                .Where(x => x.Method.Name == nameof(ILogger.Log))
                .Select(x => new
                {
                    Level = (LogLevel)x.Arguments[0],
                    Message = x.Arguments[2]?.ToString() ?? string.Empty,
                    Exception = x.Arguments[3] as Exception
                })
                .ToList();
            Assert.That(logs.Any(x => x.Level == LogLevel.Warning &&
                                     x.Message.Contains("will retry at the next scheduled check")), Is.True);
            foreach (var log in logs)
            {
                Assert.That(log.Message.Length, Is.LessThan(512), "Unexpected unbounded update log");
                Assert.That(log.Message, Does.Not.Contain(sensitiveMarker));
                Assert.That(log.Message, Does.Not.Contain("<html>"));
                Assert.That(log.Message, Does.Not.Contain("data:image/png;base64"));
                Assert.That(log.Exception, Is.Null,
                    "Logging the exception itself can emit its full untrusted HTML response");
            }
        }
    }
}
