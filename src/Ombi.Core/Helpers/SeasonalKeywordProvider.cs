using Microsoft.Extensions.Logging;
using Ombi.Api.External.ExternalApis.TheMovieDb;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Ombi.Core.Helpers
{
    public sealed class SeasonalKeywordProvider
    {
        private const string SeasonalKeywordUrl = "https://raw.githubusercontent.com/Ombi-app/Ombi.News/main/Seasonal.md";
        private static readonly TimeSpan RemoteRequestTimeout = TimeSpan.FromSeconds(5);
        private static readonly Regex KeywordExpression = new Regex(
            @"^\d+(?:\s*[|,]\s*\d+)*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly HttpClient _client;
        private readonly IMovieDbApi _movieApi;
        private readonly ILogger _logger;

        public SeasonalKeywordProvider(HttpClient client, IMovieDbApi movieApi, ILogger logger)
        {
            _client = client;
            _movieApi = movieApi;
            _logger = logger;
        }

        public async Task<string> ResolveAsync(DateTime today, CancellationToken cancellationToken)
        {
            var remoteKeywords = await TryGetRemoteKeywords(cancellationToken);
            if (!string.IsNullOrEmpty(remoteKeywords))
            {
                return remoteKeywords;
            }

            var fallbackKeyword = GetFallbackKeywordName(today);
            if (string.IsNullOrEmpty(fallbackKeyword))
            {
                return null;
            }

            try
            {
                var keywordMatches = await _movieApi.SearchKeyword(fallbackKeyword);
                var exactMatches = keywordMatches?
                    .Where(x => x.Id > 0 && string.Equals(x.Name, fallbackKeyword, StringComparison.OrdinalIgnoreCase))
                    .Take(2)
                    .ToList();

                if (exactMatches == null || exactMatches.Count != 1)
                {
                    _logger.LogWarning(
                        "No unique exact TMDB keyword match was found for the Seasonal fallback theme '{SeasonalTheme}'",
                        fallbackKeyword);
                    return null;
                }

                _logger.LogDebug(
                    "Using the date-based Seasonal fallback theme '{SeasonalTheme}' because the remote Seasonal keyword source was unavailable, empty, or invalid",
                    fallbackKeyword);
                return exactMatches[0].Id.ToString(CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Unable to resolve the TMDB keyword for Seasonal fallback theme '{SeasonalTheme}'",
                    fallbackKeyword);
                return null;
            }
        }

        public static string NormalizeKeywordExpression(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            var trimmed = content.Trim().TrimStart('\uFEFF').Trim();
            if (!KeywordExpression.IsMatch(trimmed))
            {
                return null;
            }

            var normalized = string.Concat(trimmed.Where(c => !char.IsWhiteSpace(c)));
            var keywordIds = normalized.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (keywordIds.Any(id => !int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedId) || parsedId <= 0))
            {
                return null;
            }

            return normalized;
        }

        public static string GetFallbackKeywordName(DateTime today)
        {
            return today.Month switch
            {
                10 => "halloween",
                12 => "christmas",
                _ => null,
            };
        }

        private async Task<string> TryGetRemoteKeywords(CancellationToken cancellationToken)
        {
            try
            {
                using var timeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutTokenSource.CancelAfter(RemoteRequestTimeout);

                using var response = await _client.GetAsync(SeasonalKeywordUrl, timeoutTokenSource.Token);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Seasonal keyword source returned HTTP {StatusCode}; a date-based fallback will be used when available",
                        (int)response.StatusCode);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync(timeoutTokenSource.Token);
                var normalized = NormalizeKeywordExpression(content);
                if (!string.IsNullOrEmpty(normalized))
                {
                    return normalized;
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    _logger.LogDebug("Seasonal keyword source is empty; a date-based fallback will be used when available");
                }
                else
                {
                    _logger.LogWarning("Seasonal keyword source contained an invalid TMDB keyword expression; a date-based fallback will be used when available");
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "Seasonal keyword source did not respond within {TimeoutSeconds} seconds; a date-based fallback will be used when available",
                    RemoteRequestTimeout.TotalSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Unable to read the Seasonal keyword source; a date-based fallback will be used when available");
            }

            return null;
        }
    }
}
