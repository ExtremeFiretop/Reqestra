using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ombi.Api.External.ExternalApis.TheMovieDb;
using Ombi.Api.External.ExternalApis.TheMovieDb.Models;
using Ombi.Core.Helpers;
using Ombi.Helpers;
using Ombi.Store.Repository.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ombi.Core.Services
{
    public class PersonalizedRecommendationService : IPersonalizedRecommendationService
    {
        private const int SeedLimit = 10;
        private const int SeedHistoryScanLimit = 20;
        private const int MinimumPrimaryResultsPerSeed = 8;
        private const int MatchCountScoreWeight = 100_000;
        private const int RecommendationSourceWeight = 100;
        private const int SimilarSourceWeight = 50;
        private const int MaxConcurrentProviderRequests = 4;

        private const string MovieMediaType = "movie";
        private const string TvMediaType = "tv";

        private static readonly SemaphoreSlim ProviderRequestLimiter =
            new(MaxConcurrentProviderRequests, MaxConcurrentProviderRequests);

        private readonly IMovieRequestRepository _movieRequestRepository;
        private readonly ITvRequestRepository _tvRequestRepository;
        private readonly IMovieDbApi _movieDbApi;
        private readonly ICurrentUser _currentUser;
        private readonly ICacheService _cache;
        private readonly ILogger<PersonalizedRecommendationService> _logger;

        public PersonalizedRecommendationService(
            IMovieRequestRepository movieRequestRepository,
            ITvRequestRepository tvRequestRepository,
            IMovieDbApi movieDbApi,
            ICurrentUser currentUser,
            ICacheService cache,
            ILogger<PersonalizedRecommendationService> logger)
        {
            _movieRequestRepository = movieRequestRepository;
            _tvRequestRepository = tvRequestRepository;
            _movieDbApi = movieDbApi;
            _currentUser = currentUser;
            _cache = cache;
            _logger = logger;
        }

        public Task<IReadOnlyList<MovieDbSearchResult>> GetMovieRecommendations(
            string languageCode,
            CancellationToken cancellationToken)
        {
            return GetRecommendations(MovieMediaType, languageCode, cancellationToken);
        }

        public Task<IReadOnlyList<MovieDbSearchResult>> GetTvRecommendations(
            string languageCode,
            CancellationToken cancellationToken)
        {
            return GetRecommendations(TvMediaType, languageCode, cancellationToken);
        }

        private async Task<IReadOnlyList<MovieDbSearchResult>> GetRecommendations(
            string mediaType,
            string languageCode,
            CancellationToken cancellationToken)
        {
            var user = await _currentUser.GetUser();
            if (user == null || string.IsNullOrEmpty(user.Id))
            {
                return Array.Empty<MovieDbSearchResult>();
            }

            var seeds = await GetRecentSeeds(user.Id, cancellationToken);
            if (seeds.Count == 0)
            {
                // Do not cache the net-new-user case. Their first request should make
                // recommendations appear immediately on the next Discover load.
                return Array.Empty<MovieDbSearchResult>();
            }

            var matchingSeeds = seeds
                .Select((seed, index) => new RankedSeed(seed, index))
                .Where(x => x.Seed.MediaType == mediaType)
                .ToList();

            if (matchingSeeds.Count == 0)
            {
                return Array.Empty<MovieDbSearchResult>();
            }

            var seedSignature = string.Join("|", seeds.Select(x =>
                $"{x.MediaType}:{x.MediaId}:{x.RequestedDate.Ticks}:{x.RequestId}"));
            var cacheKey = $"PersonalizedRecommendations|{user.Id}|{mediaType}|{languageCode}|{seedSignature}";

            var ranked = await _cache.GetOrAddAsync(
                cacheKey,
                () => BuildRankedRecommendations(matchingSeeds, mediaType, languageCode, cancellationToken),
                DateTimeOffset.Now.AddHours(4));

            if (ranked.Count == 0)
            {
                // Do not pin an empty provider result for four hours. This also means a
                // transient provider failure across every seed can recover on the next load.
                _cache.Remove(cacheKey);
                return Array.Empty<MovieDbSearchResult>();
            }

            // Exclude everything this user has already requested, not only the ten seed
            // titles. This query stays outside the four-hour recommendation cache so a new
            // request is never suggested back to the user even if provider data is reused.
            var requestedIds = await GetRequestedIds(user.Id, mediaType, cancellationToken);
            return ranked.Where(x => !requestedIds.Contains(x.Id)).ToList();
        }

        private async Task<List<RecommendationSeed>> GetRecentSeeds(string userId, CancellationToken cancellationToken)
        {
            // Scan a small window from each request table, deduplicate repeated requests for
            // the same title/show, then choose the newest SeedLimit across both media types.
            // The wider scan lets repeated TV season requests backfill with older distinct
            // titles without turning this into an unbounded history query.
            var movieSeeds = await _movieRequestRepository.GetAll()
                .Where(x => x.RequestedUserId == userId && x.TheMovieDbId > 0)
                .OrderByDescending(x => x.RequestedDate)
                .ThenByDescending(x => x.Id)
                .Take(SeedHistoryScanLimit)
                .Select(x => new RecommendationSeed
                {
                    MediaType = MovieMediaType,
                    MediaId = x.TheMovieDbId,
                    RequestedDate = x.RequestedDate,
                    RequestId = x.Id
                })
                .ToListAsync(cancellationToken);

            var tvSeeds = await _tvRequestRepository.GetChild(userId)
                .Where(x => x.ParentRequest != null && x.ParentRequest.ExternalProviderId > 0)
                .OrderByDescending(x => x.RequestedDate)
                .ThenByDescending(x => x.Id)
                .Take(SeedHistoryScanLimit)
                .Select(x => new RecommendationSeed
                {
                    MediaType = TvMediaType,
                    MediaId = x.ParentRequest.ExternalProviderId,
                    RequestedDate = x.RequestedDate,
                    RequestId = x.Id
                })
                .ToListAsync(cancellationToken);

            return movieSeeds
                .Concat(tvSeeds)
                .GroupBy(x => new { x.MediaType, x.MediaId })
                .Select(x => x.OrderByDescending(y => y.RequestedDate).ThenByDescending(y => y.RequestId).First())
                .OrderByDescending(x => x.RequestedDate)
                .ThenByDescending(x => x.RequestId)
                .Take(SeedLimit)
                .ToList();
        }

        private async Task<HashSet<int>> GetRequestedIds(
            string userId,
            string mediaType,
            CancellationToken cancellationToken)
        {
            if (mediaType == MovieMediaType)
            {
                var ids = await _movieRequestRepository.GetAll()
                    .Where(x => x.RequestedUserId == userId && x.TheMovieDbId > 0)
                    .Select(x => x.TheMovieDbId)
                    .Distinct()
                    .ToListAsync(cancellationToken);
                return ids.ToHashSet();
            }

            var tvIds = await _tvRequestRepository.GetChild(userId)
                .Where(x => x.ParentRequest != null && x.ParentRequest.ExternalProviderId > 0)
                .Select(x => x.ParentRequest.ExternalProviderId)
                .Distinct()
                .ToListAsync(cancellationToken);
            return tvIds.ToHashSet();
        }

        private async Task<List<MovieDbSearchResult>> BuildRankedRecommendations(
            IReadOnlyCollection<RankedSeed> seeds,
            string mediaType,
            string languageCode,
            CancellationToken cancellationToken)
        {
            var seedResults = await Task.WhenAll(seeds.Select(seed =>
                GetSeedRecommendations(seed, mediaType, languageCode, cancellationToken)));

            var candidates = new Dictionary<int, ScoredCandidate>();
            foreach (var result in seedResults)
            {
                foreach (var contribution in result)
                {
                    if (contribution.Item == null || contribution.Item.Id <= 0)
                    {
                        continue;
                    }

                    if (!candidates.TryGetValue(contribution.Item.Id, out var candidate))
                    {
                        candidate = new ScoredCandidate(contribution.Item);
                        candidates.Add(contribution.Item.Id, candidate);
                    }

                    candidate.AddContribution(contribution.SeedKey, contribution.Score);
                }
            }

            return candidates.Values
                .OrderByDescending(x => x.MatchCount)
                .ThenByDescending(x => x.TotalScore)
                .ThenByDescending(x => x.Item.Popularity)
                .ThenByDescending(x => x.Item.VoteCount)
                .ThenBy(x => x.Item.Id)
                .Select(x =>
                {
                    // Encode the overlap preference into the score as well so the frontend
                    // can correctly merge the independently paged movie and TV lists.
                    x.Item.RecommendationScore = (x.MatchCount * MatchCountScoreWeight) + x.TotalScore;
                    return x.Item;
                })
                .ToList();
        }

        private async Task<List<CandidateContribution>> GetSeedRecommendations(
            RankedSeed rankedSeed,
            string mediaType,
            string languageCode,
            CancellationToken cancellationToken)
        {
            await ProviderRequestLimiter.WaitAsync(cancellationToken);
            try
            {
                var primary = await _movieDbApi.GetRecommendations(
                    rankedSeed.Seed.MediaId,
                    mediaType,
                    languageCode,
                    cancellationToken) ?? new List<MovieDbSearchResult>();

                var contributions = BuildContributions(
                    primary,
                    rankedSeed,
                    RecommendationSourceWeight).ToList();

                // TMDB recommendations are the preferred signal. Only ask for Similar
                // when that primary result is sparse, then use it as a lower-weight source.
                if (contributions.Count < MinimumPrimaryResultsPerSeed)
                {
                    try
                    {
                        var similar = await _movieDbApi.GetSimilar(
                            rankedSeed.Seed.MediaId,
                            mediaType,
                            languageCode,
                            cancellationToken) ?? new List<MovieDbSearchResult>();
                        contributions.AddRange(BuildContributions(similar, rankedSeed, SimilarSourceWeight));
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        // Similar is only a fallback. Keep the primary recommendations if
                        // this secondary provider request happens to fail.
                        _logger.LogWarning(ex,
                            "Unable to load similar media for personalized {MediaType} seed {MediaId}",
                            mediaType,
                            rankedSeed.Seed.MediaId);
                    }
                }

                // A title can appear in both recommendations and similar for the same seed.
                // Keep only the stronger contribution from that seed so it cannot double-vote.
                return contributions
                    .GroupBy(x => x.Item.Id)
                    .Select(x => x.OrderByDescending(y => y.Score).First())
                    .ToList();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // One bad provider seed should not take down recommendations generated by
                // the user's other requests.
                _logger.LogWarning(ex,
                    "Unable to load personalized recommendations for {MediaType} seed {MediaId}",
                    mediaType,
                    rankedSeed.Seed.MediaId);
                return new List<CandidateContribution>();
            }
            finally
            {
                ProviderRequestLimiter.Release();
            }
        }

        private static IEnumerable<CandidateContribution> BuildContributions(
            IEnumerable<MovieDbSearchResult> items,
            RankedSeed rankedSeed,
            int sourceWeight)
        {
            var recencyWeight = Math.Max(1, SeedLimit - rankedSeed.GlobalPosition);
            var rank = 0;
            foreach (var item in items.Where(x => x != null && x.Id > 0))
            {
                var providerRankBonus = Math.Max(1, 20 - rank);
                yield return new CandidateContribution(
                    item,
                    $"{rankedSeed.Seed.MediaType}:{rankedSeed.Seed.MediaId}",
                    (recencyWeight * sourceWeight) + providerRankBonus);
                rank++;
            }
        }

        private sealed class RecommendationSeed
        {
            public string MediaType { get; set; }
            public int MediaId { get; set; }
            public DateTime RequestedDate { get; set; }
            public int RequestId { get; set; }
        }

        private sealed class RankedSeed
        {
            public RankedSeed(RecommendationSeed seed, int globalPosition)
            {
                Seed = seed;
                GlobalPosition = globalPosition;
            }

            public RecommendationSeed Seed { get; }
            public int GlobalPosition { get; }
        }

        private sealed class CandidateContribution
        {
            public CandidateContribution(MovieDbSearchResult item, string seedKey, int score)
            {
                Item = item;
                SeedKey = seedKey;
                Score = score;
            }

            public MovieDbSearchResult Item { get; }
            public string SeedKey { get; }
            public int Score { get; }
        }

        private sealed class ScoredCandidate
        {
            private readonly Dictionary<string, int> _seedScores = new();

            public ScoredCandidate(MovieDbSearchResult item)
            {
                Item = item;
            }

            public MovieDbSearchResult Item { get; }
            public int MatchCount => _seedScores.Count;
            public int TotalScore => _seedScores.Values.Sum();

            public void AddContribution(string seedKey, int score)
            {
                if (!_seedScores.TryGetValue(seedKey, out var existing) || score > existing)
                {
                    _seedScores[seedKey] = score;
                }
            }
        }
    }
}
