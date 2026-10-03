using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Ombi.Core.Models.Search;
using Ombi.Core.Rule.Rules;
using Ombi.Store.Context;
using Ombi.Store.Entities;
using Ombi.Store.Repository.Requests;

namespace Ombi.Core.Tests.Rule.Search
{
    [TestFixture]
    public class SonarrCacheSearchRuleTests
    {
        [Test]
        public async Task Execute_MarksEpisodesWithFilesAvailableAndComputesFullAvailability()
        {
            await using var context = CreateContext();
            context.SonarrCache.Add(new SonarrCache { TvDbId = 12345, TheMovieDbId = 54321 });
            context.SonarrEpisodeCache.AddRange(
                new SonarrEpisodeCache { TvDbId = 12345, MovieDbId = 54321, SeasonNumber = 1, EpisodeNumber = 1, HasFile = true },
                new SonarrEpisodeCache { TvDbId = 12345, MovieDbId = 54321, SeasonNumber = 1, EpisodeNumber = 2, HasFile = true });
            await context.SaveChangesAsync();

            var model = CreateSearchModel("12345", 1, 1, 2);

            var result = await new SonarrCacheRule(context).Execute(model);

            Assert.That(result.Success, Is.True);
            Assert.That(model.Approved, Is.True);
            Assert.That(model.Available, Is.True);
            Assert.That(model.FullyAvailable, Is.True);
            Assert.That(model.PartlyAvailable, Is.False);
            Assert.That(model.SeasonRequests.Single().SeasonAvailable, Is.True);
            Assert.That(model.SeasonRequests.Single().Episodes.All(x => x.Approved), Is.True);
            Assert.That(model.SeasonRequests.Single().Episodes.All(x => x.Available), Is.True);
        }

        [Test]
        public async Task Execute_LeavesMissingFilesUnavailableAndComputesPartialAvailability()
        {
            await using var context = CreateContext();
            context.SonarrCache.Add(new SonarrCache { TvDbId = 12345, TheMovieDbId = 54321 });
            context.SonarrEpisodeCache.AddRange(
                new SonarrEpisodeCache { TvDbId = 12345, MovieDbId = 54321, SeasonNumber = 1, EpisodeNumber = 1, HasFile = true },
                new SonarrEpisodeCache { TvDbId = 12345, MovieDbId = 54321, SeasonNumber = 1, EpisodeNumber = 2, HasFile = false });
            await context.SaveChangesAsync();

            var model = CreateSearchModel("12345", 1, 1, 2);

            var result = await new SonarrCacheRule(context).Execute(model);

            Assert.That(result.Success, Is.True);
            Assert.That(model.Available, Is.True);
            Assert.That(model.FullyAvailable, Is.False);
            Assert.That(model.PartlyAvailable, Is.True);
            Assert.That(model.SeasonRequests.Single().SeasonAvailable, Is.False);
            Assert.That(model.SeasonRequests.Single().Episodes.Single(x => x.EpisodeNumber == 1).Available, Is.True);
            Assert.That(model.SeasonRequests.Single().Episodes.Single(x => x.EpisodeNumber == 2).Available, Is.False);
            Assert.That(model.SeasonRequests.Single().Episodes.All(x => x.Approved), Is.True);
        }

        [Test]
        public async Task Execute_InvalidTvdbId_IsIgnoredSafely()
        {
            await using var context = CreateContext();
            var model = CreateSearchModel("not-a-tvdb-id", 1, 1);

            var result = await new SonarrCacheRule(context).Execute(model);

            Assert.That(result.Success, Is.True);
            Assert.That(model.Approved, Is.False);
            Assert.That(model.Available, Is.False);
            Assert.That(model.SeasonRequests.Single().Episodes.Single().Available, Is.False);
        }

        private static SearchTvShowViewModel CreateSearchModel(
            string tvDbId,
            int seasonNumber,
            params int[] episodeNumbers)
        {
            return new SearchTvShowViewModel
            {
                TheTvDbId = tvDbId,
                SeasonRequests = new List<SeasonRequests>
                {
                    new SeasonRequests
                    {
                        SeasonNumber = seasonNumber,
                        Episodes = episodeNumbers
                            .Select(x => new EpisodeRequests
                            {
                                EpisodeNumber = x,
                                AirDate = DateTime.Today.AddDays(-1)
                            })
                            .ToList()
                    }
                }
            };
        }

        private static TestExternalContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<TestExternalContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new TestExternalContext(options);
        }

        private sealed class TestExternalContext : ExternalContext
        {
            public TestExternalContext(DbContextOptions<TestExternalContext> options) : base(options)
            {
            }
        }
    }
}
