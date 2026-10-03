using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Ombi.Api;
using Ombi.Helpers;

namespace Ombi.Core.Tests.Api
{
    [TestFixture]
    public class ApiErrorResponseTests
    {
        [Test]
        public async Task Request_WhenErrorDeserializationDisabled_ReturnsDefaultWithoutDeserializingBody()
        {
            using var client = new HttpClient(new StubHandler(
                new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent("undefined")
                }));

            var subject = new Ombi.Api.Api(
                NullLogger<Ombi.Api.Api>.Instance,
                client,
                Mock.Of<ICacheService>(),
                Mock.Of<IHostEnvironment>());

            var beforeDeserializationCalled = false;
            var request = new Request(string.Empty, "https://community.plex.tv/api", HttpMethod.Post)
            {
                IgnoreBaseUrlAppend = true,
                DeserializeErrorResponse = false,
                OnBeforeDeserialization = _ => beforeDeserializationCalled = true
            };

            var result = await subject.Request<TestResponse>(request);

            Assert.That(result, Is.Null);
            Assert.That(beforeDeserializationCalled, Is.False,
                "A known non-success response should not be passed through success-model deserialization when disabled.");
        }

        [Test]
        public void Request_DefaultsToDeserializingErrorResponses()
        {
            var request = new Request(string.Empty, "https://example.test/", HttpMethod.Get);

            Assert.That(request.DeserializeErrorResponse, Is.True,
                "Existing integrations should retain error-response deserialization unless they explicitly opt out.");
        }

        private sealed class TestResponse
        {
            public string Value { get; set; }
        }

        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly HttpResponseMessage _response;

            public StubHandler(HttpResponseMessage response)
            {
                _response = response;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_response);
            }
        }
    }
}
