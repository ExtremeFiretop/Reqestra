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
        public void Unchanged_Observation_Before_First_Eta_Does_Not_Replace_Useful_Baseline()
        {
            var estimator = new ActiveDownloadEtaEstimator();
            var now = new DateTime(2026, 10, 10, 18, 0, 0, DateTimeKind.Utc);

            estimator.Observe("Sonarr", "download-1", 100, now);
            Assert.That(estimator.Observe("Sonarr", "download-1", 100, now.AddSeconds(5)), Is.Null);
            var eta = estimator.Observe("Sonarr", "download-1", 80, now.AddSeconds(10));

            Assert.That(eta, Is.EqualTo(TimeSpan.FromSeconds(40)));
        }

        [Test]
        public void Unchanged_Observation_Reuses_Last_Valid_Eta_Without_Replacing_Baseline()
        {
            var estimator = new ActiveDownloadEtaEstimator();
            var now = new DateTime(2026, 10, 10, 18, 0, 0, DateTimeKind.Utc);

            estimator.Observe("Radarr", "download-1", 100, now);
            var initialEta = estimator.Observe("Radarr", "download-1", 80, now.AddSeconds(10));
            var repeatedEta = estimator.Observe("Radarr", "download-1", 80, now.AddSeconds(15));
            var updatedEta = estimator.Observe("Radarr", "download-1", 60, now.AddSeconds(20));

            Assert.That(initialEta, Is.EqualTo(TimeSpan.FromSeconds(40)));
            Assert.That(repeatedEta, Is.EqualTo(initialEta));
            Assert.That(updatedEta, Is.EqualTo(TimeSpan.FromSeconds(30)));
        }

        [Test]
        public void Cached_Eta_Expires_When_Progress_Sample_Becomes_Stale()
        {
            var estimator = new ActiveDownloadEtaEstimator();
            var now = new DateTime(2026, 10, 10, 18, 0, 0, DateTimeKind.Utc);

            estimator.Observe("Radarr", "download-1", 100, now);
            Assert.That(
                estimator.Observe("Radarr", "download-1", 80, now.AddSeconds(10)),
                Is.EqualTo(TimeSpan.FromSeconds(40)));

            Assert.That(
                estimator.Observe("Radarr", "download-1", 80, now.AddMinutes(5).AddSeconds(9)),
                Is.EqualTo(TimeSpan.FromSeconds(40)));

            Assert.That(
                estimator.Observe("Radarr", "download-1", 80, now.AddMinutes(5).AddSeconds(11)),
                Is.Null);
        }

        [Test]
        public void Increased_Remaining_Size_Resets_The_Baseline_And_Cached_Eta()
        {
            var estimator = new ActiveDownloadEtaEstimator();
            var now = new DateTime(2026, 10, 10, 18, 0, 0, DateTimeKind.Utc);

            estimator.Observe("Radarr", "download-1", 100, now);
            Assert.That(
                estimator.Observe("Radarr", "download-1", 80, now.AddSeconds(10)),
                Is.EqualTo(TimeSpan.FromSeconds(40)));

            Assert.That(estimator.Observe("Radarr", "download-1", 120, now.AddSeconds(20)), Is.Null);
            Assert.That(estimator.Observe("Radarr", "download-1", 120, now.AddSeconds(25)), Is.Null);

            var eta = estimator.Observe("Radarr", "download-1", 100, now.AddSeconds(30));

            Assert.That(eta, Is.EqualTo(TimeSpan.FromSeconds(50)));
        }

        [Test]
        public void Forget_Removes_Previous_Sample_And_Cached_Eta()
        {
            var estimator = new ActiveDownloadEtaEstimator();
            var now = new DateTime(2026, 10, 10, 18, 0, 0, DateTimeKind.Utc);

            estimator.Observe("Radarr", "download-1", 100, now);
            Assert.That(
                estimator.Observe("Radarr", "download-1", 80, now.AddSeconds(10)),
                Is.EqualTo(TimeSpan.FromSeconds(40)));

            estimator.Forget("Radarr", "download-1");

            Assert.That(estimator.Observe("Radarr", "download-1", 60, now.AddSeconds(20)), Is.Null);
        }
    }
}
