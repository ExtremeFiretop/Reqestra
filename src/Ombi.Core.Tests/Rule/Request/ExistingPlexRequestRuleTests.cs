using MockQueryable.Moq;
using Moq;
using NUnit.Framework;
using Ombi.Core.Engine;
using Ombi.Core.Rule.Rules.Request;
using Ombi.Store.Entities;
using Ombi.Store.Entities.Requests;
using Ombi.Store.Repository;
using Ombi.Store.Repository.Requests;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ombi.Core.Tests.Rule.Request
{
    [TestFixture]
    public class ExistingPlexRequestRuleTests
    {
        private ExistingPlexRequestRule Rule;
        private Mock<IPlexContentRepository> PlexContentRepo;

        [SetUp]
        public void SetUp()
        {
            PlexContentRepo = new Mock<IPlexContentRepository>();
            Rule = new ExistingPlexRequestRule(PlexContentRepo.Object);
        }

        [Test]
        public async Task RequestShow_DoesNotExistAtAll_IsSuccessful()
        {
            PlexContentRepo.Setup(x => x.GetAll()).Returns(new List<PlexServerContent>().AsQueryable().BuildMock());
            var req = new ChildRequests
            {
                SeasonRequests = new List<SeasonRequests>
                {
                    new SeasonRequests
                    {
                        Episodes = new List<EpisodeRequests>
                        {
                            new EpisodeRequests
                            {
                               Id = 1,
                               EpisodeNumber = 1,
                            }
                        },
                        SeasonNumber = 1
                    }
                }
            };
            var result = await Rule.Execute(req);


            Assert.That(result.Success, Is.True);
        }

        [Test]
        public async Task RequestShow_AllEpisodesAreaRequested_IsNotSuccessful()
        {
            SetupMockData();

            var req = new ChildRequests
            {
                SeasonRequests = new List<SeasonRequests>
                {
                    new SeasonRequests
                    {
                        Episodes = new List<EpisodeRequests>
                        {
                            new EpisodeRequests
                            {
                               Id = 1,
                               EpisodeNumber = 1,
                            },
                            new EpisodeRequests
                            {
                               Id = 1,
                               EpisodeNumber = 2,
                            },
                        },
                        SeasonNumber = 1
                    }
                },
                Id = 1,
                RequestTheMovieDbId = 1,
            };
            var result = await Rule.Execute(req);


            Assert.That(result.Success, Is.False);
        }


        [Test]
        public async Task RequestShow_SomeEpisodesAreaRequested_IsSuccessful()
        {
            SetupMockData();

            var req = new ChildRequests
            {
                RequestType = RequestType.TvShow,
                SeasonRequests = new List<SeasonRequests>
                {
                    new SeasonRequests
                    {
                        Episodes = new List<EpisodeRequests>
                        {
                            new EpisodeRequests
                            {
                               Id = 1,
                               EpisodeNumber = 1,
                            },
                            new EpisodeRequests
                            {
                               Id = 2,
                               EpisodeNumber = 2,
                            },
                            new EpisodeRequests
                            {
                               Id = 3,
                               EpisodeNumber = 3,
                            },
                        },
                        SeasonNumber = 1
                    }
                },
                Id = 1,
                RequestTheMovieDbId = 1,
            };
            var result = await Rule.Execute(req);


            Assert.That(result.Success, Is.True);

            var episodes = req.SeasonRequests.SelectMany(x => x.Episodes);
            Assert.That(episodes.Count() == 1, "We didn't remove the episodes that have already been requested!");
            Assert.That(episodes.First().EpisodeNumber == 3, "We removed the wrong episode");
        }


        [Test]
        public async Task RequestShow_MatchesPlexContentByImdb_WhenTmdbIdChanges()
        {
            var content = new List<PlexServerContent>
            {
                new PlexServerContent
                {
                    Type = MediaType.Series,
                    TheMovieDbId = "299939",
                    ImdbId = "tt13207736",
                    Title = "Monster (2022)",
                    ReleaseYear = "2022",
                    Episodes = new List<IMediaServerEpisode>
                    {
                        new PlexEpisode
                        {
                            SeasonNumber = 1,
                            EpisodeNumber = 1
                        }
                    }
                }
            };
            PlexContentRepo.Setup(x => x.GetAll()).Returns(content.AsQueryable().BuildMock());

            var req = new ChildRequests
            {
                RequestType = RequestType.TvShow,
                RequestTheMovieDbId = 335840,
                RequestImdbId = "tt13207736",
                Title = "Monster",
                ReleaseYear = new System.DateTime(2022, 9, 21),
                SeasonRequests = new List<SeasonRequests>
                {
                    new SeasonRequests
                    {
                        SeasonNumber = 1,
                        Episodes = new List<EpisodeRequests>
                        {
                            new EpisodeRequests { EpisodeNumber = 1 }
                        }
                    }
                }
            };

            var result = await Rule.Execute(req);

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.EpisodesAlreadyRequested));
        }

        [Test]
        public async Task RequestShow_StandaloneSeasonMappedToAnthologySeason_IsAlreadyAvailable()
        {
            var plexSeries = new PlexServerContent
            {
                Id = 552121,
                Type = MediaType.Series,
                TheMovieDbId = "335840",
                TvDbId = "389492",
                ImdbId = "tt13207736",
                Title = "Monster (2022)",
                ReleaseYear = "2022"
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

            var plexEpisodes = titles
                .Select((title, index) => (IMediaServerEpisode)new PlexEpisode
                {
                    SeasonNumber = 4,
                    EpisodeNumber = index + 1,
                    Title = title,
                    Series = plexSeries
                })
                .ToList();
            plexSeries.Episodes = plexEpisodes;

            PlexContentRepo.Setup(x => x.GetAll())
                .Returns(new List<PlexServerContent> { plexSeries }.AsQueryable().BuildMock());
            PlexContentRepo.Setup(x => x.GetAllEpisodes())
                .Returns(plexEpisodes.AsQueryable().BuildMock());

            var request = new ChildRequests
            {
                RequestType = RequestType.TvShow,
                RequestTheMovieDbId = 299939,
                Title = "Monster: The Lizzie Borden Story",
                ReleaseYear = new System.DateTime(2026, 9, 17),
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

            var result = await Rule.Execute(request);

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.EpisodesAlreadyRequested));
            Assert.That(request.SeasonRequests[0].Episodes, Is.Empty);
        }

        [Test]
        public async Task RequestShow_NewSeasonRequest_IsSuccessful()
        {
            SetupMockData();

            var req = new ChildRequests
            {
                RequestType = RequestType.TvShow,
                SeasonRequests = new List<SeasonRequests>
                {
                    new SeasonRequests
                    {
                        Episodes = new List<EpisodeRequests>
                        {
                            new EpisodeRequests
                            {
                               Id = 1,
                               EpisodeNumber = 1,
                            },
                            new EpisodeRequests
                            {
                               Id = 2,
                               EpisodeNumber = 2,
                            },
                            new EpisodeRequests
                            {
                               Id = 3,
                               EpisodeNumber = 3,
                            },
                        },
                        SeasonNumber = 2
                    }
                },
                Id = 1,
                RequestTheMovieDbId = 1,
            };
            var result = await Rule.Execute(req);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public async Task RequestShow_DatabaseIdIsNotUsedAsTmdbFallback()
        {
            SetupMockData();

            var req = new ChildRequests
            {
                RequestType = RequestType.TvShow,
                Id = 1,
                RequestTheMovieDbId = 0,
                SeasonRequests = new List<SeasonRequests>
                {
                    new SeasonRequests
                    {
                        SeasonNumber = 1,
                        Episodes = new List<EpisodeRequests>
                        {
                            new EpisodeRequests { EpisodeNumber = 1 }
                        }
                    }
                }
            };

            var result = await Rule.Execute(req);

            Assert.That(result.Success, Is.True);
            Assert.That(req.SeasonRequests[0].Episodes, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task RequestMovie_IsSuccessful()
        {
            SetupMockData();

            var req = new MovieRequests
            {
                RequestType = RequestType.Movie,
                TheMovieDbId = 123,
                Id = 1,
            };
            var result = await Rule.Execute(req);


            Assert.That(result.Success, Is.True);
        }

        [Test]
        public async Task RequestMovie_IsAlreadyAvailable()
        {
            var content = new List<PlexServerContent> {
                new PlexServerContent
                {
                    TheMovieDbId = 123.ToString(),
                }
            };
            PlexContentRepo.Setup(x => x.GetAll()).Returns(content.AsQueryable().BuildMock());

            var req = new MovieRequests
            {
                RequestType = RequestType.Movie,
                TheMovieDbId = 123,
                Id = 1,
            };
            var result = await Rule.Execute(req);


            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.AlreadyRequested));
        }

        private void SetupMockData()
        {
            var childRequests = new List<PlexServerContent>
            {
                new PlexServerContent
                {
                    Type = MediaType.Series,
                    TheMovieDbId = "1",
                    Title = "Test",
                    ReleaseYear = "2001",
                    Episodes = new List<IMediaServerEpisode>
                    {
                        new PlexEpisode
                        {
                            EpisodeNumber = 1,
                            Id = 1,
                            SeasonNumber = 1,
                        },
                        new PlexEpisode
                        {
                            EpisodeNumber = 2,
                            Id = 2,
                            SeasonNumber = 1,
                        },
                    }
                }
            };
            PlexContentRepo.Setup(x => x.GetAll()).Returns(childRequests.AsQueryable().BuildMock());
        }
    }
}
