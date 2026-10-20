using Ombi.Api.External.ExternalApis.TheMovieDb.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ombi.Core.Services
{
    public interface IPersonalizedRecommendationService
    {
        Task<IReadOnlyList<MovieDbSearchResult>> GetMovieRecommendations(string languageCode, CancellationToken cancellationToken);
        Task<IReadOnlyList<MovieDbSearchResult>> GetTvRecommendations(string languageCode, CancellationToken cancellationToken);
    }
}
