using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Ombi.Api.External.ExternalApis.TheMovieDb;
using Ombi.Api.External.ExternalApis.TheMovieDb.Models;
using Ombi.Core.Helpers;
using Ombi.Core.Models.Requests;
using Ombi.Store.Entities.Requests;

namespace Ombi.Core.Tests
{
    [TestFixture]
    public class TvShowRequestBuilderV2Tests
    {

        [Test]
        public async Task CreateChild_StoresProviderIdentityWithoutUsingPrimaryKey()
        {
            var movieDb = new Mock<IMovieDbApi>();
            movieDb.Setup(x => x.GetTVInfo("335840", "en")).ReturnsAsync(new TvInfo
            {
                id = 335840,
                name = "Monster",
                first_air_date = "2022-09-21",
                seasons = new List<Season>(),
                ExternalIds = new ExternalIds
                {
                    ImdbId = "tt13207736",
                    TvDbId = "389492"
                }
            });

            var subject = new TvShowRequestBuilderV2(movieDb.Object);
            var result = await subject.GetShowInfo(335840, "en");

            result.CreateChild(new TvRequestViewModelV2 { TheMovieDbId = 335840 }, "user-1", RequestSource.PlexWatchlist);

            Assert.That(result.ChildRequest.Id, Is.Zero);
            Assert.That(result.ChildRequest.RequestTheMovieDbId, Is.EqualTo(335840));
            Assert.That(result.ChildRequest.RequestTvDbId, Is.EqualTo(389492));
            Assert.That(result.ChildRequest.RequestImdbId, Is.EqualTo("tt13207736"));
        }


        [Test]
        public async Task GetShowInfo_PreservesImdbId_WhenTvDbMappingIsStillMissing()
        {
            var movieDb = new Mock<IMovieDbApi>();
            movieDb.Setup(x => x.GetTVInfo("335840", "en")).ReturnsAsync(new TvInfo
            {
                id = 335840,
                name = "Monster",
                first_air_date = "2022-09-21",
                seasons = new List<Season>(),
                ExternalIds = new ExternalIds()
            });
            movieDb.Setup(x => x.GetTvExternals(335840)).ReturnsAsync(new TvExternals
            {
                imdb_id = "tt13207736",
                tvdb_id = 0
            });

            var subject = new TvShowRequestBuilderV2(movieDb.Object);
            var result = await subject.GetShowInfo(335840, "en");

            result.CreateChild(new TvRequestViewModelV2 { TheMovieDbId = 335840 }, "user-1", RequestSource.PlexWatchlist);

            Assert.That(result.ChildRequest.RequestTvDbId, Is.Zero);
            Assert.That(result.ChildRequest.RequestImdbId, Is.EqualTo("tt13207736"));
        }


        [Test]
        public async Task BuildEpisodes_CustomRequest_OnlyLoadsRequestedSeasons()
        {
            var movieDb = new Mock<IMovieDbApi>();
            movieDb.Setup(x => x.GetTVInfo("12345", "en")).ReturnsAsync(new TvInfo
            {
                id = 12345,
                name = "Test Show",
                first_air_date = "2024-01-01",
                seasons = new List<Season>
                {
                    new Season { season_number = 1 },
                    new Season { season_number = 2 },
                    new Season { season_number = 3 }
                },
                ExternalIds = new ExternalIds { TvDbId = "54321" }
            });
            movieDb.Setup(x => x.GetSeasonEpisodes(12345, 2, It.IsAny<CancellationToken>(), It.IsAny<string>()))
                .ReturnsAsync(new SeasonDetails
                {
                    season_number = 2,
                    episodes = new[]
                    {
                        new Episode { season_number = 2, episode_number = 1, name = "Requested Episode" },
                        new Episode { season_number = 2, episode_number = 2, name = "Not Requested" }
                    }
                });

            var request = new TvRequestViewModelV2
            {
                TheMovieDbId = 12345,
                Seasons = new List<SeasonsViewModel>
                {
                    new SeasonsViewModel
                    {
                        SeasonNumber = 2,
                        Episodes = new List<EpisodesViewModel>
                        {
                            new EpisodesViewModel { EpisodeNumber = 1 }
                        }
                    }
                }
            };

            var subject = await new TvShowRequestBuilderV2(movieDb.Object).GetShowInfo(12345, "en");
            subject.CreateChild(request, "user-1", RequestSource.Ombi);
            await subject.BuildEpisodes(request);

            movieDb.Verify(x => x.GetSeasonEpisodes(12345, 2, It.IsAny<CancellationToken>(), It.IsAny<string>()), Times.Once);
            movieDb.Verify(x => x.GetSeasonEpisodes(12345, 1, It.IsAny<CancellationToken>(), It.IsAny<string>()), Times.Never);
            movieDb.Verify(x => x.GetSeasonEpisodes(12345, 3, It.IsAny<CancellationToken>(), It.IsAny<string>()), Times.Never);

            Assert.That(subject.ChildRequest.SeasonRequests, Has.Count.EqualTo(1));
            Assert.That(subject.ChildRequest.SeasonRequests[0].SeasonNumber, Is.EqualTo(2));
            Assert.That(subject.ChildRequest.SeasonRequests[0].Episodes, Has.Count.EqualTo(1));
            Assert.That(subject.ChildRequest.SeasonRequests[0].Episodes[0].EpisodeNumber, Is.EqualTo(1));
        }

        [Test]
        public async Task BuildEpisodes_LatestSeason_WhenTmdbReturnsNull_DoesNotThrowOrLoadOtherSeasons()
        {
            var movieDb = new Mock<IMovieDbApi>();
            movieDb.Setup(x => x.GetTVInfo("12345", "en")).ReturnsAsync(new TvInfo
            {
                id = 12345,
                name = "Test Show",
                first_air_date = "2024-01-01",
                seasons = new List<Season>
                {
                    new Season { season_number = 1 },
                    new Season { season_number = 2 },
                    new Season { season_number = 3 }
                },
                ExternalIds = new ExternalIds { TvDbId = "54321" }
            });
            movieDb.Setup(x => x.GetSeasonEpisodes(12345, 3, It.IsAny<CancellationToken>(), It.IsAny<string>()))
                .ReturnsAsync((SeasonDetails)null);

            var request = new TvRequestViewModelV2
            {
                TheMovieDbId = 12345,
                LatestSeason = true
            };

            var subject = await new TvShowRequestBuilderV2(movieDb.Object).GetShowInfo(12345, "en");
            subject.CreateChild(request, "user-1", RequestSource.Ombi);

            await subject.BuildEpisodes(request);

            movieDb.Verify(x => x.GetSeasonEpisodes(12345, 3, It.IsAny<CancellationToken>(), It.IsAny<string>()), Times.Once);
            movieDb.Verify(x => x.GetSeasonEpisodes(12345, 1, It.IsAny<CancellationToken>(), It.IsAny<string>()), Times.Never);
            movieDb.Verify(x => x.GetSeasonEpisodes(12345, 2, It.IsAny<CancellationToken>(), It.IsAny<string>()), Times.Never);
            Assert.That(subject.ChildRequest.SeasonRequests, Is.Empty);
        }

        [Test]
        public async Task BuildEpisodes_FirstSeason_OnlyLoadsFirstRegularSeason()
        {
            var movieDb = new Mock<IMovieDbApi>();
            movieDb.Setup(x => x.GetTVInfo("12345", "en")).ReturnsAsync(new TvInfo
            {
                id = 12345,
                name = "Test Show",
                first_air_date = "2024-01-01",
                seasons = new List<Season>
                {
                    new Season { season_number = 0 },
                    new Season { season_number = 1 },
                    new Season { season_number = 2 }
                },
                ExternalIds = new ExternalIds { TvDbId = "54321" }
            });
            movieDb.Setup(x => x.GetSeasonEpisodes(12345, 1, It.IsAny<CancellationToken>(), It.IsAny<string>()))
                .ReturnsAsync(new SeasonDetails
                {
                    season_number = 1,
                    episodes = new[]
                    {
                        new Episode { season_number = 1, episode_number = 1, name = "Pilot" }
                    }
                });

            var request = new TvRequestViewModelV2
            {
                TheMovieDbId = 12345,
                FirstSeason = true
            };

            var subject = await new TvShowRequestBuilderV2(movieDb.Object).GetShowInfo(12345, "en");
            subject.CreateChild(request, "user-1", RequestSource.Ombi);
            await subject.BuildEpisodes(request);

            movieDb.Verify(x => x.GetSeasonEpisodes(12345, 1, It.IsAny<CancellationToken>(), It.IsAny<string>()), Times.Once);
            movieDb.Verify(x => x.GetSeasonEpisodes(12345, 2, It.IsAny<CancellationToken>(), It.IsAny<string>()), Times.Never);
            movieDb.Verify(x => x.GetSeasonEpisodes(12345, 0, It.IsAny<CancellationToken>(), It.IsAny<string>()), Times.Never);
            Assert.That(subject.ChildRequest.SeasonRequests, Has.Count.EqualTo(1));
            Assert.That(subject.ChildRequest.SeasonRequests[0].SeasonNumber, Is.EqualTo(1));
        }

        [Test]
        public async Task GetShowInfo_WhenTmdbReturnsNull_ReturnsNullInsteadOfThrowing()
        {
            var movieDb = new Mock<IMovieDbApi>();
            movieDb.Setup(x => x.GetTVInfo("250308", "en")).ReturnsAsync((TvInfo)null);

            var subject = new TvShowRequestBuilderV2(movieDb.Object);

            var result = await subject.GetShowInfo(250308, "en");

            Assert.That(result, Is.Null);
        }
    }
}
