using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Moq;
using NUnit.Framework;
using Ombi.Api;
using TmdbApiClient = global::Ombi.Api.External.ExternalApis.TheMovieDb.TheMovieDbApi;
using Ombi.Api.External.ExternalApis.TheMovieDb.Models;
using Ombi.Core.Settings;
using Ombi.Core.Settings.Models.External;

namespace Ombi.Schedule.Tests
{
    [TestFixture]
    public class TheMovieDbApiSecurityTests
    {
        [Test]
        public async Task Find_SendsApiKeyOnlyOverHttps()
        {
            var api = new Mock<IApi>();
            Request captured = null;
            api.Setup(x => x.Request<FindResult>(It.IsAny<Request>(), It.IsAny<CancellationToken>()))
                .Callback<Request, CancellationToken>((request, _) => captured = request)
                .ReturnsAsync(new FindResult());

            var subject = CreateSubject(api.Object);
            await subject.Find("tt1234567", ExternalSource.imdb_id);

            AssertSecureTmdbRequest(captured);
            Assert.That(captured.FullUri.AbsolutePath, Is.EqualTo("/3/find/tt1234567"));
        }

        [Test]
        public async Task GetFullMovieInfo_WithQueryParameters_UsesHttps()
        {
            var api = new Mock<IApi>();
            Request captured = null;
            api.Setup(x => x.Request<FullMovieInfo>(It.IsAny<Request>(), It.IsAny<CancellationToken>()))
                .Callback<Request, CancellationToken>((request, _) => captured = request)
                .ReturnsAsync(new FullMovieInfo());

            var subject = CreateSubject(api.Object);
            await subject.GetFullMovieInfo(123, CancellationToken.None, "en");

            AssertSecureTmdbRequest(captured);
            Assert.That(captured.FullUri.AbsolutePath, Is.EqualTo("/3/movie/123"));
        }

        private static TmdbApiClient CreateSubject(IApi api)
        {
            return new TmdbApiClient(Mock.Of<IMapper>(), api,
                Mock.Of<ISettingsService<TheMovieDbSettings>>());
        }

        private static void AssertSecureTmdbRequest(Request request)
        {
            Assert.That(request, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(request.FullUri.Scheme, Is.EqualTo(Uri.UriSchemeHttps));
                Assert.That(request.FullUri.Host, Is.EqualTo("api.themoviedb.org"));
                Assert.That(request.FullUri.IsDefaultPort, Is.True);
                Assert.That(request.FullUri.Query, Does.Contain("api_key="));
            });
        }
    }
}
