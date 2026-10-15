using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MockQueryable.Moq;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Ombi.Api.External.ExternalApis.Radarr;
using Ombi.Api.External.ExternalApis.Radarr.Models;
using Ombi.Core.Senders;
using Ombi.Core.Settings;
using Ombi.Settings.Settings.Models.External;
using Ombi.Store.Entities;
using Ombi.Store.Entities.Requests;
using Ombi.Store.Repository;

namespace Ombi.Core.Tests.Senders
{
    [TestFixture]
    public class MovieSenderTests
    {
        [TestCase(true, true)]
        [TestCase(false, false)]
        public async Task ExistingMovie_ZeroOverride_ReprofilesOnlyForExplicitDefaultReset(
            bool resetToDefault,
            bool expectProfileChange)
        {
            var mocker = new AutoMocker();
            var settings = new RadarrSettings
            {
                Enabled = true,
                ApiKey = "key",
                Ip = "localhost",
                Port = 7878,
                DefaultQualityProfile = "1",
                DefaultRootPath = "/movies",
                AddOnly = false,
                MinimumAvailability = "announced"
            };
            mocker.GetMock<ISettingsService<RadarrSettings>>()
                .Setup(x => x.GetSettingsAsync())
                .ReturnsAsync(settings);
            mocker.GetMock<IRepository<UserQualityProfiles>>()
                .Setup(x => x.GetAll())
                .Returns(new List<UserQualityProfiles>().AsQueryable().BuildMock());

            var existingMovie = new MovieResponse
            {
                id = 10,
                tmdbId = 123,
                qualityProfileId = 2,
                monitored = true
            };
            var radarr = mocker.GetMock<IRadarrV3Api>();
            radarr.Setup(x => x.GetMovies(settings.ApiKey, settings.FullUri))
                .ReturnsAsync(new List<MovieResponse> { existingMovie });
            radarr.Setup(x => x.UpdateMovie(It.IsAny<MovieResponse>(), settings.ApiKey, settings.FullUri))
                .ReturnsAsync((MovieResponse movie, string _, string __) => movie);
            radarr.Setup(x => x.MovieSearch(It.IsAny<int[]>(), settings.ApiKey, settings.FullUri))
                .ReturnsAsync(true);

            var request = new MovieRequests
            {
                TheMovieDbId = 123,
                Title = "Test Movie",
                ReleaseDate = new DateTime(2026, 1, 1),
                RequestedUserId = "user",
                QualityOverride = 0,
                ResetQualityProfileToDefault = resetToDefault
            };

            var subject = mocker.CreateInstance<MovieSender>();
            var result = await subject.Send(request, false);

            Assert.That(result.Success, Is.True);
            radarr.Verify(
                x => x.UpdateMovie(
                    It.Is<MovieResponse>(movie => movie.qualityProfileId == 1),
                    settings.ApiKey,
                    settings.FullUri),
                expectProfileChange ? Times.Once() : Times.Never());
            radarr.Verify(
                x => x.MovieSearch(
                    It.Is<int[]>(ids => ids.SequenceEqual(new[] { existingMovie.id })),
                    settings.ApiKey,
                    settings.FullUri),
                expectProfileChange ? Times.Once() : Times.Never());
        }
    }
}
