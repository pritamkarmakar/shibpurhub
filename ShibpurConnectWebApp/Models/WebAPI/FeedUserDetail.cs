using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    public class FeedUserDetail
    {
        public string UserId { get; set; }

        public string FullName { get; set; }

        public string CareerDetail { get; set; }

        public string ImageUrl { get; set; }

        public int QuestionCount { get; set; }

        public int AnswerCount { get; set; }

        public int Reputation { get; set; }
        
        public bool IsFollowedByMe { get; set; }
        
    }
}