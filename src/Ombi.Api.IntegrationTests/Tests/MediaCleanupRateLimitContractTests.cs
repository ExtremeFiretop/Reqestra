using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Ombi.Api.IntegrationTests.Harness;

namespace Ombi.Api.IntegrationTests.Tests
{
    [TestFixture]
    public class MediaCleanupRateLimitContractTests : IntegrationTestBase
    {
        [Test]
        public async Task MutationEndpoint_IsLimitedAfterSixtyRequestsPerWindow()
        {
            for (var i = 0; i < 60; i++)
            {
                using var response = await Client.PostAsync($"/api/v1/mediacleanup/cancel/rate-limit-contract-{i}", null);
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Request {i + 1} should be admitted by the mutation limiter.");
            }

            using var rejected = await Client.PostAsync("/api/v1/mediacleanup/cancel/rate-limit-contract-rejected", null);
            Assert.That(rejected.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
            Assert.That(rejected.Headers.RetryAfter, Is.Not.Null, "Clients need a Retry-After interval.");
            var json = JObject.Parse(await rejected.Content.ReadAsStringAsync());
            Assert.That(json.Value<string>("message"), Does.Contain("Too many Media Cleanup actions"));
            Assert.That(json.Value<string>("message"), Does.Contain("not saved"));
            Assert.That(json.Value<int>("retryAfterSeconds"), Is.GreaterThan(0));
        }
    }
}
