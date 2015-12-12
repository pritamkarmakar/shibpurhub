using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    public class FeedContentDetail
    {
        public string LogId { get; set; }

        public string Header { get; set; }
        
        public string SubHeader { get; set; }

        public string SimpleDetail { get; set; }

        public Dictionary<string, string> ComplexDetail { get; set; }

        public string ActionName { get; set; }

        public string ActionUrl { get; set; }

        public int ViewCount { get; set; }

        public DateTime? PostedDateInUTC { get; set; }

        public int AnswersCount { get; set; }
        
        public int FollowedByCount { get; set; }
        
        public bool IsFollowedByme { get; set; }
        
        public int UpvoteCount { get; set; }
        
        public bool IsUpvotedByme { get; set; }
    }
}