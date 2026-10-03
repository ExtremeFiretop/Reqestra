using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ombi.Store.Entities;
using Ombi.Store.Repository;
using Ombi.Store.Repository.Requests;

namespace Ombi.Core.Rule.Rules
{
    /// <summary>
    /// Conservative fallback for provider metadata splits where a standalone TMDB show maps to
    /// a season inside an anthology in Plex. A match is only accepted when exactly one Plex season
    /// has the same episode numbers and titles for the entire source season.
    /// </summary>
    internal static class PlexEpisodeFingerprintMatcher
    {
        private const int MinimumEpisodeFingerprintSize = 3;

        public static async Task<PlexEpisodeFingerprintMatch> FindSingleSeasonMatch(
            IPlexContentRepository repository,
            IEnumerable<SeasonRequests> seasons,
            int? restrictToSeriesId = null)
        {
            var sourceSeasons = seasons?
                .Where(x => x?.Episodes != null && x.Episodes.Count > 0)
                .ToList() ?? new List<SeasonRequests>();

            // Keep this fallback deliberately narrow. It is intended for providers that split an
            // anthology season into a standalone show (one source season -> one Plex season).
            if (sourceSeasons.Count != 1)
            {
                return null;
            }

            var sourceSeason = sourceSeasons[0];
            var fingerprintEpisodes = sourceSeason.Episodes
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Title))
                .GroupBy(x => x.EpisodeNumber)
                .Select(x => x.First())
                .OrderBy(x => x.EpisodeNumber)
                .ToList();

            if (fingerprintEpisodes.Count < MinimumEpisodeFingerprintSize)
            {
                return null;
            }

            // Use the longest title as the anchor to reduce the number of candidate seasons we
            // need to inspect while keeping the final decision based on the full season fingerprint.
            var anchor = fingerprintEpisodes
                .OrderByDescending(x => x.Title.Length)
                .First();

            var anchorQuery = repository.GetAllEpisodes()
                .Where(x => x.Series.Type == MediaType.Series &&
                            x.EpisodeNumber == anchor.EpisodeNumber &&
                            x.Title == anchor.Title);

            if (restrictToSeriesId.HasValue)
            {
                anchorQuery = anchorQuery.Where(x => x.Series.Id == restrictToSeriesId.Value);
            }

            var anchorMatches = await anchorQuery
                .Select(x => new
                {
                    SeriesId = x.Series.Id,
                    x.SeasonNumber
                })
                .Distinct()
                .ToListAsync();

            if (anchorMatches.Count == 0)
            {
                return null;
            }

            var candidateSeriesIds = anchorMatches
                .Select(x => x.SeriesId)
                .Distinct()
                .ToList();

            var candidateEpisodes = await repository.GetAllEpisodes()
                .Where(x => candidateSeriesIds.Contains(x.Series.Id))
                .Select(x => new
                {
                    SeriesId = x.Series.Id,
                    x.SeasonNumber,
                    x.EpisodeNumber,
                    x.Title
                })
                .ToListAsync();

            var matches = new List<(int SeriesId, int SeasonNumber)>();
            foreach (var candidate in anchorMatches)
            {
                var seasonEpisodes = candidateEpisodes
                    .Where(x => x.SeriesId == candidate.SeriesId && x.SeasonNumber == candidate.SeasonNumber)
                    .ToList();

                // Requiring the complete season to have the same episode count prevents a short
                // common-title subset from accidentally linking unrelated series.
                if (seasonEpisodes.Count != fingerprintEpisodes.Count)
                {
                    continue;
                }

                var candidateByNumber = seasonEpisodes
                    .GroupBy(x => x.EpisodeNumber)
                    .ToDictionary(x => x.Key, x => x.First().Title);

                var fullFingerprintMatches = fingerprintEpisodes.All(expected =>
                    candidateByNumber.TryGetValue(expected.EpisodeNumber, out var actualTitle) &&
                    string.Equals(
                        expected.Title?.Trim(),
                        actualTitle?.Trim(),
                        StringComparison.OrdinalIgnoreCase));

                if (fullFingerprintMatches)
                {
                    matches.Add((candidate.SeriesId, candidate.SeasonNumber));
                }
            }

            // Ambiguous fingerprints are intentionally ignored rather than guessing.
            if (matches.Count != 1)
            {
                return null;
            }

            var match = matches[0];
            var content = await repository.GetAll()
                .Include(x => x.Episodes)
                .FirstOrDefaultAsync(x => x.Id == match.SeriesId && x.Type == MediaType.Series);

            if (content == null)
            {
                return null;
            }

            return new PlexEpisodeFingerprintMatch
            {
                Content = content,
                SourceSeasonNumber = sourceSeason.SeasonNumber,
                PlexSeasonNumber = match.SeasonNumber
            };
        }
    }

    internal sealed class PlexEpisodeFingerprintMatch
    {
        public PlexServerContent Content { get; set; }
        public int SourceSeasonNumber { get; set; }
        public int PlexSeasonNumber { get; set; }
    }
}
