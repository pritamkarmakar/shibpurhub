using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    public class FeedContentDetail
    {
        public string Header { get; set; }

        public string SimpleDetail { get; set; }

        public Dictionary<string, string> ComplexDetail { get; set; }

        public int ViewCount { get; set; }

        public DateTime? PostedDateInUTC { get; set; }

        public int AnswersCount { get; set; }
    }
}