using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Ombi.Store.Repository.Requests;

namespace Ombi.Store.Entities.Requests
{
    [Table("ChildRequests")]
    public class ChildRequests : BaseRequest
    {
        [ForeignKey(nameof(ParentRequestId))]
        public TvRequests ParentRequest { get; set; }
        public int ParentRequestId { get; set; }
        public int? IssueId { get; set; }
        public SeriesType SeriesType { get; set; }
        public int? QualityOverride { get; set; }

        /// <summary>
        /// This is to see if the user is subscribed in the UI
        /// </summary>
        [NotMapped]
        public bool Subscribed { get; set; }

        [NotMapped]
        public bool ShowSubscribe { get; set; }

        [NotMapped]
        public DateTime ReleaseYear { get; set; } // Used in the ExistingPlexRequestRule.cs

        // Request-time provider identities used by the duplicate/content rules before this child
        // has been persisted. Keep these separate from Entity.Id so a provider id can never be
        // accidentally written as the ChildRequests primary key.
        [NotMapped]
        [JsonIgnore]
        public int RequestTheMovieDbId { get; set; }

        [NotMapped]
        [JsonIgnore]
        public int RequestTvDbId { get; set; }

        [NotMapped]
        [JsonIgnore]
        public string RequestImdbId { get; set; }

        // Request-time alias resolution hints. ExistingTvRequestRule can establish a safe anthology
        // parent/season mapping before other request rules remove episodes from this transient graph.
        // Preserve that mapping for TvRequestEngine so later attachment does not have to infer the
        // relationship again from a potentially reduced episode list.
        [NotMapped]
        [JsonIgnore]
        public int RequestExistingParentId { get; set; }

        [NotMapped]
        [JsonIgnore]
        public Dictionary<int, int> RequestSeasonMappings { get; set; } = new Dictionary<int, int>();

        [ForeignKey(nameof(IssueId))]
        public List<Issues> Issues { get; set; }

        public List<SeasonRequests> SeasonRequests { get; set; }

        [NotMapped]
        public string RequestStatus
        {
            get
            {
                if (Available)
                {
                    return "Common.Available";
                }

                if (Denied ?? false)
                {
                    return "Common.Denied";
                }

                if (Approved & !Available)
                {
                    return "Common.ProcessingRequest";
                }

                if (!Approved && !Available)
                {
                    return "Common.PendingApproval";
                }

                return string.Empty;
            }
        }

        [NotMapped]
        public int RequestedUserPlayedProgress { get; set; }
    }

    public enum SeriesType
    {
        Standard = 0,
        Anime = 1
    }
}