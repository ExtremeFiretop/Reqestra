using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Ombi.Core.Helpers;

namespace Ombi.Core.Tests.Helpers
{
    [TestFixture]
    public class DiscoveryReleaseFilterTests
    {
        private static readonly DateTime Today = new DateTime(2026, 10, 11);

        [TestCase("2026-10-10", true)]
        [TestCase("2026-10-11", true)]
        [TestCase("2026-10-12", false)]
        [TestCase("", false)]
        [TestCase("not-a-date", false)]
        public void IsReleased_Requires_A_Known_Date_On_Or_Before_Today(string releaseDate, bool expected)
        {
            Assert.That(DiscoveryReleaseFilter.IsReleased(releaseDate, Today), Is.EqualTo(expected));
        }

        [TestCase("2026-10-10", false)]
        [TestCase("2026-10-11", false)]
        [TestCase("2026-10-12", true)]
        [TestCase("", false)]
        [TestCase("not-a-date", false)]
        public void IsUpcoming_Requires_A_Known_Date_After_Today(string releaseDate, bool expected)
        {
            Assert.That(DiscoveryReleaseFilter.IsUpcoming(releaseDate, Today), Is.EqualTo(expected));
        }

        [Test]
        public async Task LoadFilteredPage_Applies_Logical_Offset_After_Filtering()
        {
            var pages = new Dictionary<int, List<TestItem>>
            {
                [1] = new List<TestItem>
                {
                    new TestItem(1, "2026-10-10"),
                    new TestItem(2, "2026-10-12"),
                    new TestItem(3, "2026-10-09")
                },
                [2] = new List<TestItem>
                {
                    new TestItem(4, "2026-10-13"),
                    new TestItem(5, "2026-10-11"),
                    new TestItem(6, "2026-10-08")
                }
            };

            var result = await DiscoveryReleaseFilter.LoadFilteredPage(
                currentPosition: 1,
                amountToLoad: 3,
                sourcePageSize: 3,
                pageLoader: page => Task.FromResult(
                    pages.TryGetValue(page, out var items) ? items : new List<TestItem>()),
                include: item => DiscoveryReleaseFilter.IsReleased(item.ReleaseDate, Today));

            Assert.That(result.Select(x => x.Id), Is.EqualTo(new[] { 3, 5, 6 }));
        }

        private sealed class TestItem
        {
            public TestItem(int id, string releaseDate)
            {
                Id = id;
                ReleaseDate = releaseDate;
            }

            public int Id { get; }
            public string ReleaseDate { get; }
        }
    }
}
