using System;
using System.Linq;
using NUnit.Framework;
using Ombi.Core.Engine;
using Ombi.Store.Entities.Requests;

namespace Ombi.Core.Tests.Engine
{
    [TestFixture]
    public class TvRequestGridAggregatorTests
    {
        [Test]
        public void Processing_Request_Wins_Over_Newer_Completed_Request_Regardless_Of_Input_Order()
        {
            var parent = new TvRequests { Id = 1428, Title = "MythBusters" };
            var processing = BuildChild(10, parent, new DateTime(2024, 4, 27), approved: true);
            var completed = BuildChild(20, parent, new DateTime(2026, 9, 15), approved: true);
            completed.Completed = true;

            var completedFirst = TvRequestGridAggregator.CollapseToSeriesCards(new[] { completed, processing }).Single();
            var processingFirst = TvRequestGridAggregator.CollapseToSeriesCards(new[] { processing, completed }).Single();

            Assert.That(completedFirst.Id, Is.EqualTo(processing.Id));
            Assert.That(processingFirst.Id, Is.EqualTo(processing.Id));
            Assert.That(completedFirst.RequestStatus, Is.EqualTo("Common.ProcessingRequest"));
        }

        [Test]
        public void Active_Download_Wins_Over_Other_Unresolved_States()
        {
            var parent = new TvRequests { Id = 99, Title = "Example" };
            var processing = BuildChild(1, parent, new DateTime(2026, 1, 1), approved: true);
            var searching = BuildChild(2, parent, new DateTime(2026, 1, 2), approved: true);
            searching.Searching = true;
            var downloading = BuildChild(3, parent, new DateTime(2026, 1, 3), approved: true);
            downloading.Downloading = true;

            var result = TvRequestGridAggregator.CollapseToSeriesCards(new[] { processing, searching, downloading }).Single();

            Assert.That(result.Id, Is.EqualTo(downloading.Id));
            Assert.That(result.RequestStatus, Is.EqualTo("Common.Downloading"));
        }

        [Test]
        public void Same_Title_Different_Parents_Are_Not_Collapsed_Together()
        {
            var firstParent = new TvRequests { Id = 101, Title = "The Office" };
            var secondParent = new TvRequests { Id = 202, Title = "The Office" };

            var first = BuildChild(1, firstParent, new DateTime(2026, 1, 1), approved: true);
            var second = BuildChild(2, secondParent, new DateTime(2026, 1, 2), approved: true);

            var result = TvRequestGridAggregator.CollapseToSeriesCards(new[] { first, second });

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result.Select(x => x.ParentRequestId), Is.EquivalentTo(new[] { 101, 202 }));
        }

        [Test]
        public void Equal_Lifecycle_Priority_Uses_Newest_Child_Deterministically()
        {
            var parent = new TvRequests { Id = 77, Title = "Example" };
            var older = BuildChild(1, parent, new DateTime(2024, 1, 1), approved: true);
            older.Searching = true;
            var newer = BuildChild(2, parent, new DateTime(2026, 1, 1), approved: true);
            newer.Searching = true;

            var result = TvRequestGridAggregator.CollapseToSeriesCards(new[] { newer, older }).Single();

            Assert.That(result.Id, Is.EqualTo(newer.Id));
            Assert.That(result.RequestStatus, Is.EqualTo("Common.Searching"));
        }

        private static ChildRequests BuildChild(
            int id,
            TvRequests parent,
            DateTime requestedDate,
            bool approved)
        {
            return new ChildRequests
            {
                Id = id,
                ParentRequest = parent,
                ParentRequestId = parent.Id,
                RequestedDate = requestedDate,
                Approved = approved,
                Available = false,
                Denied = false
            };
        }
    }
}
