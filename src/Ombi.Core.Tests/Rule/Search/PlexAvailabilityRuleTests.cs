using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using NUnit.Framework;
using Ombi.Core.Models.Search;
using Ombi.Core.Rule.Rules.Search;
using Ombi.Core.Services;
using Ombi.Core.Settings;
using Ombi.Core.Settings.Models.External;
using Ombi.Helpers;
using Ombi.Store.Entities;
using Ombi.Store.Repository;
using Ombi.Store.Repository.Requests;

namespace Ombi.Core.Tests.Rule.Search
{
    [TestFixture]
    public class PlexAvailabilityRuleTests
    {
        private Mock<IPlexContentRepository> PlexContentRepo { get; set; }
        private Mock<ILogger<PlexAvailabilityRule>> LoggerMock { get; set; }
        private Mock<IFeatureService> FeatureMock { get; set; }
        private Mock<ISettingsService<PlexSettings>> PlexSettingsMock { get; set; }
        private PlexAvailabilityRule Rule { get; set; }

        [SetUp]
        public void SetUp()
        {
            PlexContentRepo = new Mock<IPlexContentRepository>();
            LoggerMock = new Mock<ILogger<PlexAvailabilityRule>>();
            FeatureMock = new Mock<IFeatureService>();
            PlexSettingsMock = new Mock<ISettingsService<PlexSettings>>();
            Rule = new PlexAvailabilityRule(
                PlexContentRepo.Object,
                LoggerMock.Object,
                PlexSettingsMock.Object,
                FeatureMock.Object);
        }

        [Test]
        public async Task TvShow_StandaloneSeasonMappedToAnthologySeason_IsFullyAvailable()
        {
            var plexSeries = new PlexServerContent
            {
                Id = 552121,
                Type = MediaType.Series,
                Title = "Monster (2022)",
                TheMovieDbId = "335840",
                TvDbId = "389492",
                ImdbId = "tt13207736",
                Url = "http://plex/monster"
            };

            var titles = new[]
            {
                "Bloodbath",
                "Strong Kitty",
                "Whack Job!",
                "R.I.P (Rest in Pestilence) Abby Borden",
                "41",
                "Bed and Breakfast",
                "The Trial of the Century",
                "Carnival"
            };

            var dahmerTitles = new[]
            {
                "Episode One",
                "Please Don't Go",
                "Doin' a Dahmer",
                "The Good Boy Box",
                "Blood on Their Hands",
                "Silenced",
                "Cassandra",
                "Lionel",
                "The Bogeyman",
                "God of Forgiveness, God of Vengeance"
            };

            var plexEpisodes = dahmerTitles
                .Select((title, index) => (IMediaServerEpisode)new PlexEpisode
                {
                    SeasonNumber = 1,
                    EpisodeNumber = index + 1,
                    Title = title,
                    Series = plexSeries
                })
                .Concat(titles.Select((title, index) => (IMediaServerEpisode)new PlexEpisode
                {
                    SeasonNumber = 4,
                    EpisodeNumber = index + 1,
                    Title = title,
                    Series = plexSeries
                }))
                .ToList();
            plexSeries.Episodes = plexEpisodes;

            PlexContentRepo
                .Setup(x => x.GetByType(It.IsAny<string>(), It.IsAny<ProviderType>(), MediaType.Series))
                .ReturnsAsync((PlexServerContent)null);
            PlexContentRepo.Setup(x => x.GetAllEpisodes())
                .Returns(plexEpisodes.AsQueryable().BuildMock());
            PlexContentRepo.Setup(x => x.GetAll())
                .Returns(new List<PlexServerContent> { plexSeries }.AsQueryable().BuildMock());

            var search = new SearchTvShowViewModel
            {
                Id = 299939,
                TheMovieDbId = "299939",
                Title = "Monster: The Lizzie Borden Story",
                SeasonRequests = new List<SeasonRequests>
                {
                    new SeasonRequests
                    {
                        SeasonNumber = 1,
                        Episodes = titles
                            .Select((title, index) => new EpisodeRequests
                            {
                                EpisodeNumber = index + 1,
                                Title = title
                            })
                            .ToList()
                    }
                }
            };

            var result = await Rule.Execute(search);

            Assert.That(result.Success, Is.True);
            Assert.That(search.Available, Is.True);
            Assert.That(search.FullyAvailable, Is.True);
            Assert.That(search.PartlyAvailable, Is.False);
            Assert.That(search.SeasonRequests[0].SeasonAvailable, Is.True);
            Assert.That(search.SeasonRequests[0].Episodes.All(x => x.Available), Is.True);
        }
        [Test]
        public async Task TvShow_DuplicateProviderIdRows_OnlyUsesEpisodesFromMatchedContentRow()
        {
            var matchedSeries = new PlexServerContent
            {
                Id = 100,
                Type = MediaType.Series,
                Title = "Matched Series",
                TheMovieDbId = "12345",
                Url = "http://plex/matched"
            };
            var duplicateSeries = new PlexServerContent
            {
                Id = 200,
                Type = MediaType.Series,
                Title = "Stale Duplicate",
                TheMovieDbId = "12345",
                Url = "http://plex/duplicate"
            };

            var duplicateEpisode = new PlexEpisode
            {
                SeasonNumber = 1,
                EpisodeNumber = 1,
                Title = "Pilot",
                Series = duplicateSeries
            };

            matchedSeries.Episodes = new List<IMediaServerEpisode>();
            duplicateSeries.Episodes = new List<IMediaServerEpisode> { duplicateEpisode };

            PlexContentRepo
                .Setup(x => x.GetByType("12345", ProviderType.TheMovieDbId, MediaType.Series))
                .ReturnsAsync(matchedSeries);
            PlexContentRepo.Setup(x => x.GetAllEpisodes())
                .Returns(new List<IMediaServerEpisode> { duplicateEpisode }.AsQueryable().BuildMock());
            PlexContentRepo.Setup(x => x.GetAll())
                .Returns(new List<PlexServerContent> { matchedSeries, duplicateSeries }.AsQueryable().BuildMock());

            var search = new SearchTvShowViewModel
            {
                Id = 12345,
                TheMovieDbId = "12345",
                Title = "Matched Series",
                SeasonRequests = new List<SeasonRequests>
                {
                    new SeasonRequests
                    {
                        SeasonNumber = 1,
                        Episodes = new List<EpisodeRequests>
                        {
                            new EpisodeRequests
                            {
                                EpisodeNumber = 1,
                                Title = "Pilot",
                                AirDate = System.DateTime.Today.AddDays(-1)
                            }
                        }
                    }
                }
            };

            var result = await Rule.Execute(search);

            Assert.That(result.Success, Is.True);
            Assert.That(search.SeasonRequests[0].Episodes[0].Available, Is.False);
            Assert.That(search.FullyAvailable, Is.False);
        }

    }
}
