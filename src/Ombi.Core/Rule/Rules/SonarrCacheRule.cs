using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ombi.Core.Engine;
using Ombi.Core.Models.Search;
using Ombi.Core.Rule.Rules.Search;
using Ombi.Helpers;
using Ombi.Store.Context;
using Ombi.Store.Entities;
using Ombi.Store.Entities.Requests;
using Ombi.Store.Repository.Requests;

namespace Ombi.Core.Rule.Rules
{
    public class SonarrCacheRule
    {
        public SonarrCacheRule(ExternalContext ctx)
        {
            _ctx = ctx;
        }

        private readonly ExternalContext _ctx;

        public async Task<RuleResult> Execute(BaseRequest obj)
        {
            if (obj.RequestType == RequestType.TvShow)
            {
                var vm = (ChildRequests) obj;
                if (vm.SeasonRequests.Any())
                {
                    var requestTvDbId = vm.RequestTvDbId;

                    // Sonarr is TVDB-centric. Do not fall back to TMDB for request-time episode
                    // deduplication: Sonarr can expose an anthology parent with the same TMDB ID
                    // as a standalone TMDB season while using different season numbering. Without
                    // episode-title metadata in SonarrEpisodeCache, a TMDB-only match cannot prove
                    // that source S1 is the same as Sonarr S1. A false negative is safer here than
                    // rejecting or mutating the wrong anthology request.
                    //
                    // Also never interpret ChildRequests.Id as a provider ID. It is the database
                    // primary key and is intentionally separate from RequestTheMovieDbId.
                    var monitoredEpisodes = new HashSet<(int SeasonNumber, int EpisodeNumber)>();

                    if (requestTvDbId > 0)
                    {
                        var tvDbEpisodes = await _ctx.SonarrEpisodeCache
                            .AsNoTracking()
                            .Where(x => x.TvDbId == requestTvDbId)
                            .Select(x => new { x.SeasonNumber, x.EpisodeNumber })
                            .ToListAsync();
                        monitoredEpisodes.UnionWith(
                            tvDbEpisodes.Select(x => (x.SeasonNumber, x.EpisodeNumber)));
                    }

                    if (monitoredEpisodes.Count > 0)
                    {
                        foreach (var season in vm.SeasonRequests)
                        {
                            season.Episodes.RemoveAll(ep =>
                                monitoredEpisodes.Contains((season.SeasonNumber, ep.EpisodeNumber)));
                        }

                        var anyEpisodes = vm.SeasonRequests.SelectMany(x => x.Episodes).Any();
                        if (!anyEpisodes)
                        {
                            return new RuleResult { ErrorCode = ErrorCode.EpisodesAlreadyRequested, Message = $"We already have episodes requested from series {vm.Title}" };
                        }
                    }
                }
            }
            return new RuleResult { Success = true };
        }

        public async Task<RuleResult> Execute(SearchViewModel obj)
        {
            if (obj.Type == RequestType.TvShow)
            {
                var vm = (SearchTvShowViewModel) obj;
                // Check if it's in Sonarr
                if (!vm.TheTvDbId.HasValue())
                {
                    return new RuleResult { Success = true };
                }
                if (!int.TryParse(vm.TheTvDbId, out var tvdbidint))
                {
                    return new RuleResult { Success = true };
                }

                var existsInSonarr = await _ctx.SonarrCache
                    .AsNoTracking()
                    .AnyAsync(x => x.TvDbId == tvdbidint);
                if (existsInSonarr)
                {
                    vm.Approved = true;

                    if (vm.SeasonRequests.Any())
                    {
                        // Fetch every cached Sonarr episode for this show in one query, then
                        // evaluate the TMDB episode list in memory. This removes the per-episode
                        // EF query pattern that made Discover very expensive for long-running shows.
                        var sonarrEpisodes = await _ctx.SonarrEpisodeCache
                            .AsNoTracking()
                            .Where(x => x.TvDbId == tvdbidint)
                            .Select(x => new { x.SeasonNumber, x.EpisodeNumber, x.HasFile })
                            .ToListAsync();

                        var monitoredEpisodes = sonarrEpisodes
                            .Select(x => (x.SeasonNumber, x.EpisodeNumber))
                            .ToHashSet();
                        var episodesWithFiles = sonarrEpisodes
                            .Where(x => x.HasFile)
                            .Select(x => (x.SeasonNumber, x.EpisodeNumber))
                            .ToHashSet();

                        foreach (var season in vm.SeasonRequests)
                        {
                            foreach (var ep in season.Episodes)
                            {
                                var episodeKey = (season.SeasonNumber, ep.EpisodeNumber);
                                if (!monitoredEpisodes.Contains(episodeKey))
                                {
                                    continue;
                                }

                                ep.Approved = true;
                                if (episodesWithFiles.Contains(episodeKey))
                                {
                                    ep.Available = true;
                                    obj.Available = true;
                                }
                            }
                        }

                        AvailabilityRuleHelper.CheckForUnairedEpisodes(vm);
                    }
                }
            }
            return new RuleResult { Success = true };
        }
    }
}