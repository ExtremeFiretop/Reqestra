using System;
using System.Collections.Generic;
using System.Linq;
using Ombi.Store.Entities.Requests;

namespace Ombi.Core.Engine
{
    public static class TvRequestGridAggregator
    {
        public static List<ChildRequests> CollapseToSeriesCards(IEnumerable<ChildRequests> requests)
        {
            return requests?
                .Where(x => x != null)
                .GroupBy(GetSeriesKey)
                .Select(group => group
                    .OrderByDescending(GetLifecyclePriority)
                    .ThenByDescending(x => x.RequestedDate)
                    .ThenByDescending(x => x.Id)
                    .First())
                .ToList() ?? new List<ChildRequests>();
        }

        private static string GetSeriesKey(ChildRequests request)
        {
            if (request.ParentRequestId > 0)
            {
                return $"parent:{request.ParentRequestId}";
            }

            if ((request.ParentRequest?.Id ?? 0) > 0)
            {
                return $"parent:{request.ParentRequest.Id}";
            }

            // Grid requests are persisted and normally always have a parent id. Keep malformed or
            // transient children independent rather than accidentally collapsing them by title.
            return $"child:{request.Id}:{request.RequestedDate.Ticks}";
        }

        private static int GetLifecyclePriority(ChildRequests request)
        {
            if (request.Downloading)
            {
                return 900;
            }

            if (request.Importing)
            {
                return 850;
            }

            if (request.Standby)
            {
                return 825;
            }

            if (request.Queued)
            {
                return 800;
            }

            if (!request.Approved && !request.Available && !(request.Denied ?? false))
            {
                return 700;
            }

            if (request.Approved && !request.Available && !(request.Denied ?? false) &&
                !request.Importing && !request.Standby && !request.Searching && !request.Completed)
            {
                return 600;
            }

            if (request.Searching)
            {
                return 500;
            }

            if (request.Completed)
            {
                return 400;
            }

            if (request.Available)
            {
                return 300;
            }

            if (request.Denied ?? false)
            {
                return 200;
            }

            return 0;
        }
    }
}
