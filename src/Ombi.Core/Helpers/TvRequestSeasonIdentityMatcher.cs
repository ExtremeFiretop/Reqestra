using System;
using System.Collections.Generic;
using System.Linq;
using Ombi.Store.Entities.Requests;
using Ombi.Store.Repository.Requests;

namespace Ombi.Core.Helpers
{
    /// <summary>
    /// Provides conservative identity checks for TV requests whose stable provider IDs can point
    /// at the same anthology parent while TMDB exposes individual anthology seasons as standalone
    /// shows. Provider aliases alone are not enough to prove that season numbers are equivalent.
    /// </summary>
    public static class TvRequestSeasonIdentityMatcher
    {
        private const int MinimumEpisodeFingerprintSize = 3;

        public static bool SeriesMetadataMatches(ChildRequests source, TvRequests candidate)
        {
            if (source == null || candidate == null)
            {
                return false;
            }

            var sourceTitle = NormalizeTitle(source.Title);
            var candidateTitle = NormalizeTitle(candidate.Title);
            if (string.IsNullOrEmpty(sourceTitle) ||
                !string.Equals(sourceTitle, candidateTitle, StringComparison.Ordinal))
            {
                return false;
            }

            var sourceYear = source.ReleaseYear.Year;
            var candidateYear = candidate.ReleaseDate.Year;
            return sourceYear > 1 && candidateYear > 1 && sourceYear == candidateYear;
        }

        /// <summary>
        /// Finds exactly one target season whose episode-number/title fingerprint is compatible
        /// with the source season. One side may be a subset of the other so partial historical
        /// requests can still establish the mapping, but at least three titled episodes must agree.
        /// Ambiguous or conflicting matches intentionally return null.
        /// </summary>
        public static int? FindSingleSeasonMatch(
            SeasonRequests sourceSeason,
            IEnumerable<SeasonRequests> targetSeasons)
        {
            var sourceFingerprint = GetFingerprint(sourceSeason?.Episodes);
            if (sourceFingerprint.Count < MinimumEpisodeFingerprintSize)
            {
                return null;
            }

            var candidates = (targetSeasons ?? Enumerable.Empty<SeasonRequests>())
                .Where(x => x != null)
                .GroupBy(x => x.SeasonNumber)
                .Select(x => new
                {
                    SeasonNumber = x.Key,
                    Fingerprint = GetFingerprint(x.SelectMany(y => y.Episodes ?? new List<EpisodeRequests>()))
                })
                .Where(x => x.Fingerprint.Count >= MinimumEpisodeFingerprintSize)
                .ToList();

            var matches = new List<int>();
            foreach (var candidate in candidates)
            {
                var sharedEpisodeNumbers = sourceFingerprint.Keys
                    .Intersect(candidate.Fingerprint.Keys)
                    .ToList();

                if (sharedEpisodeNumbers.Count < MinimumEpisodeFingerprintSize)
                {
                    continue;
                }

                var hasConflict = sharedEpisodeNumbers.Any(episodeNumber =>
                    !string.Equals(
                        sourceFingerprint[episodeNumber],
                        candidate.Fingerprint[episodeNumber],
                        StringComparison.Ordinal));

                if (hasConflict)
                {
                    continue;
                }

                // Require the smaller fingerprint to be completely represented in the larger one.
                // This supports partial requests without accepting a coincidental three-episode
                // overlap between otherwise different seasons.
                var smallerFingerprintSize = Math.Min(sourceFingerprint.Count, candidate.Fingerprint.Count);
                if (sharedEpisodeNumbers.Count != smallerFingerprintSize)
                {
                    continue;
                }

                matches.Add(candidate.SeasonNumber);
            }

            return matches.Distinct().Count() == 1 ? matches[0] : null;
        }

        public static TvRequestAliasIdentityMatch FindSafeAliasMatch(
            ChildRequests source,
            IEnumerable<TvRequests> candidates)
        {
            var parentCandidates = (candidates ?? Enumerable.Empty<TvRequests>())
                .Where(x => x != null)
                .GroupBy(x => x.Id)
                .Select(x => x.First())
                .Select(parent => new AliasParentCandidate
                {
                    Parent = parent,
                    TargetSeasons = parent.ChildRequests?
                        .Where(x => x?.SeasonRequests != null)
                        .SelectMany(x => x.SeasonRequests)
                        .ToList() ?? new List<SeasonRequests>()
                })
                .ToList();

            return FindSafeAliasMatch(source, parentCandidates);
        }

        public static TvRequestAliasIdentityMatch FindSafeAliasMatchFromChildren(
            ChildRequests source,
            IEnumerable<ChildRequests> candidateChildren)
        {
            var parentCandidates = (candidateChildren ?? Enumerable.Empty<ChildRequests>())
                .Where(x => x?.ParentRequest != null)
                .GroupBy(x => x.ParentRequest.Id)
                .Select(group => new AliasParentCandidate
                {
                    Parent = group.First().ParentRequest,
                    TargetSeasons = group
                        .Where(x => x.SeasonRequests != null)
                        .SelectMany(x => x.SeasonRequests)
                        .ToList()
                })
                .ToList();

            return FindSafeAliasMatch(source, parentCandidates);
        }

        public static TvRequests FindSafeAliasParent(
            ChildRequests source,
            IEnumerable<TvRequests> candidates)
        {
            return FindSafeAliasMatch(source, candidates)?.Parent;
        }

        private static TvRequestAliasIdentityMatch FindSafeAliasMatch(
            ChildRequests source,
            IEnumerable<AliasParentCandidate> candidates)
        {
            var parents = candidates?.ToList() ?? new List<AliasParentCandidate>();
            if (parents.Count == 0)
            {
                return null;
            }

            // Same normalized title + release year is strong evidence that this is the same TMDB
            // representation after an ID change rather than a different season of an anthology.
            var metadataMatches = parents
                .Where(x => SeriesMetadataMatches(source, x.Parent))
                .OrderBy(x => x.Parent.Id)
                .ToList();

            if (metadataMatches.Count > 0)
            {
                var selected = metadataMatches[0];
                return new TvRequestAliasIdentityMatch
                {
                    Parent = selected.Parent,
                    UseLiteralSeasonNumbers = true,
                    SeasonMappings = source?.SeasonRequests?
                        .GroupBy(x => x.SeasonNumber)
                        .ToDictionary(x => x.Key, x => x.Key) ?? new Dictionary<int, int>()
                };
            }

            var fingerprintMatches = new List<TvRequestAliasIdentityMatch>();
            foreach (var candidate in parents)
            {
                var mappings = new Dictionary<int, int>();
                foreach (var sourceSeason in source?.SeasonRequests ?? new List<SeasonRequests>())
                {
                    var targetSeason = FindSingleSeasonMatch(sourceSeason, candidate.TargetSeasons);
                    if (targetSeason.HasValue)
                    {
                        mappings[sourceSeason.SeasonNumber] = targetSeason.Value;
                    }
                }

                if (mappings.Count > 0)
                {
                    fingerprintMatches.Add(new TvRequestAliasIdentityMatch
                    {
                        Parent = candidate.Parent,
                        SeasonMappings = mappings
                    });
                }
            }

            // Never attach to an arbitrary parent when more than one alias candidate satisfies the
            // fingerprint. Creating a separate parent is safer than cross-linking anthology requests.
            return fingerprintMatches.Count == 1 ? fingerprintMatches[0] : null;
        }

        private static Dictionary<int, string> GetFingerprint(IEnumerable<EpisodeRequests> episodes)
        {
            return (episodes ?? Enumerable.Empty<EpisodeRequests>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Title))
                .GroupBy(x => x.EpisodeNumber)
                .Select(x => x.First())
                .Select(x => new
                {
                    x.EpisodeNumber,
                    Title = NormalizeTitle(x.Title)
                })
                .Where(x => !string.IsNullOrEmpty(x.Title))
                .ToDictionary(x => x.EpisodeNumber, x => x.Title);
        }

        private static string NormalizeTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return string.Empty;
            }

            return new string(title
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());
        }

        private sealed class AliasParentCandidate
        {
            public TvRequests Parent { get; set; }
            public List<SeasonRequests> TargetSeasons { get; set; }
        }
    }

    public sealed class TvRequestAliasIdentityMatch
    {
        public TvRequests Parent { get; set; }
        public bool UseLiteralSeasonNumbers { get; set; }
        public Dictionary<int, int> SeasonMappings { get; set; } = new Dictionary<int, int>();
    }
}
