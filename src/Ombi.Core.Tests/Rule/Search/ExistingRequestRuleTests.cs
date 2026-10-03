using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Ombi.Core.Models.Search;
using Ombi.Core.Rule.Rules.Search;
using Ombi.Store.Entities;
using Ombi.Store.Entities.Requests;
using Ombi.Store.Repository;
using Ombi.Store.Repository.Requests;

namespace Ombi.Core.Tests.Rule.Search
{
    public class ExistingRequestRuleTests
    {
        [SetUp]
        public void Setup()
        {

            MovieMock = new Mock<IMovieRequestRepository>();
            TvMock = new Mock<ITvRequestRepository>();
            MusicMock = new Mock<IMusicRequestRepository>();
            Rule = new ExistingRule(MovieMock.Object, TvMock.Object, MusicMock.Object);
        }

        private ExistingRule Rule { get; set; }
        private Mock<IMovieRequestRepository> MovieMock { get; set; }
        private Mock<ITvRequestRepository> TvMock { get; set; }
        private Mock<IMusicRequestRepository> MusicMock { get; set; }


        [Test]
        public async Task ShouldBe_Requested_WhenExistingMovie()
        {
            var list = new MovieRequests
            {
                TheMovieDbId = 123,
                Approved = true,
                RequestType = RequestType.Movie,
                RequestedDate = System.DateTime.Now,
            };

            MovieMock.Setup(x => x.GetRequestAsync(123)).ReturnsAsync(list);
            var search = new SearchMovieViewModel
            {
                Id = 123,
            };
            var result = await Rule.Execute(search);

            Assert.That(result.Success, Is.True);
            Assert.That(search.Approved, Is.True);
            Assert.That(search.Requested, Is.True);
        }

        [Test]
        public async Task ShouldBe_NotRequested_WhenNewMovie()
        {
            var list = new MovieRequests
            {
                TheMovieDbId = 123,
                Approved = true
            };

            MovieMock.Setup(x => x.GetRequestAsync(123)).ReturnsAsync(list);
            var search = new SearchMovieViewModel
            {
                Id = 999,

            };
            var result = await Rule.Execute(search);

            Assert.True(result.Success);
            Assert.False(search.Approved);
            Assert.False(search.Requested);
        }

        [Test]
        public async Task ShouldBe_Requested_WhenExisitngTv()
        {
            var list = new TvRequests
            {
                TvDbId = 123,
                ChildRequests = new List<ChildRequests>
                {
                    new ChildRequests()
                    {
                        Approved = true

                    }
                }
            };

            TvMock.Setup(x => x.GetRequest(123)).Returns(list);
            var search = new SearchTvShowViewModel
            {
                Id = 123,
            };
            var result = await Rule.Execute(search);

            Assert.True(result.Success);
            Assert.True(search.Approved);
            Assert.True(search.Requested);
        }

        [Test]
        public async Task ShouldBe_NotRequested_WhenNewTv()
        {
            var list = new TvRequests
            {
                TvDbId = 123,
                ChildRequests = new List<ChildRequests>
                {
                    new ChildRequests()
                    {
                        Approved = true

                    }
                }
            };


            TvMock.Setup(x => x.GetRequest(123)).Returns(list);
            var search = new SearchTvShowViewModel()
            {
                Id = 999,

            };
            var result = await Rule.Execute(search);

            Assert.True(result.Success);
            Assert.False(search.Approved);
            Assert.False(search.Requested);
        }

        [Test]
        public async Task ShouldBeFullyAvailable_NoFutureAiredEpisodes_NoRequest()
        {
            var search = new SearchTvShowViewModel()
            {
                Id = 999,
                SeasonRequests = new List<SeasonRequests>
                {
                    new SeasonRequests
                    {
                        Episodes = new List<EpisodeRequests>
                        {
                            new EpisodeRequests
                            {
                                Available = true,
                                AirDate = new System.DateTime(2020,01,01)
                            },
                            new EpisodeRequests
                            {
                                Available = true,
                                AirDate = new System.DateTime(2020,01,02)
                            },
                        }
                    }
                }
            };
            var result = await Rule.Execute(search);

            Assert.True(result.Success);
            Assert.That(search.FullyAvailable, Is.True);
            Assert.That(search.PartlyAvailable, Is.False);
        }

        [Test]
        public async Task ShouldBeFullyAvailable_AndPartly_FutureAiredEpisodes_NoRequest()
        {
            var search = new SearchTvShowViewModel()
            {
                Id = 999,
                SeasonRequests = new List<SeasonRequests>
                {
                    new SeasonRequests
                    {
                        Episodes = new List<EpisodeRequests>
                        {
                            new EpisodeRequests
                            {
                                Available = true,
                                AirDate = new System.DateTime(2020,01,01)
                            },
                            new EpisodeRequests
                            {
                                Available = true,
                                AirDate = new System.DateTime(2020,01,02)
                            },
                            new EpisodeRequests
                            {
                                Available = true,
                                AirDate = new System.DateTime(2029,01,02)
                            },
                        }
                    }
                }
            };
            var result = await Rule.Execute(search);

            Assert.True(result.Success);
            Assert.That(search.FullyAvailable, Is.True);
            Assert.That(search.PartlyAvailable, Is.True);
        }

        [Test]
        public async Task TvSearch_ProviderIdChanged_SameTitleAndYear_MarksExistingRequest()
        {
            var existing = CreateTvParent(
                id: 42,
                tmdbId: 335840,
                tvdbId: 0,
                imdbId: string.Empty,
                title: "Monster: The Lizzie Borden Story",
                releaseDate: new System.DateTime(2026, 9, 17),
                seasonNumber: 1,
                approved: true,
                episodeTitles: new[] { "Bloodbath", "Strong Kitty", "Whack Job!" });

            TvMock.Setup(x => x.Get()).Returns(new[] { existing }.AsQueryable());

            var search = CreateTvSearch(
                tmdbId: 299939,
                tvdbId: null,
                imdbId: null,
                title: "Monster: The Lizzie Borden Story",
                firstAired: "2026-09-17",
                seasonNumber: 1,
                episodeTitles: new[] { "Bloodbath", "Strong Kitty", "Whack Job!" });

            var result = await Rule.Execute(search);

            Assert.That(result.Success, Is.True);
            Assert.That(search.Requested, Is.True);
            Assert.That(search.RequestId, Is.EqualTo(42));
            Assert.That(search.Approved, Is.True);
            Assert.That(search.SeasonRequests.Single().Episodes.All(x => x.Requested), Is.True);
        }

        [Test]
        public async Task TvSearch_SharedAnthologyAlias_DifferentSeasonFingerprint_IsNotMarkedRequested()
        {
            var existing = CreateTvParent(
                id: 43,
                tmdbId: 335840,
                tvdbId: 389492,
                imdbId: "tt13207736",
                title: "Monster (2022)",
                releaseDate: new System.DateTime(2022, 9, 21),
                seasonNumber: 1,
                approved: true,
                episodeTitles: new[] { "Episode One", "Please Don't Go", "Doin' a Dahmer" });

            TvMock.Setup(x => x.Get()).Returns(new[] { existing }.AsQueryable());

            var search = CreateTvSearch(
                tmdbId: 299939,
                tvdbId: "389492",
                imdbId: "tt13207736",
                title: "Monster: The Lizzie Borden Story",
                firstAired: "2026-09-17",
                seasonNumber: 1,
                episodeTitles: new[] { "Bloodbath", "Strong Kitty", "Whack Job!" });

            var result = await Rule.Execute(search);

            Assert.That(result.Success, Is.True);
            Assert.That(search.Requested, Is.False);
            Assert.That(search.SeasonRequests.Single().Episodes.Any(x => x.Requested), Is.False);
        }

        [Test]
        public async Task TvSearch_SharedAnthologyAlias_FingerprintMapsStandaloneSeasonToExistingSeason()
        {
            var existing = CreateTvParent(
                id: 44,
                tmdbId: 335840,
                tvdbId: 389492,
                imdbId: "tt13207736",
                title: "Monster (2022)",
                releaseDate: new System.DateTime(2022, 9, 21),
                seasonNumber: 4,
                approved: true,
                episodeTitles: new[] { "Bloodbath", "Strong Kitty", "Whack Job!" });

            TvMock.Setup(x => x.Get()).Returns(new[] { existing }.AsQueryable());

            var search = CreateTvSearch(
                tmdbId: 299939,
                tvdbId: "389492",
                imdbId: "tt13207736",
                title: "Monster: The Lizzie Borden Story",
                firstAired: "2026-09-17",
                seasonNumber: 1,
                episodeTitles: new[] { "Bloodbath", "Strong Kitty", "Whack Job!" });

            var result = await Rule.Execute(search);

            Assert.That(result.Success, Is.True);
            Assert.That(search.Requested, Is.True);
            Assert.That(search.RequestId, Is.EqualTo(44));
            Assert.That(search.Approved, Is.True);
            Assert.That(search.SeasonRequests.Single().Episodes.All(x => x.Requested), Is.True);
        }

        private static TvRequests CreateTvParent(
            int id,
            int tmdbId,
            int tvdbId,
            string imdbId,
            string title,
            System.DateTime releaseDate,
            int seasonNumber,
            bool approved,
            params string[] episodeTitles)
        {
            var child = new ChildRequests
            {
                Approved = approved,
                SeasonRequests = new List<SeasonRequests>()
            };
            var season = new SeasonRequests
            {
                SeasonNumber = seasonNumber,
                ChildRequest = child,
                Episodes = episodeTitles.Select((episodeTitle, index) => new EpisodeRequests
                {
                    EpisodeNumber = index + 1,
                    Title = episodeTitle,
                    Requested = true,
                    Season = null
                }).ToList()
            };
            foreach (var episode in season.Episodes)
            {
                episode.Season = season;
            }
            child.SeasonRequests.Add(season);

            return new TvRequests
            {
                Id = id,
                ExternalProviderId = tmdbId,
                TvDbId = tvdbId,
                ImdbId = imdbId,
                Title = title,
                ReleaseDate = releaseDate,
                ChildRequests = new List<ChildRequests> { child }
            };
        }

        private static SearchTvShowViewModel CreateTvSearch(
            int tmdbId,
            string tvdbId,
            string imdbId,
            string title,
            string firstAired,
            int seasonNumber,
            params string[] episodeTitles)
        {
            return new SearchTvShowViewModel
            {
                Id = tmdbId,
                TheTvDbId = tvdbId,
                ImdbId = imdbId,
                Title = title,
                FirstAired = firstAired,
                SeasonRequests = new List<SeasonRequests>
                {
                    new SeasonRequests
                    {
                        SeasonNumber = seasonNumber,
                        Episodes = episodeTitles.Select((episodeTitle, index) => new EpisodeRequests
                        {
                            EpisodeNumber = index + 1,
                            Title = episodeTitle
                        }).ToList()
                    }
                }
            };
        }

    }
}
