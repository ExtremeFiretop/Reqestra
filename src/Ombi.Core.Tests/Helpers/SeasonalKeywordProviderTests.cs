using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Ombi.Api.External.ExternalApis.TheMovieDb;
using Ombi.Api.External.ExternalApis.TheMovieDb.Models;
using Ombi.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Ombi.Core.Tests.Helpers
{
    [TestFixture]
    public class SeasonalKeywordProviderTests
    {
        [Test]
        public async Task ResolveAsync_UsesValidRemoteKeywordExpressionWithoutFallbackLookup()
        {
            using var client = CreateHttpClient(HttpStatusCode.OK, " 123 | 456, 789 \n");
            var movieApi = new Mock<IMovieDbApi>();
            var provider = CreateProvider(client, movieApi.Object);

            var result = await provider.ResolveAsync(new DateTime(2026, 10, 18), CancellationToken.None);

            Assert.That(result, Is.EqualTo("123|456,789"));
            movieApi.Verify(x => x.SearchKeyword(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task ResolveAsync_UsesHalloweenFallbackWhenRemoteSourceIsEmptyInOctober()
        {
            using var client = CreateHttpClient(HttpStatusCode.OK, "\n");
            var movieApi = new Mock<IMovieDbApi>();
            movieApi.Setup(x => x.SearchKeyword("halloween"))
                .ReturnsAsync(new List<TheMovidDbKeyValue>
                {
                    new TheMovidDbKeyValue { Id = 3335, Name = "halloween" },
                });
            var provider = CreateProvider(client, movieApi.Object);

            var result = await provider.ResolveAsync(new DateTime(2026, 10, 18), CancellationToken.None);

            Assert.That(result, Is.EqualTo("3335"));
        }

        [Test]
        public async Task ResolveAsync_UsesChristmasFallbackWhenRemoteSourceIsInvalidInDecember()
        {
            using var client = CreateHttpClient(HttpStatusCode.OK, "not-a-keyword-expression");
            var movieApi = new Mock<IMovieDbApi>();
            movieApi.Setup(x => x.SearchKeyword("christmas"))
                .ReturnsAsync(new List<TheMovidDbKeyValue>
                {
                    new TheMovidDbKeyValue { Id = 207317, Name = "christmas" },
                });
            var provider = CreateProvider(client, movieApi.Object);

            var result = await provider.ResolveAsync(new DateTime(2026, 12, 10), CancellationToken.None);

            Assert.That(result, Is.EqualTo("207317"));
        }

        [Test]
        public async Task ResolveAsync_UsesFallbackWhenRemoteSourceFails()
        {
            using var client = CreateHttpClient(HttpStatusCode.ServiceUnavailable, string.Empty);
            var movieApi = new Mock<IMovieDbApi>();
            movieApi.Setup(x => x.SearchKeyword("halloween"))
                .ReturnsAsync(new List<TheMovidDbKeyValue>
                {
                    new TheMovidDbKeyValue { Id = 3335, Name = "halloween" },
                });
            var provider = CreateProvider(client, movieApi.Object);

            var result = await provider.ResolveAsync(new DateTime(2026, 10, 18), CancellationToken.None);

            Assert.That(result, Is.EqualTo("3335"));
        }

        [Test]
        public async Task ResolveAsync_ReturnsNoSeasonalDefinitionOutsideFallbackWindows()
        {
            using var client = CreateHttpClient(HttpStatusCode.OK, string.Empty);
            var movieApi = new Mock<IMovieDbApi>();
            var provider = CreateProvider(client, movieApi.Object);

            var result = await provider.ResolveAsync(new DateTime(2026, 6, 15), CancellationToken.None);

            Assert.That(result, Is.Null);
            movieApi.Verify(x => x.SearchKeyword(It.IsAny<string>()), Times.Never);
        }

        [TestCase("123|456", "123|456")]
        [TestCase(" 123 | 456, 789 \n", "123|456,789")]
        [TestCase("\uFEFF123,456", "123,456")]
        [TestCase("123\n456", null)]
        [TestCase("123|oops", null)]
        [TestCase("0|123", null)]
        [TestCase("", null)]
        public void NormalizeKeywordExpression_ValidatesAndNormalizesRemoteContent(string input, string expected)
        {
            Assert.That(SeasonalKeywordProvider.NormalizeKeywordExpression(input), Is.EqualTo(expected));
        }

        private static SeasonalKeywordProvider CreateProvider(HttpClient client, IMovieDbApi movieApi)
        {
            return new SeasonalKeywordProvider(client, movieApi, Mock.Of<ILogger>());
        }

        private static HttpClient CreateHttpClient(HttpStatusCode statusCode, string content)
        {
            return new HttpClient(new StubHttpMessageHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(content),
                })));
        }

        private sealed class StubHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

            public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
            {
                _handler = handler;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return _handler(request, cancellationToken);
            }
        }
    }
}
