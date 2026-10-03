using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ombi.Core.Engine;
using Ombi.Core.Helpers;
using Ombi.Core.Rule.Interfaces;
using Ombi.Store.Entities;
using Ombi.Store.Entities.Requests;
using Ombi.Store.Repository.Requests;

namespace Ombi.Core.Rule.Rules.Request
{
    public class ExistingTvRequestRule : BaseRequestRule, IRules<BaseRequest>
    {
        public ExistingTvRequestRule(ITvRequestRepository rv)
        {
            Tv = rv;
        }

        private ITvRequestRepository Tv { get; }

        /// <summary>
        /// We check if the request exists, if it does then we don't want to re-request it.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        public async Task<RuleResult> Execute(BaseRequest obj)
        {
            if (obj.RequestType == RequestType.TvShow)
            {
                var tv = (ChildRequests)obj;

                // Repair legacy request graphs before using them to decide whether an episode is
                // already requested. This makes stale rows self-heal on the very next request attempt
                // instead of requiring another delete operation first.
                await Tv.CleanupOrphanedRequestData();

                var requestTheMovieDbId = tv.RequestTheMovieDbId;
                var requestTvDbId = tv.RequestTvDbId;
                var requestImdbId = tv.RequestImdbId;
                var hasTheMovieDbId = requestTheMovieDbId > 0;
                var hasTvDbId = requestTvDbId > 0;
                var hasImdbId = !string.IsNullOrEmpty(requestImdbId);

                var currentRequests = await Tv.GetChild()
                    .Where(x =>
                        (hasTheMovieDbId && x.ParentRequest.ExternalProviderId == requestTheMovieDbId) ||
                        (hasTvDbId && x.ParentRequest.TvDbId == requestTvDbId) ||
                        (hasImdbId && x.ParentRequest.ImdbId == requestImdbId))
                    .ToListAsync();
                if (currentRequests.Count == 0)
                {
                    return Success();
                }

                // Exact TMDB identity means both requests use the same provider representation, so
                // literal season numbers are safe. Alternate TVDB/IMDb identities can instead point
                // at a shared anthology parent and must prove their season relationship first.
                var exactTmdbRequests = hasTheMovieDbId
                    ? currentRequests.Where(x => x.ParentRequest.ExternalProviderId == requestTheMovieDbId).ToList()
                    : new List<ChildRequests>();

                if (exactTmdbRequests.Count > 0)
                {
                    RemoveExistingEpisodesByLiteralSeason(tv, exactTmdbRequests);
                }
                else
                {
                    RemoveExistingEpisodesFromSafeAliases(tv, currentRequests);
                }

                var anyEpisodes = tv.SeasonRequests.SelectMany(x => x.Episodes).Any();

                if (!anyEpisodes)
                {
                    return Fail(ErrorCode.EpisodesAlreadyRequested, $"We already have episodes requested from series {tv.Title}");
                }
            }

            return Success();
        }

        private static void RemoveExistingEpisodesByLiteralSeason(
            ChildRequests request,
            IEnumerable<ChildRequests> currentRequests)
        {
            var existingSeasons = currentRequests
                .Where(x => x?.SeasonRequests != null)
                .SelectMany(x => x.SeasonRequests)
                .ToList();

            foreach (var season in request.SeasonRequests)
            {
                var existingEpisodeNumbers = existingSeasons
                    .Where(x => x.SeasonNumber == season.SeasonNumber)
                    .SelectMany(x => x.Episodes ?? new List<EpisodeRequests>())
                    .Select(x => x.EpisodeNumber)
                    .ToHashSet();

                season.Episodes.RemoveAll(x => existingEpisodeNumbers.Contains(x.EpisodeNumber));
            }
        }

        private static void RemoveExistingEpisodesFromSafeAliases(
            ChildRequests request,
            IEnumerable<ChildRequests> currentRequests)
        {
            var currentRequestList = currentRequests?.ToList() ?? new List<ChildRequests>();
            var aliasMatch = TvRequestSeasonIdentityMatcher.FindSafeAliasMatchFromChildren(
                request,
                currentRequestList);

            if (aliasMatch == null)
            {
                return;
            }

            request.RequestExistingParentId = aliasMatch.Parent.Id;
            foreach (var mapping in aliasMatch.SeasonMappings)
            {
                request.RequestSeasonMappings[mapping.Key] = mapping.Value;
            }

            var targetSeasons = currentRequestList
                .Where(x => x?.ParentRequest?.Id == aliasMatch.Parent.Id && x.SeasonRequests != null)
                .SelectMany(x => x.SeasonRequests)
                .ToList();

            foreach (var sourceSeason in request.SeasonRequests)
            {
                if (!aliasMatch.SeasonMappings.TryGetValue(sourceSeason.SeasonNumber, out var targetSeasonNumber))
                {
                    continue;
                }

                var existingEpisodeNumbers = targetSeasons
                    .Where(x => x.SeasonNumber == targetSeasonNumber)
                    .SelectMany(x => x.Episodes ?? new List<EpisodeRequests>())
                    .Select(x => x.EpisodeNumber)
                    .ToHashSet();

                sourceSeason.Episodes.RemoveAll(x => existingEpisodeNumbers.Contains(x.EpisodeNumber));
            }
        }
    }
}
