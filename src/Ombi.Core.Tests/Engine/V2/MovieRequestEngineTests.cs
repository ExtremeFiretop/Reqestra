using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using MockQueryable.Moq;
using Moq.AutoMock;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Ombi.Api.External.ExternalApis.TheMovieDb;
using Ombi.Core.Engine;
using Ombi.Core.Helpers;
using Ombi.Core.Models.Requests;
using Ombi.Core.Rule.Interfaces;
using Ombi.Core.Senders;
using Ombi.Core.Services;
using Ombi.Core.Settings;
using Ombi.Helpers;
using Ombi.Settings.Settings.Models;
using Ombi.Store.Entities;
using Ombi.Store.Entities.Requests;
using Ombi.Store.Repository;
using Ombi.Store.Repository.Requests;
using Ombi.Test.Common;

namespace Ombi.Core.Tests.Engine.V2
{
    [TestFixture]
    public class MovieRequestEngineTests
    {
        private MovieRequestEngine _engine;
        private Mock<IMovieRequestRepository> _movieRequestRepository;
        [SetUp]
        public void Setup()
        {
            var movieApi = new Mock<IMovieDbApi>();
            var requestService = new Mock<IRequestServiceMain>();
            _movieRequestRepository = new Mock<IMovieRequestRepository>();
            requestService.Setup(x => x.MovieRequestService).Returns(_movieRequestRepository.Object);
            var user = new Mock<ICurrentUser>();
            var notificationHelper = new Mock<INotificationHelper>();
            var rules = new Mock<IRuleEvaluator>();
            var movieSender = new Mock<IMovieSender>();
            var logger = new Mock<ILogger<MovieRequestEngine>>();
            var userManager = MockHelper.MockUserManager(new List<OmbiUser>());
            var requestLogRepo = new Mock<IRepository<RequestLog>>();
            var cache = new Mock<ICacheService>();
            var ombiSettings = new Mock<ISettingsService<OmbiSettings>>();
            var requestSubs = new Mock<IRepository<RequestSubscription>>();
            var mediaCache = new Mock<IMediaCacheService>();
            var featureService = new Mock<IFeatureService>();
            var userPlayedMovieRepository = new Mock<IUserPlayedMovieRepository>();
            var qualityProfileSelection = new Mock<IQualityProfileSelectionService>();
            _engine = new MovieRequestEngine(movieApi.Object, requestService.Object, user.Object, notificationHelper.Object, rules.Object, movieSender.Object,
                logger.Object, userManager.Object, requestLogRepo.Object, cache.Object, ombiSettings.Object, requestSubs.Object, mediaCache.Object, featureService.Object, userPlayedMovieRepository.Object, qualityProfileSelection.Object);
        }

        [Test]
        public async Task ReProcessRequest_ProfileOwner_UpdatesProfileAndResends()
        {
            var mocker = new AutoMocker();
            var user = new OmbiUser { Id = "owner-1", UserName = "owner" };
            var userManager = MockHelper.MockUserManager(new List<OmbiUser> { user });
            userManager.Setup(x => x.IsInRoleAsync(user, OmbiRoles.PowerUser)).ReturnsAsync(false);
            userManager.Setup(x => x.IsInRoleAsync(user, OmbiRoles.Admin)).ReturnsAsync(false);
            userManager.Setup(x => x.IsInRoleAsync(user, OmbiRoles.SelectQualityProfile)).ReturnsAsync(true);

            var currentUser = new Mock<ICurrentUser>();
            currentUser.Setup(x => x.Username).Returns("owner");
            currentUser.Setup(x => x.GetUser()).ReturnsAsync(user);

            var request = new MovieRequests
            {
                Id = 44,
                RequestedUserId = user.Id,
                Approved = true,
                Available = false,
                QualityOverride = 3
            };
            var movieRepository = new Mock<IMovieRequestRepository>();
            movieRepository.Setup(x => x.GetWithUser())
                .Returns(new List<MovieRequests> { request }.AsQueryable().BuildMock());
            movieRepository.Setup(x => x.Update(request)).Returns(Task.CompletedTask);

            var requestService = new Mock<IRequestServiceMain>();
            requestService.Setup(x => x.MovieRequestService).Returns(movieRepository.Object);
            requestService.Setup(x => x.TvRequestService).Returns(new Mock<ITvRequestRepository>().Object);
            requestService.Setup(x => x.MusicRequestRepository).Returns(new Mock<IMusicRequestRepository>().Object);

            mocker.Use(currentUser.Object);
            mocker.Use(userManager.Object);
            mocker.Use(requestService.Object);
            mocker.GetMock<IQualityProfileSelectionService>()
                .Setup(x => x.IsValidRadarrProfile(7, false))
                .ReturnsAsync(true);
            mocker.GetMock<IMovieSender>()
                .Setup(x => x.Send(request, false))
                .ReturnsAsync(new SenderResult { Success = true, Sent = true });

            var subject = mocker.CreateInstance<MovieRequestEngine>();
            var result = await subject.ReProcessRequest(request.Id, false, CancellationToken.None, 7);

            Assert.That(result.Result, Is.True);
            Assert.That(request.QualityOverride, Is.EqualTo(7));
            movieRepository.Verify(x => x.Update(request), Times.Once);
            mocker.GetMock<IMovieSender>().Verify(x => x.Send(request, false), Times.Once);
        }

        [Test]
        public async Task ReProcessRequest_PrivilegedUserWithoutProfile_PreservesExistingBehavior()
        {
            var mocker = new AutoMocker();
            var user = new OmbiUser { Id = "admin-1", UserName = "admin" };
            var userManager = MockHelper.MockUserManager(new List<OmbiUser> { user });
            userManager.Setup(x => x.IsInRoleAsync(user, OmbiRoles.PowerUser)).ReturnsAsync(true);

            var currentUser = new Mock<ICurrentUser>();
            currentUser.Setup(x => x.Username).Returns("admin");
            currentUser.Setup(x => x.GetUser()).ReturnsAsync(user);

            var request = new MovieRequests
            {
                Id = 46,
                RequestedUserId = "someone-else",
                Approved = true,
                Available = false,
                QualityOverride = 3
            };
            var movieRepository = new Mock<IMovieRequestRepository>();
            movieRepository.Setup(x => x.GetWithUser())
                .Returns(new List<MovieRequests> { request }.AsQueryable().BuildMock());

            var requestService = new Mock<IRequestServiceMain>();
            requestService.Setup(x => x.MovieRequestService).Returns(movieRepository.Object);
            requestService.Setup(x => x.TvRequestService).Returns(new Mock<ITvRequestRepository>().Object);
            requestService.Setup(x => x.MusicRequestRepository).Returns(new Mock<IMusicRequestRepository>().Object);

            mocker.Use(currentUser.Object);
            mocker.Use(userManager.Object);
            mocker.Use(requestService.Object);
            mocker.GetMock<IMovieSender>()
                .Setup(x => x.Send(request, false))
                .ReturnsAsync(new SenderResult { Success = true, Sent = true });

            var subject = mocker.CreateInstance<MovieRequestEngine>();
            var result = await subject.ReProcessRequest(request.Id, false, CancellationToken.None, null);

            Assert.That(result.Result, Is.True);
            Assert.That(request.QualityOverride, Is.EqualTo(3));
            movieRepository.Verify(x => x.Update(It.IsAny<MovieRequests>()), Times.Never);
            mocker.GetMock<IMovieSender>().Verify(x => x.Send(request, false), Times.Once);
        }

        [Test]
        public async Task ReProcessRequest_ProfileUserCannotRetryAnotherUsersMovie()
        {
            var mocker = new AutoMocker();
            var user = new OmbiUser { Id = "owner-1", UserName = "owner" };
            var userManager = MockHelper.MockUserManager(new List<OmbiUser> { user });
            userManager.Setup(x => x.IsInRoleAsync(user, OmbiRoles.PowerUser)).ReturnsAsync(false);
            userManager.Setup(x => x.IsInRoleAsync(user, OmbiRoles.Admin)).ReturnsAsync(false);
            userManager.Setup(x => x.IsInRoleAsync(user, OmbiRoles.SelectQualityProfile)).ReturnsAsync(true);

            var currentUser = new Mock<ICurrentUser>();
            currentUser.Setup(x => x.Username).Returns("owner");
            currentUser.Setup(x => x.GetUser()).ReturnsAsync(user);

            var request = new MovieRequests
            {
                Id = 45,
                RequestedUserId = "someone-else",
                Approved = true,
                Available = false,
                QualityOverride = 3
            };
            var movieRepository = new Mock<IMovieRequestRepository>();
            movieRepository.Setup(x => x.GetWithUser())
                .Returns(new List<MovieRequests> { request }.AsQueryable().BuildMock());

            var requestService = new Mock<IRequestServiceMain>();
            requestService.Setup(x => x.MovieRequestService).Returns(movieRepository.Object);
            requestService.Setup(x => x.TvRequestService).Returns(new Mock<ITvRequestRepository>().Object);
            requestService.Setup(x => x.MusicRequestRepository).Returns(new Mock<IMusicRequestRepository>().Object);

            mocker.Use(currentUser.Object);
            mocker.Use(userManager.Object);
            mocker.Use(requestService.Object);

            var subject = mocker.CreateInstance<MovieRequestEngine>();
            var result = await subject.ReProcessRequest(request.Id, false, CancellationToken.None, 7);

            Assert.That(result.Result, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.NoPermissions));
            Assert.That(request.QualityOverride, Is.EqualTo(3));
            movieRepository.Verify(x => x.Update(It.IsAny<MovieRequests>()), Times.Never);
            mocker.GetMock<IMovieSender>().Verify(x => x.Send(It.IsAny<MovieRequests>(), It.IsAny<bool>()), Times.Never);
        }

        [Test]
        [Ignore("Needs to be tested")]
        public async Task Get_UnavailableRequests()
        {
            _movieRequestRepository.Setup(x => x.GetWithUser()).Returns(new List<MovieRequests>
            {
                new MovieRequests
                {
                    Available = true
                },
                new MovieRequests
                {
                    Available = false,
                    Approved = false
                },
                new MovieRequests
                {
                    Available = false,
                    Approved = true,
                    Title = "Come get me"
                }
            }.AsQueryable());
            var result = await _engine.GetUnavailableRequests(100, 0, "RequestedDate", "asc");

            Assert.That(result.Total, Is.EqualTo(1));
            Assert.That(result.Collection.FirstOrDefault().Title, Is.EqualTo("Come get me"));
        }
    }
}