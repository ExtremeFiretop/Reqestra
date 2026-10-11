using System;
using NUnit.Framework;
using Ombi.Core.Services;

namespace Ombi.Core.Tests.Services
{
    [TestFixture]
    public class ActiveDownloadEtaEstimatorTests
    {
        [Test]
        public void First_Observation_Has_No_Eta()
        {
            var estimator = new ActiveDownloadEtaEstimator();
            var now = new DateTime(2026, 10, 10, 18, 0, 0, DateTimeKind.Utc);

            var eta = estimator.Observe("Radarr", "download-1", 100, now);

            Assert.That(eta, Is.Null);
        }

        [Test]
        public void Later_Observation_Derives_Eta_From_Observed_Progress()
        {
            var estimator = new ActiveDownloadEtaEstimator();
            var now = new DateTime(2026, 10, 10, 18, 0, 0, DateTimeKind.Utc);

            estimator.Observe("Radarr", "download-1", 100, now);
            var eta = estimator.Observe("Radarr", "download-1", 80, now.AddSeconds(10));

            Assert.That(eta, Is.EqualTo(TimeSpan.FromSeconds(40)));
        }

        [Test]
        public void Unchanged_Observation_Does_Not_Replace_Useful_Baseline()
        {
            var estimator = new ActiveDownloadEtaEstimator();
            var now = new DateTime(2026, 10, 10, 18, 0, 0, DateTimeKind.Utc);

            estimator.Observe("Sonarr", "download-1", 100, now);
            Assert.That(estimator.Observe("Sonarr", "download-1", 100, now.AddSeconds(5)), Is.Null);
            var eta = estimator.Observe("Sonarr", "download-1", 80, now.AddSeconds(10));

            Assert.That(eta, Is.EqualTo(TimeSpan.FromSeconds(40)));
        }

        [Test]
        public void Increased_Remaining_Size_Resets_The_Baseline()
        {
            var estimator = new ActiveDownloadEtaEstimator();
            var now = new DateTime(2026, 10, 10, 18, 0, 0, DateTimeKind.Utc);

            estimator.Observe("Radarr", "download-1", 100, now);
            Assert.That(estimator.Observe("Radarr", "download-1", 120, now.AddSeconds(10)), Is.Null);
            var eta = estimator.Observe("Radarr", "download-1", 100, now.AddSeconds(20));

            Assert.That(eta, Is.EqualTo(TimeSpan.FromSeconds(50)));
        }

        [Test]
        public void Forget_Removes_Previous_Sample()
        {
            var estimator = new ActiveDownloadEtaEstimator();
            var now = new DateTime(2026, 10, 10, 18, 0, 0, DateTimeKind.Utc);

            estimator.Observe("Radarr", "download-1", 100, now);
            estimator.Forget("Radarr", "download-1");

            Assert.That(estimator.Observe("Radarr", "download-1", 80, now.AddSeconds(10)), Is.Null);
        }
    }
}
