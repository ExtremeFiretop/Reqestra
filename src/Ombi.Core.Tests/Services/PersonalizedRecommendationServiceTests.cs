using MockQueryable.Moq;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Ombi.Api.External.ExternalApis.TheMovieDb;
using Ombi.Api.External.ExternalApis.TheMovieDb.Models;
using Ombi.Core.Helpers;
using Ombi.Core.Services;
using Ombi.Helpers;
using Ombi.Store.Entities;
using Ombi.Store.Entities.Requests;
using Ombi.Store.Repository.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ombi.Core.Tests.Services
{
    [TestFixture]
    public class PersonalizedRecommendationServiceTests
    {
        private const string UserId = "user-1";

        private AutoMocker _mocker;
        private PersonalizedRecommendationService _subject;
        private Mock<IMovieRequestRepository> _movieRequests;
        private Mock<ITvRequestRepository> _tvRequests;
        private Mock<IMovieDbApi> _movieDbApi;

        [SetUp]
        public void Setup()
        {
            _mocker = new AutoMocker();
            _movieRequests = _mocker.GetMock<IMovieRequestRepository>();
            _tvRequests = _mocker.GetMock<ITvRequestRepository>();
            _movieDbApi = _mocker.GetMock<IMovieDbApi>();

            _mocker.GetMock<ICurrentUser>()
                .Setup(x => x.GetUser())
                .ReturnsAsync(new OmbiUser { Id = UserId, UserName = "test" });

            _mocker.GetMock<ICacheService>()
                .Setup(x => x.GetOrAddAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<MovieDbSearchResult>>>>(),
                    It.IsAny<DateTimeOffset>()))
                .Returns((string _, Func<Task<List<MovieDbSearchResult>>> factory, DateTimeOffset __) => factory());

            SetupRequests(new List<MovieRequests>(), new List<ChildRequests>());
            _subject = _mocker.CreateInstance<PersonalizedRecommendationService>();
        }

        [Test]
        public async Task NewUserWithoutRequests_ReturnsEmptyWithoutCallingProvider()
        {
            var result = await _subject.GetMovieRecommendations("en", CancellationToken.None);

            Assert.That(result, Is.Empty);
            _movieDbApi.Verify(x => x.GetRecommendations(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _mocker.GetMock<ICacheService>().Verify(x => x.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<MovieDbSearchResult>>>>(),
                It.IsAny<DateTimeOffset>()), Times.Never);
        }

        [Test]
        public async Task FirstRequestAfterEmptyHistory_ProducesRecommendationsWithoutWaitingForCacheExpiry()
        {
            var emptyResult = await _subject.GetMovieRecommendations("en", CancellationToken.None);
            Assert.That(emptyResult, Is.Empty);

            SetupRequests(new List<MovieRequests>
            {
                MovieRequest(1, DateTime.UtcNow)
            }, new List<ChildRequests>());
            _movieDbApi.Setup(x => x.GetRecommendations(1, "movie", "en", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<MovieDbSearchResult>
                {
                    new MovieDbSearchResult { Id = 101, Title = "First recommendation" }
                });
            _movieDbApi.Setup(x => x.GetSimilar(1, "movie", "en", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<MovieDbSearchResult>());

            var result = await _subject.GetMovieRecommendations("en", CancellationToken.None);

            Assert.That(result.Select(x => x.Id), Is.EqualTo(new[] { 101 }));
        }

        [Test]
        public async Task RequestsFromOtherUsers_AreNotUsedAsPersonalizationSeeds()
        {
            var ownRequest = MovieRequest(1, DateTime.UtcNow.AddMinutes(-1));
            var otherUsersRequest = new MovieRequests
            {
                Id = 2,
                TheMovieDbId = 2,
                RequestedUserId = "someone-else",
                RequestedDate = DateTime.UtcNow
            };
            SetupRequests(new List<MovieRequests> { ownRequest, otherUsersRequest }, new List<ChildRequests>());

            _movieDbApi.Setup(x => x.GetRecommendations(1, "movie", "en", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<MovieDbSearchResult>
                {
                    new MovieDbSearchResult { Id = 101, Title = "Personal" }
                });
            _movieDbApi.Setup(x => x.GetSimilar(1, "movie", "en", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<MovieDbSearchResult>());

            var result = await _subject.GetMovieRecommendations("en", CancellationToken.None);

            Assert.That(result.Select(x => x.Id), Is.EqualTo(new[] { 101 }));
            _movieDbApi.Verify(x => x.GetRecommendations(2, "movie", "en", It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task OnePreviousRequest_IsEnoughToProduceRecommendations()
        {
            SetupRequests(new List<MovieRequests>
            {
                MovieRequest(1, DateTime.UtcNow)
            }, new List<ChildRequests>());
            _movieDbApi.Setup(x => x.GetRecommendations(1, "movie", "en", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<MovieDbSearchResult>
                {
                    new MovieDbSearchResult { Id = 101, Title = "Recommendation" }
                });
            _movieDbApi.Setup(x => x.GetSimilar(1, "movie", "en", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<MovieDbSearchResult>());

            var result = await _subject.GetMovieRecommendations("en", CancellationToken.None);

            Assert.That(result.Select(x => x.Id), Is.EqualTo(new[] { 101 }));
        }

        [Test]
        public async Task SparseRecommendations_UseSimilarAsLowerWeightFallback()
        {
            SetupRequests(new List<MovieRequests>
            {
                MovieRequest(1, DateTime.UtcNow)
            }, new List<ChildRequests>());
            _movieDbApi.Setup(x => x.GetRecommendations(1, "movie", "en", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<MovieDbSearchResult>());
            _movieDbApi.Setup(x => x.GetSimilar(1, "movie", "en", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<MovieDbSearchResult>
                {
                    new MovieDbSearchResult { Id = 202, Title = "Similar" }
                });

            var result = await _subject.GetMovieRecommendations("en", CancellationToken.None);

            Assert.That(result.Select(x => x.Id), Is.EqualTo(new[] { 202 }));
        }

        [Test]
        public async Task SimilarFallbackFailure_KeepsPrimaryRecommendations()
        {
            SetupRequests(new List<MovieRequests>
            {
                MovieRequest(1, DateTime.UtcNow)
            }, new List<ChildRequests>());
            _movieDbApi.Setup(x => x.GetRecommendations(1, "movie", "en", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<MovieDbSearchResult>
                {
                    new MovieDbSearchResult { Id = 101, Title = "Primary" }
                });
            _movieDbApi.Setup(x => x.GetSimilar(1, "movie", "en", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("similar unavailable"));

            var result = await _subject.GetMovieRecommendations("en", CancellationToken.None);

            Assert.That(result.Select(x => x.Id), Is.EqualTo(new[] { 101 }));
        }

        [Test]
        public async Task CandidateRecommendedByMultipleSeeds_RanksAheadOfSingleSeedCandidates()
        {
            var now = DateTime.UtcNow;
            SetupRequests(new List<MovieRequests>
            {
                MovieRequest(1, now),
                MovieRequest(2, now.AddMinutes(-1))
            }, new List<ChildRequests>());

            _movieDbApi.Setup(x => x.GetRecommendations(1, "movie", "en", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<MovieDbSearchResult>
                {
                    new MovieDbSearchResult { Id = 100, Title = "Single", Popularity = 1000 },
                    new MovieDbSearchResult { Id = 101, Title = "Overlap", Popularity = 1 }
                });
            _movieDbApi.Setup(x => x.GetRecommendations(2, "movie", "en", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<MovieDbSearchResult>
                {
                    new MovieDbSearchResult { Id = 101, Title = "Overlap", Popularity = 1 },
                    new MovieDbSearchResult { Id = 102, Title = "Other", Popularity = 1000 }
                });
            _movieDbApi.Setup(x => x.GetSimilar(It.IsAny<int>(), "movie", "en", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<MovieDbSearchResult>());

            var result = (await _subject.GetMovieRecommendations("en", CancellationToken.None)).ToList();

            Assert.That(result[0].Id, Is.EqualTo(101));
            Assert.That(result[0].RecommendationScore, Is.GreaterThan(result[1].RecommendationScore));
        }

        [Test]
        public async Task MoreThanTenRequests_UsesOnlyTenMostRecentDistinctSeeds()
        {
            var now = DateTime.UtcNow;
            var requests = Enumerable.Range(1, 12)
                .Select(index => MovieRequest(index, now.AddMinutes(-index)))
                .ToList();
            SetupRequests(requests, new List<ChildRequests>());

            for (var seedId = 1; seedId <= 10; seedId++)
            {
                var capturedSeedId = seedId;
                _movieDbApi.Setup(x => x.GetRecommendations(capturedSeedId, "movie", "en", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<MovieDbSearchResult>
                    {
                        new MovieDbSearchResult { Id = 1000 + capturedSeedId, Title = $"Recommendation {capturedSeedId}" }
                    });
                _movieDbApi.Setup(x => x.GetSimilar(capturedSeedId, "movie", "en", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<MovieDbSearchResult>());
            }

            var result = await _subject.GetMovieRecommendations("en", CancellationToken.None);

            Assert.That(result, Has.Count.EqualTo(10));
            _movieDbApi.Verify(x => x.GetRecommendations(11, "movie", "en", It.IsAny<CancellationToken>()), Times.Never);
            _movieDbApi.Verify(x => x.GetRecommendations(12, "movie", "en", It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task PreviouslyRequestedCandidate_IsExcludedEvenWhenNotUsedAsASeed()
        {
            var now = DateTime.UtcNow;
            var requests = Enumerable.Range(1, 11)
                .Select(index => MovieRequest(index, now.AddMinutes(-index)))
                .ToList();
            requests.Add(MovieRequest(999, now.AddYears(-1)));
            SetupRequests(requests, new List<ChildRequests>());

            foreach (var seed in requests.OrderByDescending(x => x.RequestedDate).Take(10))
            {
                _movieDbApi.Setup(x => x.GetRecommendations(seed.TheMovieDbId, "movie", "en", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<MovieDbSearchResult>
                    {
                        new MovieDbSearchResult { Id = 999, Title = "Already requested" },
                        new MovieDbSearchResult { Id = 5000 + seed.TheMovieDbId, Title = "New" }
                    });
                _movieDbApi.Setup(x => x.GetSimilar(seed.TheMovieDbId, "movie", "en", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<MovieDbSearchResult>());
            }

            var result = await _subject.GetMovieRecommendations("en", CancellationToken.None);

            Assert.That(result.Any(x => x.Id == 999), Is.False);
            Assert.That(result, Is.Not.Empty);
        }

        [Test]
        public async Task EmptyProviderResult_IsNotPinnedInPersonalizedCache()
        {
            SetupRequests(new List<MovieRequests>
            {
                MovieRequest(1, DateTime.UtcNow)
            }, new List<ChildRequests>());

            _movieDbApi.SetupSequence(x => x.GetRecommendations(1, "movie", "en", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("provider unavailable"))
                .ReturnsAsync(new List<MovieDbSearchResult>
                {
                    new MovieDbSearchResult { Id = 101, Title = "Recovered" }
                });
            _movieDbApi.Setup(x => x.GetSimilar(1, "movie", "en", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<MovieDbSearchResult>());

            var first = await _subject.GetMovieRecommendations("en", CancellationToken.None);
            var second = await _subject.GetMovieRecommendations("en", CancellationToken.None);

            Assert.That(first, Is.Empty);
            Assert.That(second.Select(x => x.Id), Is.EqualTo(new[] { 101 }));
            _mocker.GetMock<ICacheService>().Verify(x => x.Remove(
                It.Is<string>(key => key.StartsWith("PersonalizedRecommendations|", StringComparison.Ordinal))), Times.Once);
        }

        [Test]
        public async Task TvOnlyHistory_WorksWithoutMovieSeeds()
        {
            SetupRequests(new List<MovieRequests>(), new List<ChildRequests>
            {
                TvRequest(10, 300, DateTime.UtcNow)
            });
            _movieDbApi.Setup(x => x.GetRecommendations(300, "tv", "en", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<MovieDbSearchResult>
                {
                    new MovieDbSearchResult { Id = 301, Title = "TV Recommendation" }
                });
            _movieDbApi.Setup(x => x.GetSimilar(300, "tv", "en", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<MovieDbSearchResult>());

            var result = await _subject.GetTvRecommendations("en", CancellationToken.None);

            Assert.That(result.Select(x => x.Id), Is.EqualTo(new[] { 301 }));
        }

        private void SetupRequests(List<MovieRequests> movies, List<ChildRequests> tv)
        {
            _movieRequests.Setup(x => x.GetAll()).Returns(movies.AsQueryable().BuildMock());
            _tvRequests.Setup(x => x.GetChild(UserId)).Returns(tv.AsQueryable().BuildMock());
        }

        private static MovieRequests MovieRequest(int id, DateTime requestedDate)
        {
            return new MovieRequests
            {
                Id = id,
                TheMovieDbId = id,
                RequestedUserId = UserId,
                RequestedDate = requestedDate
            };
        }

        private static ChildRequests TvRequest(int requestId, int theMovieDbId, DateTime requestedDate)
        {
            return new ChildRequests
            {
                Id = requestId,
                RequestedUserId = UserId,
                RequestedDate = requestedDate,
                ParentRequest = new TvRequests
                {
                    Id = requestId,
                    ExternalProviderId = theMovieDbId
                }
            };
        }
    }
}
