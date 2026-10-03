using System;
using System.Collections.Generic;
using NUnit.Framework;
using Ombi.Core.Helpers;
using Ombi.Store.Entities.Requests;
using Ombi.Store.Repository.Requests;

namespace Ombi.Core.Tests.Helpers
{
    [TestFixture]
    public class TvRequestSeasonIdentityMatcherTests
    {
        [Test]
        public void FindSafeAliasParent_SharedAnthologyIdsButDifferentContent_DoesNotAttach()
        {
            var dahmerParent = CreateParent(
                1,
                "DAHMER - Monster: The Jeffrey Dahmer Story",
                2022,
                1,
                "Episode One",
                "Please Don't Go",
                "Doin' a Dahmer");

            var lizzieRequest = CreateRequest(
                "Monster: The Lizzie Borden Story",
                2026,
                1,
                "Bloodbath",
                "Strong Kitty",
                "Whack Job!");

            var result = TvRequestSeasonIdentityMatcher.FindSafeAliasParent(
                lizzieRequest,
                new[] { dahmerParent });

            Assert.That(result, Is.Null);
        }

        [Test]
        public void FindSafeAliasParent_StandaloneSeasonMatchesAnthologySeason_AttachesMappedParent()
        {
            var anthologyParent = CreateParent(
                2,
                "Monster (2022)",
                2022,
                4,
                "Bloodbath",
                "Strong Kitty",
                "Whack Job!");

            var lizzieRequest = CreateRequest(
                "Monster: The Lizzie Borden Story",
                2026,
                1,
                "Bloodbath",
                "Strong Kitty",
                "Whack Job!");

            var result = TvRequestSeasonIdentityMatcher.FindSafeAliasParent(
                lizzieRequest,
                new[] { anthologyParent });

            Assert.That(result, Is.SameAs(anthologyParent));
            Assert.That(
                TvRequestSeasonIdentityMatcher.FindSingleSeasonMatch(
                    lizzieRequest.SeasonRequests[0],
                    anthologyParent.ChildRequests[0].SeasonRequests),
                Is.EqualTo(4));
        }

        [Test]
        public void FindSafeAliasParent_SameTitleAndYearAfterProviderIdChange_AttachesWithoutFingerprint()
        {
            var existingParent = CreateParent(
                3,
                "Monster",
                2022,
                1,
                "Old title one",
                "Old title two",
                "Old title three");

            var request = new ChildRequests
            {
                Title = "Monster",
                ReleaseYear = new DateTime(2022, 9, 21),
                SeasonRequests = new List<SeasonRequests>
                {
                    new SeasonRequests
                    {
                        SeasonNumber = 2,
                        Episodes = new List<EpisodeRequests>
                        {
                            new EpisodeRequests { EpisodeNumber = 1, Title = "New season episode" }
                        }
                    }
                }
            };

            var result = TvRequestSeasonIdentityMatcher.FindSafeAliasParent(
                request,
                new[] { existingParent });

            Assert.That(result, Is.SameAs(existingParent));
        }

        [Test]
        public void FindSingleSeasonMatch_AmbiguousFingerprint_ReturnsNull()
        {
            var source = CreateSeason(1, "Same One", "Same Two", "Same Three");
            var targets = new[]
            {
                CreateSeason(2, "Same One", "Same Two", "Same Three"),
                CreateSeason(4, "Same One", "Same Two", "Same Three")
            };

            var result = TvRequestSeasonIdentityMatcher.FindSingleSeasonMatch(source, targets);

            Assert.That(result, Is.Null);
        }

        private static ChildRequests CreateRequest(
            string title,
            int year,
            int seasonNumber,
            params string[] episodeTitles)
        {
            return new ChildRequests
            {
                Title = title,
                ReleaseYear = new DateTime(year, 1, 1),
                SeasonRequests = new List<SeasonRequests>
                {
                    CreateSeason(seasonNumber, episodeTitles)
                }
            };
        }

        private static TvRequests CreateParent(
            int id,
            string title,
            int year,
            int seasonNumber,
            params string[] episodeTitles)
        {
            return new TvRequests
            {
                Id = id,
                Title = title,
                ReleaseDate = new DateTime(year, 1, 1),
                ChildRequests = new List<ChildRequests>
                {
                    new ChildRequests
                    {
                        SeasonRequests = new List<SeasonRequests>
                        {
                            CreateSeason(seasonNumber, episodeTitles)
                        }
                    }
                }
            };
        }

        private static SeasonRequests CreateSeason(int seasonNumber, params string[] episodeTitles)
        {
            var episodes = new List<EpisodeRequests>();
            for (var i = 0; i < episodeTitles.Length; i++)
            {
                episodes.Add(new EpisodeRequests
                {
                    EpisodeNumber = i + 1,
                    Title = episodeTitles[i]
                });
            }

            return new SeasonRequests
            {
                SeasonNumber = seasonNumber,
                Episodes = episodes
            };
        }
    }
}
