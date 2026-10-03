using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Ombi.Core.Engine;
using Ombi.Core.Rule.Rules;
using Ombi.Store.Context;
using Ombi.Store.Entities;
using Ombi.Store.Entities.Requests;
using Ombi.Store.Repository.Requests;

namespace Ombi.Core.Tests.Rule.Request
{
    [TestFixture]
    public class SonarrCacheRequestRuleTests
    {
        [Test]
        public async Task Execute_DoesNotUseTmdbOnlyEpisodeMatchesForRequestDeduplication()
        {
            await using var context = CreateContext();
            context.SonarrEpisodeCache.AddRange(
                new SonarrEpisodeCache { TvDbId = 389492, MovieDbId = 299939, SeasonNumber = 1, EpisodeNumber = 1 },
                new SonarrEpisodeCache { TvDbId = 389492, MovieDbId = 299939, SeasonNumber = 1, EpisodeNumber = 2 },
                new SonarrEpisodeCache { TvDbId = 389492, MovieDbId = 299939, SeasonNumber = 1, EpisodeNumber = 3 });
            await context.SaveChangesAsync();

            var request = CreateRequest(
                requestTheMovieDbId: 299939,
                requestTvDbId: 0,
                seasonNumber: 1,
                episodeNumbers: new[] { 1, 2, 3 });

            var result = await new SonarrCacheRule(context).Execute(request);

            Assert.That(result.Success, Is.True);
            Assert.That(request.SeasonRequests.Single().Episodes.Select(x => x.EpisodeNumber),
                Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public async Task Execute_DoesNotTreatChildRequestPrimaryKeyAsTmdbId()
        {
            await using var context = CreateContext();
            context.SonarrEpisodeCache.Add(
                new SonarrEpisodeCache { TvDbId = 12345, MovieDbId = 299939, SeasonNumber = 1, EpisodeNumber = 1 });
            await context.SaveChangesAsync();

            var request = CreateRequest(
                requestTheMovieDbId: 0,
                requestTvDbId: 0,
                seasonNumber: 1,
                episodeNumbers: 1);
            request.Id = 299939;

            var result = await new SonarrCacheRule(context).Execute(request);

            Assert.That(result.Success, Is.True);
            Assert.That(request.SeasonRequests.Single().Episodes, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task Execute_UsesTvdbEpisodeMatchesWhenTvdbIdentityIsAvailable()
        {
            await using var context = CreateContext();
            context.SonarrEpisodeCache.AddRange(
                new SonarrEpisodeCache { TvDbId = 389492, MovieDbId = 299939, SeasonNumber = 4, EpisodeNumber = 1 },
                new SonarrEpisodeCache { TvDbId = 389492, MovieDbId = 299939, SeasonNumber = 4, EpisodeNumber = 2 });
            await context.SaveChangesAsync();

            var request = CreateRequest(
                requestTheMovieDbId: 299939,
                requestTvDbId: 389492,
                seasonNumber: 4,
                episodeNumbers: new[] { 1, 2 });

            var result = await new SonarrCacheRule(context).Execute(request);

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.EpisodesAlreadyRequested));
            Assert.That(request.SeasonRequests.Single().Episodes, Is.Empty);
        }

        private static ChildRequests CreateRequest(
            int requestTheMovieDbId,
            int requestTvDbId,
            int seasonNumber,
            params int[] episodeNumbers)
        {
            return new ChildRequests
            {
                RequestType = RequestType.TvShow,
                RequestTheMovieDbId = requestTheMovieDbId,
                RequestTvDbId = requestTvDbId,
                Title = "Anthology test",
                SeasonRequests = new List<SeasonRequests>
                {
                    new SeasonRequests
                    {
                        SeasonNumber = seasonNumber,
                        Episodes = episodeNumbers
                            .Select(x => new EpisodeRequests { EpisodeNumber = x })
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
