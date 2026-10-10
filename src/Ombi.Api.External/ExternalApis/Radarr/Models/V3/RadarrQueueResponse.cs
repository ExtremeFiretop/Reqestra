using System;
using System.Collections.Generic;
using Ombi.Api.External.ExternalApis.Radarr.Models;

namespace Ombi.Api.External.ExternalApis.Radarr.Models.V3
{
    public class RadarrQueueResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public List<RadarrQueueRecord> Records { get; set; } = new List<RadarrQueueRecord>();
    }

    public class RadarrQueueRecord
    {
        public int? MovieId { get; set; }
        public MovieResponse Movie { get; set; }
        public decimal Size { get; set; }
        public decimal Sizeleft { get; set; }
        public TimeSpan? Timeleft { get; set; }
        public DateTime? EstimatedCompletionTime { get; set; }
        public string DownloadId { get; set; }
    }
}
