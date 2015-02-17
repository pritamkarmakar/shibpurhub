using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShibpurConnectWebApp.Models
{
    public class FeedViewModel
    {
        public int ItemsPerPage
        {
            get
            {
                return 10;
            }
        }

        public int? UserID { get; set; }

        public string Category { get; set; }

        public IList<DiscussionThread> Threads { get; set; }

        public int Page { get; set; }

        public int PageCount
        {
            get
            {
                var pageCount = this.Threads.Count / ItemsPerPage;
                return this.Threads.Count % ItemsPerPage == 0 ? pageCount : pageCount + 1;
            }
        }

    }
}