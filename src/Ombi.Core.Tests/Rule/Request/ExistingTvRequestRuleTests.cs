using MockQueryable.Moq;
using Moq;
using NUnit.Framework;
using Ombi.Core.Rule.Rules.Request;
using Ombi.Store.Entities;
using Ombi.Store.Entities.Requests;
using Ombi.Store.Repository.Requests;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ombi.Core.Tests.Rule.Request
{
    [TestFixture]
    public class ExistingTvRequestRuleTests
    {
        private ExistingTvRequestRule Rule;
        private Mock<ITvRequestRepository> TvRequestRepo;

        [SetUp]
        public void SetUp()
        {
            TvRequestRepo = new Mock<ITvRequestRepository>();
            Rule = new ExistingTvRequestRule(TvRequestRepo.Object);
        }

        [Test]
        public async Task RequestShow_DoesNotExistAtAll_IsSuccessful()
        {
            TvRequestRepo.Setup(x => x.GetChild()).Returns(new List<ChildRequests>().AsQueryable().BuildMock());
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
        public async Task RequestShow_EpisodesRequestedByDifferentChildren_AreAllRemoved()
        {
            var childRequests = new List<ChildRequests>
            {
                new ChildRequests
                {
                    ParentRequest = new TvRequests { ExternalProviderId = 1 },
                    SeasonRequests = new List<SeasonRequests>
                    {
                        new SeasonRequests
                        {
                            SeasonNumber = 1,
                            Episodes = new List<EpisodeRequests> { new EpisodeRequests { EpisodeNumber = 1 } }
                        }
                    }
                },
                new ChildRequests
                {
                    ParentRequest = new TvRequests { ExternalProviderId = 1 },
                    SeasonRequests = new List<SeasonRequests>
                    {
                        new SeasonRequests
                        {
                            SeasonNumber = 1,
                            Episodes = new List<EpisodeRequests> { new EpisodeRequests { EpisodeNumber = 2 } }
                        }
                    }
                }
            };
            TvRequestRepo.Setup(x => x.GetChild()).Returns(childRequests.AsQueryable().BuildMock());

            var req = new ChildRequests
            {
                RequestType = RequestType.TvShow,
                Id = 1,
                RequestTheMovieDbId = 1,
                SeasonRequests = new List<SeasonRequests>
                {
                    new SeasonRequests
                    {
                        SeasonNumber = 1,
                        Episodes = new List<EpisodeRequests>
                        {
                            new EpisodeRequests { EpisodeNumber = 1 },
                            new EpisodeRequests { EpisodeNumber = 2 },
                            new EpisodeRequests { EpisodeNumber = 3 },
                        }
                    }
                }
            };

            var result = await Rule.Execute(req);

            Assert.That(result.Success, Is.True);
            Assert.That(req.SeasonRequests.Single().Episodes.Select(x => x.EpisodeNumber), Is.EqualTo(new[] { 3 }));
        }


        [Test]
        public async Task RequestShow_MatchesExistingRequestByImdb_WhenTmdbIdChanges()
        {
            var childRequests = new List<ChildRequests>
            {
                new ChildRequests
                {
                    ParentRequest = new TvRequests
                    {
                        ExternalProviderId = 299939,
                        ImdbId = "tt13207736",
                        Title = "Monster",
                        ReleaseDate = new System.DateTime(2022, 9, 21)
                    },
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
                }
            };
            TvRequestRepo.Setup(x => x.GetChild()).Returns(childRequests.AsQueryable().BuildMock());
            TvRequestRepo.Setup(x => x.CleanupOrphanedRequestData()).ReturnsAsync(0);

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
            Assert.That(result.ErrorCode, Is.EqualTo(Ombi.Core.Engine.ErrorCode.EpisodesAlreadyRequested));
        }


        [Test]
        public async Task RequestShow_SharedAnthologyAlias_DoesNotTreatDifferentSeasonContentAsDuplicate()
        {
            var childRequests = new List<ChildRequests>
            {
                new ChildRequests
                {
                    ParentRequest = new TvRequests
                    {
                        Id = 10,
                        ExternalProviderId = 113988,
                        TvDbId = 389492,
                        ImdbId = "tt13207736",
                        Title = "DAHMER - Monster: The Jeffrey Dahmer Story",
                        ReleaseDate = new System.DateTime(2022, 9, 21)
                    },
                    SeasonRequests = new List<SeasonRequests>
                    {
                        new SeasonRequests
                        {
                            SeasonNumber = 1,
                            Episodes = new List<EpisodeRequests>
                            {
                                new EpisodeRequests { EpisodeNumber = 1, Title = "Episode One" },
                                new EpisodeRequests { EpisodeNumber = 2, Title = "Please Don't Go" },
                                new EpisodeRequests { EpisodeNumber = 3, Title = "Doin' a Dahmer" }
                            }
                        }
                    }
                }
            };
            TvRequestRepo.Setup(x => x.GetChild()).Returns(childRequests.AsQueryable().BuildMock());
            TvRequestRepo.Setup(x => x.CleanupOrphanedRequestData()).ReturnsAsync(0);

            var req = new ChildRequests
            {
                RequestType = RequestType.TvShow,
                RequestTheMovieDbId = 299939,
                RequestTvDbId = 389492,
                RequestImdbId = "tt13207736",
                Title = "Monster: The Lizzie Borden Story",
                ReleaseYear = new System.DateTime(2026, 9, 17),
                SeasonRequests = new List<SeasonRequests>
                {
                    new SeasonRequests
                    {
                        SeasonNumber = 1,
                        Episodes = new List<EpisodeRequests>
                        {
                            new EpisodeRequests { EpisodeNumber = 1, Title = "Bloodbath" },
                            new EpisodeRequests { EpisodeNumber = 2, Title = "Strong Kitty" },
                            new EpisodeRequests { EpisodeNumber = 3, Title = "Whack Job!" }
                        }
                    }
                }
            };

            var result = await Rule.Execute(req);

            Assert.That(result.Success, Is.True);
            Assert.That(req.SeasonRequests.Single().Episodes.Count, Is.EqualTo(3));
            Assert.That(req.RequestExistingParentId, Is.EqualTo(0));
            Assert.That(req.RequestSeasonMappings, Is.Empty);
        }

        [Test]
        public async Task RequestShow_SharedAnthologyAlias_UsesEpisodeFingerprintToMapSeason()
        {
            var childRequests = new List<ChildRequests>
            {
                new ChildRequests
                {
                    ParentRequest = new TvRequests
                    {
                        Id = 11,
                        ExternalProviderId = 335840,
                        TvDbId = 389492,
                        ImdbId = "tt13207736",
                        Title = "Monster (2022)",
                        ReleaseDate = new System.DateTime(2022, 9, 21)
                    },
                    SeasonRequests = new List<SeasonRequests>
                    {
                        new SeasonRequests
                        {
                            SeasonNumber = 4,
                            Episodes = new List<EpisodeRequests>
                            {
                                new EpisodeRequests { EpisodeNumber = 1, Title = "Bloodbath" },
                                new EpisodeRequests { EpisodeNumber = 2, Title = "Strong Kitty" },
                                new EpisodeRequests { EpisodeNumber = 3, Title = "Whack Job!" }
                            }
                        }
                    }
                }
            };
            TvRequestRepo.Setup(x => x.GetChild()).Returns(childRequests.AsQueryable().BuildMock());
            TvRequestRepo.Setup(x => x.CleanupOrphanedRequestData()).ReturnsAsync(0);

            var req = new ChildRequests
            {
                RequestType = RequestType.TvShow,
                RequestTheMovieDbId = 299939,
                RequestTvDbId = 389492,
                RequestImdbId = "tt13207736",
                Title = "Monster: The Lizzie Borden Story",
                ReleaseYear = new System.DateTime(2026, 9, 17),
                SeasonRequests = new List<SeasonRequests>
                {
                    new SeasonRequests
                    {
                        SeasonNumber = 1,
                        Episodes = new List<EpisodeRequests>
                        {
                            new EpisodeRequests { EpisodeNumber = 1, Title = "Bloodbath" },
                            new EpisodeRequests { EpisodeNumber = 2, Title = "Strong Kitty" },
                            new EpisodeRequests { EpisodeNumber = 3, Title = "Whack Job!" }
                        }
                    }
                }
            };

            var result = await Rule.Execute(req);

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(Ombi.Core.Engine.ErrorCode.EpisodesAlreadyRequested));
            Assert.That(req.RequestExistingParentId, Is.EqualTo(11));
            Assert.That(req.RequestSeasonMappings[1], Is.EqualTo(4));
        }


        [Test]
        public async Task RequestShow_SharedAnthologyAlias_PreservesMappingAfterPartialDuplicateRemoval()
        {
            var childRequests = new List<ChildRequests>
            {
                new ChildRequests
                {
                    ParentRequest = new TvRequests
                    {
                        Id = 12,
                        ExternalProviderId = 335840,
                        TvDbId = 389492,
                        ImdbId = "tt13207736",
                        Title = "Monster (2022)",
                        ReleaseDate = new System.DateTime(2022, 9, 21)
                    },
                    SeasonRequests = new List<SeasonRequests>
                    {
                        new SeasonRequests
                        {
                            SeasonNumber = 4,
                            Episodes = new List<EpisodeRequests>
                            {
                                new EpisodeRequests { EpisodeNumber = 1, Title = "Bloodbath" },
                                new EpisodeRequests { EpisodeNumber = 2, Title = "Strong Kitty" },
                                new EpisodeRequests { EpisodeNumber = 3, Title = "Whack Job!" }
                            }
                        }
                    }
                }
            };
            TvRequestRepo.Setup(x => x.GetChild()).Returns(childRequests.AsQueryable().BuildMock());
            TvRequestRepo.Setup(x => x.CleanupOrphanedRequestData()).ReturnsAsync(0);

            var req = new ChildRequests
            {
                RequestType = RequestType.TvShow,
                RequestTheMovieDbId = 299939,
                RequestTvDbId = 389492,
                RequestImdbId = "tt13207736",
                Title = "Monster: The Lizzie Borden Story",
                ReleaseYear = new System.DateTime(2026, 9, 17),
                SeasonRequests = new List<SeasonRequests>
                {
                    new SeasonRequests
                    {
                        SeasonNumber = 1,
                        Episodes = new List<EpisodeRequests>
                        {
                            new EpisodeRequests { EpisodeNumber = 1, Title = "Bloodbath" },
                            new EpisodeRequests { EpisodeNumber = 2, Title = "Strong Kitty" },
                            new EpisodeRequests { EpisodeNumber = 3, Title = "Whack Job!" },
                            new EpisodeRequests { EpisodeNumber = 4, Title = "R.I.P (Rest in Pestilence) Abby Borden" },
                            new EpisodeRequests { EpisodeNumber = 5, Title = "41" }
                        }
                    }
                }
            };

            var result = await Rule.Execute(req);

            Assert.That(result.Success, Is.True);
            Assert.That(req.SeasonRequests.Single().Episodes.Select(x => x.EpisodeNumber), Is.EqualTo(new[] { 4, 5 }));
            Assert.That(req.RequestExistingParentId, Is.EqualTo(12));
            Assert.That(req.RequestSeasonMappings[1], Is.EqualTo(4));
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

        private void SetupMockData()
        {
            var childRequests = new List<ChildRequests>
            {
                new ChildRequests
                {
                    ParentRequest = new TvRequests
                    {
                        Id = 1,
                        ExternalProviderId = 1,
                    },
                    SeasonRequests = new List<SeasonRequests>
                    {
                        new SeasonRequests
                        {
                            Id = 1,
                            SeasonNumber = 1,
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
                                }
                            }
                        }
                    }
                }
            };
            TvRequestRepo.Setup(x => x.GetChild()).Returns(childRequests.AsQueryable().BuildMock());
        }
    }
}
