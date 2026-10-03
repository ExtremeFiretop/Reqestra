using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ombi.Core.Engine;
using Ombi.Core.Rule.Rules;
using Ombi.Core.Rule.Interfaces;
using Ombi.Store.Entities;
using Ombi.Store.Entities.Requests;
using Ombi.Store.Repository;
using Ombi.Store.Repository.Requests;

namespace Ombi.Core.Rule.Rules.Request
{
    public class ExistingPlexRequestRule : BaseRequestRule, IRules<BaseRequest>
    {
        public ExistingPlexRequestRule(IPlexContentRepository rv)
        {
            _plexContent = rv;
        }

        private readonly IPlexContentRepository _plexContent;

        /// <summary>
        /// We check if the request exists, if it does then we don't want to re-request it.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        public async Task<RuleResult> Execute(BaseRequest obj)
        {
            if (obj.RequestType == RequestType.TvShow)
            {
                var tvRequest = (ChildRequests) obj;

                var requestTheMovieDbId = tvRequest.RequestTheMovieDbId;
                var requestTheMovieDbIdString = requestTheMovieDbId > 0
                    ? requestTheMovieDbId.ToString()
                    : string.Empty;
                var requestTvDbIdString = tvRequest.RequestTvDbId > 0
                    ? tvRequest.RequestTvDbId.ToString()
                    : string.Empty;
                var requestImdbId = tvRequest.RequestImdbId ?? string.Empty;
                var hasTheMovieDbId = !string.IsNullOrEmpty(requestTheMovieDbIdString);
                var hasTvDbId = !string.IsNullOrEmpty(requestTvDbIdString);
                var hasImdbId = !string.IsNullOrEmpty(requestImdbId);

                var tvContent = _plexContent.GetAll().Include(x => x.Episodes).Where(x => x.Type == MediaType.Series);

                // Prefer the request's exact TMDB identity before falling back to alternate ids.
                // This matters when old duplicate rows already exist from a previous provider-id
                // migration: an exact current identity should always win over an alias match.
                PlexServerContent providerIdMatch = null;
                if (hasTheMovieDbId)
                {
                    providerIdMatch = await tvContent.FirstOrDefaultAsync(x => x.TheMovieDbId == requestTheMovieDbIdString);
                }
                if (providerIdMatch == null && hasTvDbId)
                {
                    providerIdMatch = await tvContent.FirstOrDefaultAsync(x => x.TvDbId == requestTvDbIdString);
                }
                if (providerIdMatch == null && hasImdbId)
                {
                    providerIdMatch = await tvContent.FirstOrDefaultAsync(x => x.ImdbId == requestImdbId);
                }

                if (providerIdMatch != null)
                {
                    var providerFingerprintMatch = await PlexEpisodeFingerprintMatcher.FindSingleSeasonMatch(
                        _plexContent,
                        tvRequest.SeasonRequests,
                        providerIdMatch.Id);
                    if (providerFingerprintMatch != null)
                    {
                        return CheckExistingContent(
                            tvRequest,
                            providerIdMatch,
                            providerFingerprintMatch.SourceSeasonNumber,
                            providerFingerprintMatch.PlexSeasonNumber);
                    }

                    return CheckExistingContent(tvRequest, providerIdMatch);
                }

                // Provider ids can be incomplete on older Plex metadata. Keep the existing
                // title/year fallback as a last resort.
                var titleAndYearMatch = await tvContent.FirstOrDefaultAsync(x =>
                    x.Title == tvRequest.Title
                    && x.ReleaseYear == tvRequest.ReleaseYear.Year.ToString());
                if (titleAndYearMatch != null)
                {
                    var titleFingerprintMatch = await PlexEpisodeFingerprintMatcher.FindSingleSeasonMatch(
                        _plexContent,
                        tvRequest.SeasonRequests,
                        titleAndYearMatch.Id);
                    if (titleFingerprintMatch != null)
                    {
                        return CheckExistingContent(
                            tvRequest,
                            titleAndYearMatch,
                            titleFingerprintMatch.SourceSeasonNumber,
                            titleFingerprintMatch.PlexSeasonNumber);
                    }

                    return CheckExistingContent(tvRequest, titleAndYearMatch);
                }

                var fingerprintMatch = await PlexEpisodeFingerprintMatcher.FindSingleSeasonMatch(
                    _plexContent,
                    tvRequest.SeasonRequests);
                if (fingerprintMatch != null)
                {
                    return CheckExistingContent(
                        tvRequest,
                        fingerprintMatch.Content,
                        fingerprintMatch.SourceSeasonNumber,
                        fingerprintMatch.PlexSeasonNumber);
                }

                return Success();
            }
            if (obj.RequestType == RequestType.Movie)
            {
                var movie = (MovieRequests)obj;
                var exists = _plexContent.GetAll().Where(x => x.Type == MediaType.Movie).Any(x => x.TheMovieDbId == movie.Id.ToString() || x.TheMovieDbId == movie.TheMovieDbId.ToString());
                if (exists)
                {
                    return Fail(ErrorCode.AlreadyRequested, "This movie is already available." );
                }
            }
            return Success();
        }


        private RuleResult CheckExistingContent(
            ChildRequests child,
            PlexServerContent content,
            int? sourceSeasonNumber = null,
            int? plexSeasonNumber = null)
        {
            foreach (var season in child.SeasonRequests)
            {
                var episodesToRemove = new List<EpisodeRequests>();
                var seasonNumberToCheck = sourceSeasonNumber.HasValue && plexSeasonNumber.HasValue &&
                                          season.SeasonNumber == sourceSeasonNumber.Value
                    ? plexSeasonNumber.Value
                    : season.SeasonNumber;
                var currentSeasonRequest =
                    content.Episodes.Where(x => x.SeasonNumber == seasonNumberToCheck).ToList();
                if (!currentSeasonRequest.Any())
                {
                    continue;
                }
                foreach (var e in season.Episodes)
                {
                    var existingEpRequest = currentSeasonRequest.FirstOrDefault(x => x.EpisodeNumber == e.EpisodeNumber);
                    if (existingEpRequest != null)
                    {
                        episodesToRemove.Add(e);
                    }
                }

                episodesToRemove.ForEach(x =>
                {
                    season.Episodes.Remove(x);
                });
            }

            var anyEpisodes = child.SeasonRequests.SelectMany(x => x.Episodes).Any();

            if (!anyEpisodes)
            {
                return Fail(ErrorCode.EpisodesAlreadyRequested, $"We already have episodes requested from series {child.Title}");
            }

            return Success();
        }
    }
}