using System;
using System.Linq;
using System.Threading.Tasks;
using Ombi.Core.Models.Search;
using Ombi.Core.Models.Search.V2.Music;
using Ombi.Core.Helpers;
using Ombi.Core.Rule.Interfaces;
using Ombi.Store.Entities;
using Ombi.Store.Entities.Requests;
using Ombi.Store.Repository.Requests;

namespace Ombi.Core.Rule.Rules.Search
{
    public class ExistingRule : BaseSearchRule, IRules<SearchViewModel>
    {
        public ExistingRule(IMovieRequestRepository movie, ITvRequestRepository tv, IMusicRequestRepository music)
        {
            Movie = movie;
            Tv = tv;
            Music = music;
        }

        private IMovieRequestRepository Movie { get; }
        private IMusicRequestRepository Music { get; }
        private ITvRequestRepository Tv { get; }

        public async Task<RuleResult> Execute(SearchViewModel obj)
        {
            if (obj is SearchMovieViewModel movie)
            {
                var movieRequests = await Movie.GetRequestAsync(obj.Id);
                if (movieRequests != null) // Do we already have a request for this?
                {
                    // If the RequestDate is a min value, that means there's only a 4k request
                    movie.Requested = movieRequests.RequestedDate != DateTime.MinValue;
                    movie.RequestId = movieRequests.Id;
                    movie.Approved = movieRequests.Approved;
                    movie.Denied = movieRequests.Denied ?? false;
                    movie.DeniedReason = movieRequests.DeniedReason;
                    movie.Available = movieRequests.Available;
                    movie.Has4KRequest = movieRequests.Has4KRequest;
                    movie.RequestedDate4k = movieRequests.RequestedDate4k;
                    movie.Approved4K = movieRequests.Approved4K;
                    movie.Available4K = movieRequests.Available4K;
                    movie.Denied4K = movieRequests.Denied4K;
                    movie.DeniedReason4K = movieRequests.DeniedReason4K;
                    movie.MarkedAsApproved4K = movieRequests.MarkedAsApproved4K;
                    movie.MarkedAsAvailable4K = movieRequests.MarkedAsAvailable4K;
                    movie.MarkedAsDenied4K = movieRequests.MarkedAsDenied4K;

                    return Success();
                }
                return Success();
            }
            if (obj.Type == RequestType.TvShow)
            {
                var request = (SearchTvShowViewModel)obj;
                var tvRequests = Tv.GetRequest(obj.Id);
                TvRequestAliasIdentityMatch aliasMatch = null;

                if (tvRequests == null)
                {
                    var source = BuildSearchRequestIdentity(request);
                    var candidateQuery = Tv.Get();
                    if (candidateQuery != null)
                    {
                        var hasTvDbId = source.RequestTvDbId > 0;
                        var hasImdbId = !string.IsNullOrWhiteSpace(source.RequestImdbId);
                        var sourceYear = source.ReleaseYear.Year;
                        var hasMetadata = !string.IsNullOrWhiteSpace(source.Title) && sourceYear > 1;

                        if (hasTvDbId || hasImdbId || hasMetadata)
                        {
                            var candidates = candidateQuery
                                .Where(x =>
                                    (hasTvDbId && x.TvDbId == source.RequestTvDbId) ||
                                    (hasImdbId && x.ImdbId == source.RequestImdbId) ||
                                    (hasMetadata && x.Title == source.Title && x.ReleaseDate.Year == sourceYear))
                                .ToList();

                            aliasMatch = TvRequestSeasonIdentityMatcher.FindSafeAliasMatch(source, candidates);
                            tvRequests = aliasMatch?.Parent;
                        }
                    }
                }

                if (tvRequests != null) // Do we already have a request for this?
                {
                    request.RequestId = tvRequests.Id;
                    request.Requested = true;

                    var targetSeasonNumbers = new System.Collections.Generic.HashSet<int>();
                    if (aliasMatch == null || aliasMatch.UseLiteralSeasonNumbers)
                    {
                        foreach (var season in request.SeasonRequests)
                        {
                            targetSeasonNumbers.Add(season.SeasonNumber);
                        }
                    }
                    else
                    {
                        foreach (var targetSeason in aliasMatch.SeasonMappings.Values)
                        {
                            targetSeasonNumbers.Add(targetSeason);
                        }
                    }

                    var relevantChildren = tvRequests.ChildRequests
                        .Where(x => targetSeasonNumbers.Count == 0 ||
                                    x.SeasonRequests.Any(s => targetSeasonNumbers.Contains(s.SeasonNumber)))
                        .ToList();
                    request.Approved = relevantChildren.Any(x => x.Approved);

                    // Reflect existing requests in the search result. Provider aliases only compare
                    // against the season number proven by the identity matcher; a shared TVDB/IMDb
                    // parent does not imply equivalent season numbering.
                    foreach (var season in request.SeasonRequests)
                    {
                        var targetSeasonNumber = season.SeasonNumber;
                        if (aliasMatch != null && !aliasMatch.UseLiteralSeasonNumbers &&
                            !aliasMatch.SeasonMappings.TryGetValue(season.SeasonNumber, out targetSeasonNumber))
                        {
                            continue;
                        }

                        foreach (var existingRequestChildRequest in relevantChildren)
                        {
                            var existingSeason = existingRequestChildRequest.SeasonRequests
                                .FirstOrDefault(x => x.SeasonNumber == targetSeasonNumber);
                            if (existingSeason == null) continue;

                            foreach (var ep in existingSeason.Episodes)
                            {
                                var episodeSearching = season.Episodes
                                    .FirstOrDefault(x => x.EpisodeNumber == ep.EpisodeNumber);
                                if (episodeSearching == null)
                                {
                                    continue;
                                }

                                episodeSearching.Requested = true;
                                episodeSearching.Available = ep.Available;
                                episodeSearching.Approved = existingRequestChildRequest.Approved;
                                episodeSearching.Denied = existingRequestChildRequest.Denied;
                                episodeSearching.DeniedReason = existingRequestChildRequest.DeniedReason;
                            }
                        }
                    }

                    if (request.SeasonRequests.Any() &&
                        request.SeasonRequests.All(x => x.Episodes.All(e => e.Denied ?? false)))
                    {
                        request.Denied = true;
                        request.DeniedReason = relevantChildren.FirstOrDefault(x => x.Denied ?? false)?.DeniedReason;
                    }
                }

                AvailabilityRuleHelper.CheckForUnairedEpisodes(request);

                return Success();
            }
            if (obj.Type == RequestType.Album)
            {
                if (obj is SearchAlbumViewModel album)
                {
                    var albumRequest = await Music.GetRequestAsync(album.ForeignAlbumId);
                    if (albumRequest != null) // Do we already have a request for this?
                    {
                        obj.Requested = true;
                        obj.RequestId = albumRequest.Id;
                        obj.Denied = albumRequest.Denied;
                        obj.DeniedReason = albumRequest.DeniedReason;
                        obj.Approved = albumRequest.Approved;
                        obj.Available = albumRequest.Available;

                        return Success();
                    }
                }
                if (obj is ReleaseGroup release)
                {
                    var albumRequest = await Music.GetRequestAsync(release.Id);
                    if (albumRequest != null) // Do we already have a request for this?
                    {
                        obj.Requested = true;
                        obj.RequestId = albumRequest.Id;
                        obj.Approved = albumRequest.Approved;
                        obj.Available = albumRequest.Available;

                        return Success();
                    }
                }

                return Success();
            }
            return Success();
        }

        private static ChildRequests BuildSearchRequestIdentity(SearchTvShowViewModel request)
        {
            var releaseYear = DateTime.MinValue;
            if (!string.IsNullOrWhiteSpace(request.FirstAired))
            {
                DateTime.TryParse(request.FirstAired, out releaseYear);
            }

            _ = int.TryParse(request.TheTvDbId, out var tvDbId);

            return new ChildRequests
            {
                Title = request.Title,
                ReleaseYear = releaseYear,
                RequestTheMovieDbId = request.Id,
                RequestTvDbId = tvDbId,
                RequestImdbId = request.ImdbId,
                SeasonRequests = request.SeasonRequests
            };
        }

    }
}
