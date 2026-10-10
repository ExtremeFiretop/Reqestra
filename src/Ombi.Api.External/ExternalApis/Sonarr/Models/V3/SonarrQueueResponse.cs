using System;
using System.Collections.Generic;
using Ombi.Api.External.ExternalApis.Sonarr.Models;

namespace Ombi.Api.External.ExternalApis.Sonarr.Models.V3
{
    public class SonarrQueueResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public List<SonarrQueueRecord> Records { get; set; } = new List<SonarrQueueRecord>();
    }

    public class SonarrQueueRecord
    {
        public int? SeriesId { get; set; }
        public int? EpisodeId { get; set; }
        public int? SeasonNumber { get; set; }
        public SonarrSeries Series { get; set; }
        public Episode Episode { get; set; }
        public decimal Size { get; set; }
        public decimal Sizeleft { get; set; }
        public TimeSpan? Timeleft { get; set; }
        public DateTime? EstimatedCompletionTime { get; set; }
        public string DownloadId { get; set; }
    }
}
