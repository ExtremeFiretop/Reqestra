using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using Ombi.Store.Entities;
using Ombi.Store.Entities.Requests;

namespace Ombi.Store.Repository.Requests
{

    public class SeasonRequests : Entity
    {
        public int SeasonNumber { get; set; }
        public string Overview { get; set; }
        public List<EpisodeRequests> Episodes { get; set; } = new List<EpisodeRequests>();
        
        public int ChildRequestId { get; set; }
        [ForeignKey(nameof(ChildRequestId))]
        public ChildRequests ChildRequest { get; set; }
        [NotMapped] public bool SeasonAvailable { get; set; }
    }

    public class EpisodeRequests : Entity
    {
        public int EpisodeNumber { get; set; }
        public string Title { get; set; }
        public DateTime AirDate { get; set; }
        public string Url { get; set; }
        public bool Available { get; set; }
        public bool Approved { get; set; }
        public bool Requested { get; set; }
        [NotMapped]
        public bool? Denied { get; set; }
        [NotMapped]
        public string DeniedReason { get; set; }

        [NotMapped]
        public bool Downloading { get; set; }
        [NotMapped]
        public bool Queued { get; set; }
        [NotMapped]
        public bool Importing { get; set; }
        [NotMapped]
        public bool Standby { get; set; }
        [NotMapped]
        public bool Searching { get; set; }
        [NotMapped]
        public bool Completed { get; set; }

        public int SeasonId { get; set; }
        [ForeignKey(nameof(SeasonId))]
        public SeasonRequests Season { get; set; }

        [NotMapped] public string AirDateDisplay => AirDate == DateTime.MinValue ? "Unknown" : AirDate.ToString(CultureInfo.InvariantCulture);

        [NotMapped]
        public string RequestStatus
        {
            get
            {
                if (Downloading)
                {
                    return "Common.Downloading";
                }

                if (Importing)
                {
                    return "Common.Importing";
                }

                if (Standby)
                {
                    return "Common.Standby";
                }

                if (Queued)
                {
                    return "Common.Queued";
                }

                if (Available)
                {
                    return "Common.Available";
                }

                if (Denied ?? false)
                {
                    return "Common.Denied";
                }

                if (Completed)
                {
                    return "Common.Completed";
                }

                if (Searching)
                {
                    return "Common.Searching";
                }

                if (Approved & !Available)
                {
                    return "Common.ProcessingRequest";
                }

                if (Requested && !Approved && !Available)
                {
                    return "Common.PendingApproval";
                }

                return string.Empty;
            }
        }
    }
}