using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Ombi.Api;
using Ombi.Api.External.MediaServers.Plex;
using Ombi.Api.External.MediaServers.Plex.Models;
using Ombi.Api.External.MediaServers.Plex.Models.Community;
using Ombi.Core.Settings;
using Ombi.Core.Settings.Models.External;
using Ombi.Settings.Settings.Models;

namespace Ombi.Core.Tests.Api
{
    [TestFixture]
    public class PlexApiHistoryTests
    {

        [Test]
        public async Task GetAllFriends_DoesNotDeserializeHttpErrorBodiesAsCommunityResponse()
        {
            var api = new Mock<IApi>();
            Request captured = null;
            api.Setup(x => x.Request<PlexCommunityFriendsResponse>(It.IsAny<Request>(), It.IsAny<CancellationToken>()))
                .Callback<Request, CancellationToken>((request, _) => captured = request)
                .ReturnsAsync(new PlexCommunityFriendsResponse());

            var subject = CreateSubject(api.Object);

            await subject.GetAllFriends("plex-token", CancellationToken.None);

            Assert.That(captured, Is.Not.Null);
            Assert.That(captured.DeserializeErrorResponse, Is.False,
                "Plex Community HTTP failures should not be parsed as successful GraphQL response models.");
        }

        [Test]
        public async Task GetWatchlistForUser_DoesNotDeserializeHttpErrorBodiesAsCommunityResponse()
        {
            var api = new Mock<IApi>();
            Request captured = null;
            api.Setup(x => x.Request<PlexCommunityWatchlistResponse>(It.IsAny<Request>(), It.IsAny<CancellationToken>()))
                .Callback<Request, CancellationToken>((request, _) => captured = request)
                .ReturnsAsync(new PlexCommunityWatchlistResponse());

            var subject = CreateSubject(api.Object);

            await subject.GetWatchlistForUser("plex-token", "plex-user-id", null, CancellationToken.None);

            Assert.That(captured, Is.Not.Null);
            Assert.That(captured.DeserializeErrorResponse, Is.False,
                "Plex Community HTTP failures should not be parsed as successful GraphQL response models.");
        }

        private static PlexApi CreateSubject(IApi api)
        {
            var customization = new Mock<ISettingsService<CustomizationSettings>>();
            customization.Setup(x => x.GetSettings())
                .Returns(new CustomizationSettings { ApplicationName = "Ombi" });

            var plexSettings = new Mock<ISettingsService<PlexSettings>>();
            plexSettings.Setup(x => x.GetSettingsAsync())
                .ReturnsAsync(new PlexSettings { InstallId = Guid.NewGuid() });

            return new PlexApi(api, customization.Object, plexSettings.Object);
        }

        [Test]
        public async Task GetHistory_PreservesHttpFailureStatusForCaller()
        {
            var api = new Mock<IApi>();
            Request captured = null;
            api.Setup(x => x.Request<PlexContainer>(It.IsAny<Request>(), It.IsAny<CancellationToken>()))
                .Callback<Request, CancellationToken>((request, _) => captured = request)
                .ReturnsAsync(new PlexContainer());

            var customization = new Mock<ISettingsService<CustomizationSettings>>();
            customization.Setup(x => x.GetSettings())
                .Returns(new CustomizationSettings { ApplicationName = "Ombi" });

            var plexSettings = new Mock<ISettingsService<PlexSettings>>();
            plexSettings.Setup(x => x.GetSettingsAsync())
                .ReturnsAsync(new PlexSettings { InstallId = Guid.NewGuid() });

            var subject = new PlexApi(api.Object, customization.Object, plexSettings.Object);

            await subject.GetHistory("plex-token", "https://localhost:32400", "76124");

            Assert.That(captured, Is.Not.Null);
            Assert.That(captured.ThrowOnErrorStatus, Is.True,
                "Plex history HTTP failures must not collapse into a default PlexContainer.");
            Assert.That(captured.IgnoreErrors, Is.True,
                "Media Cleanup owns contextual logging for optional play-history failures.");
        }
    }
}
