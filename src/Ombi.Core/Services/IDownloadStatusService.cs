using System.Collections.Generic;
using System.Threading.Tasks;
using Ombi.Store.Entities.Requests;

namespace Ombi.Core.Services
{
    public interface IDownloadStatusService
    {
        Task PopulateMovieDownloadStatus(IEnumerable<MovieRequests> requests);
        Task PopulateTvDownloadStatus(IEnumerable<ChildRequests> requests);
    }
}
