using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.AutoMock;
using MockQueryable.Moq;
using NUnit.Framework;
using AutoMapper;
using Ombi.Api;
using Ombi.Api.External.ExternalApis.TheMovieDb;
using Ombi.Api.External.ExternalApis.TheMovieDb.Models;
using Ombi.Core.Settings;
using Ombi.Core.Settings.Models.External;
using Ombi.Schedule.Jobs.Ombi;
using Ombi.Store.Entities;
using Ombi.Store.Repository;

namespace Ombi.Schedule.Tests
{
    [TestFixture]
    public class RefreshMetadataTests
    {
        private AutoMocker _mocker;
        private RefreshMetadata _subject;

        [SetUp]
        public void Setup()
        {
            _mocker = new AutoMocker();
            _subject = _mocker.CreateInstance<RefreshMetadata>();
        }

        [TestCase(null, false)]
        [TestCase("", false)]
        [TestCase("tt1234567", false)]
        [TestCase("0", false)]
        [TestCase("-1", false)]
        [TestCase("98765", true)]
        public void HasTvDb_RequiresPositiveNumericTvDbId(string tvDbId, bool expected)
        {
            var content = new PlexServerContent { TvDbId = tvDbId };

            Assert.That(content.HasTvDb, Is.EqualTo(expected));
        }

        [TestCase(null, false)]
        [TestCase("", false)]
        [TestCase(" ", false)]
        [TestCase("0", false)]
        [TestCase("-1", false)]
        [TestCase("not-a-number", false)]
        [TestCase("tt1234567", false)]
        [TestCase("42", true)]
        public void HasTheMovieDb_RequiresPositiveNumericTmdbId(string theMovieDbId, bool expected)
        {
            var content = new PlexServerContent { TheMovieDbId = theMovieDbId };

            Assert.That(content.HasTheMovieDb, Is.EqualTo(expected));
        }

        [Test]
        public async Task StartPlex_WithLegacyZeroTmdbId_RepairsPersistedTmdbId()
        {
            var show = new PlexServerContent
            {
                Title = "Example Show",
                Type = MediaType.Series,
                TheMovieDbId = "0",
                ImdbId = "tt1234567",
                TvDbId = "98765"
            };

            _mocker.GetMock<IPlexContentRepository>()
                .Setup(x => x.GetAll())
                .Returns(new List<PlexServerContent> { show }.AsQueryable().BuildMock());
            _mocker.GetMock<IPlexContentRepository>()
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);
            _mocker.GetMock<IMovieDbApi>()
                .Setup(x => x.Find("98765", ExternalSource.tvdb_id))
                .ReturnsAsync(new FindResult
                {
                    tv_results = new[] { new TvResults { id = 42 } }
                });

            await InvokeStartPlex(new PlexSettings());

            Assert.That(show.TheMovieDbId, Is.EqualTo("42"));
            Assert.That(show.HasTheMovieDb, Is.True);
            _mocker.GetMock<IMovieDbApi>()
                .Verify(x => x.Find("98765", ExternalSource.tvdb_id), Times.Once);
        }

        [Test]
        public async Task StartPlex_WithLegacyImdbValueStoredAsTvDbId_RepairsPersistedTvDbId()
        {
            var show = new PlexServerContent
            {
                Title = "Example Show",
                Type = MediaType.Series,
                TheMovieDbId = "42",
                ImdbId = "tt1234567",
                TvDbId = "tt1234567"
            };

            _mocker.GetMock<IPlexContentRepository>()
                .Setup(x => x.GetAll())
                .Returns(new List<PlexServerContent> { show }.AsQueryable().BuildMock());
            _mocker.GetMock<IPlexContentRepository>()
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);
            _mocker.GetMock<IMovieDbApi>()
                .Setup(x => x.GetTvExternals(42))
                .ReturnsAsync(new TvExternals
                {
                    imdb_id = "tt1234567",
                    tvdb_id = 98765
                });

            await InvokeStartPlex(new PlexSettings());

            Assert.That(show.TvDbId, Is.EqualTo("98765"));
            Assert.That(show.HasTvDb, Is.True);
            _mocker.GetMock<IPlexContentRepository>()
                .Verify(x => x.UpdateWithoutSave(show), Times.Once);
        }

        [Test]
        public async Task GetTvDbId_WithImdbFallback_ReturnsTvDbId()
        {
            _mocker.GetMock<IMovieDbApi>()
                .Setup(x => x.Find("tt1234567", ExternalSource.imdb_id))
                .ReturnsAsync(new FindResult
                {
                    tv_results = new[] { new TvResults { id = 42 } }
                });
            _mocker.GetMock<IMovieDbApi>()
                .Setup(x => x.GetTvExternals(42))
                .ReturnsAsync(new TvExternals
                {
                    imdb_id = "tt1234567",
                    tvdb_id = 98765
                });

            var result = await InvokeGetTvDbId(false, true, string.Empty, "tt1234567", "Example Show");

            Assert.That(result, Is.EqualTo("98765"));
        }

        [Test]
        public async Task GetTvDbId_WithImdbFallbackAndNoTvDbId_ReturnsEmpty()
        {
            _mocker.GetMock<IMovieDbApi>()
                .Setup(x => x.Find("tt1234567", ExternalSource.imdb_id))
                .ReturnsAsync(new FindResult
                {
                    tv_results = new[] { new TvResults { id = 42 } }
                });
            _mocker.GetMock<IMovieDbApi>()
                .Setup(x => x.GetTvExternals(42))
                .ReturnsAsync(new TvExternals
                {
                    imdb_id = "tt1234567",
                    tvdb_id = 0
                });

            var result = await InvokeGetTvDbId(false, true, string.Empty, "tt1234567", "Example Show");

            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetTvDbId_WithImdbFallbackAndNoTmdbMatch_ReturnsEmptyWithoutExternalLookup()
        {
            _mocker.GetMock<IMovieDbApi>()
                .Setup(x => x.Find("tt1234567", ExternalSource.imdb_id))
                .ReturnsAsync(new FindResult { tv_results = System.Array.Empty<TvResults>() });

            var result = await InvokeGetTvDbId(false, true, string.Empty, "tt1234567", "Example Show");

            Assert.That(result, Is.Empty);
            _mocker.GetMock<IMovieDbApi>()
                .Verify(x => x.GetTvExternals(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task GetTvDbId_WithTmdbIdAndZeroTvDbId_ReturnsEmpty()
        {
            _mocker.GetMock<IMovieDbApi>()
                .Setup(x => x.GetTvExternals(42))
                .ReturnsAsync(new TvExternals { tvdb_id = 0 });

            var result = await InvokeGetTvDbId(true, false, "42", string.Empty, "Example Show");

            Assert.That(result, Is.Empty);
        }


        [Test]
        public async Task GetTheMovieDbId_WithMissingImdbValue_DoesNotCallFind()
        {
            var result = await _subject.GetTheMovieDbId(false, true, string.Empty, "   ", "Example Movie", true);

            Assert.That(result, Is.Empty);
            _mocker.GetMock<IMovieDbApi>()
                .Verify(x => x.Find(It.IsAny<string>(), It.IsAny<ExternalSource>()), Times.Never);
        }

        [Test]
        public async Task GetTheMovieDbId_WithMissingTvDbValue_DoesNotCallFind()
        {
            var result = await _subject.GetTheMovieDbId(true, false, "   ", string.Empty, "Example Show", false);

            Assert.That(result, Is.Empty);
            _mocker.GetMock<IMovieDbApi>()
                .Verify(x => x.Find(It.IsAny<string>(), It.IsAny<ExternalSource>()), Times.Never);
        }

        [Test]
        public async Task TheMovieDbFind_WithBlankExternalId_DoesNotSendRequest()
        {
            var api = new Mock<IApi>();
            var subject = new Ombi.Api.External.ExternalApis.TheMovieDb.TheMovieDbApi(
                Mock.Of<IMapper>(),
                api.Object,
                Mock.Of<ISettingsService<TheMovieDbSettings>>());

            var result = await subject.Find("   ", ExternalSource.imdb_id);

            Assert.That(result, Is.Not.Null);
            api.Verify(x => x.Request<FindResult>(It.IsAny<Request>(), It.IsAny<CancellationToken>()), Times.Never);
        }


        [Test]
        public async Task TheMovieDbMediaEndpoints_WithInvalidTmdbIds_DoNotSendRequests()
        {
            var api = new Mock<IApi>();
            var subject = new Ombi.Api.External.ExternalApis.TheMovieDb.TheMovieDbApi(
                Mock.Of<IMapper>(),
                api.Object,
                Mock.Of<ISettingsService<TheMovieDbSettings>>());

            var movieInfo = await subject.GetMovieInformation(0);
            var fullMovieInfo = await subject.GetFullMovieInfo(-1, CancellationToken.None, "en");
            var externals = await subject.GetTvExternals(0);
            var similar = await subject.SimilarMovies(-1, "en");
            var movieInfoWithExtras = await subject.GetMovieInformationWithExtraInfo(0);
            var tvInfo = await subject.GetTVInfo("0");
            var malformedTvInfo = await subject.GetTVInfo("not-a-number");
            var season = await subject.GetSeasonEpisodes(0, 1, CancellationToken.None);
            var movieProviders = await subject.GetMovieWatchProviders(0, CancellationToken.None);
            var tvProviders = await subject.GetTvWatchProviders(-1, CancellationToken.None);
            var tvImages = await subject.GetTvImages("0", CancellationToken.None);
            var movieImages = await subject.GetMovieImages("   ", CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(movieInfo, Is.Not.Null);
                Assert.That(fullMovieInfo, Is.Not.Null);
                Assert.That(externals, Is.Not.Null);
                Assert.That(similar, Is.Empty);
                Assert.That(movieInfoWithExtras, Is.Not.Null);
                Assert.That(tvInfo, Is.Null);
                Assert.That(malformedTvInfo, Is.Null);
                Assert.That(season?.episodes, Is.Empty);
                Assert.That(movieProviders, Is.Not.Null);
                Assert.That(tvProviders, Is.Not.Null);
                Assert.That(tvImages, Is.Not.Null);
                Assert.That(movieImages, Is.Not.Null);
            });

            Assert.That(api.Invocations, Is.Empty);
        }

        private async Task InvokeStartPlex(PlexSettings settings)
        {
            var method = typeof(RefreshMetadata).GetMethod("StartPlex", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null);

            var task = (Task)method.Invoke(_subject, new object[] { settings });
            await task;
        }

        private async Task<string> InvokeGetTvDbId(bool hasTheMovieDb, bool hasImdb, string theMovieDbId, string imdbId, string title)
        {
            var method = typeof(RefreshMetadata).GetMethod("GetTvDbId", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null);

            var task = (Task<string>)method.Invoke(_subject, new object[]
            {
                hasTheMovieDb,
                hasImdb,
                theMovieDbId,
                imdbId,
                title
            });

            return await task;
        }
    }
}
