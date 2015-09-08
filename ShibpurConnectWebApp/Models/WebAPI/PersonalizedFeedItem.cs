using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    [Serializable]
    public class PersonalizedFeedItem
    {
        [DataMember]
        public int ActivityType { get; set; }

        [DataMember]
        public string UserName { get; set; }

        [DataMember]
        public string UserProfileUrl { get; set; }

        [DataMember]
        public string UserProfileImageUrl { get; set; }

        [DataMember]
        public string ActionText { get; set; }

        [DataMember]
        public string TargetAction { get; set; }

        [DataMember]
        public string TargetActionUrl { get; set; }

        [DataMember]
        public string UserDesignation { get; set; }

        [DataMember]
        public string ItemHeader { get; set; }

        [DataMember]
        public string ItemDetail { get; set; }

        [DataMember]
        public string ItemImageUrl { get; set; }

        [DataMember]
        public Dictionary<string, string> ItemValues { get; set; }

        public int ViewCount { get; set; }

        public DateTime? PostedDateInUTC { get; set; }

        public int AnswersCount { get; set; }
    }
}