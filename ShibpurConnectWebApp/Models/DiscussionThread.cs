using System;
using System.Collections.Generic;

namespace ShibpurConnectWebApp.Models
{
    public class DiscussionThread
    {
        public int ThreadID { get; set; }

        public string Question { get; set; }

        public string AskedBy { get; set; }

        public string DetailText { get; set; }

        public IList<string> Categories { get; set; }

        public DateTime DatePosted { get; set; }

        public string DatePostedFormatted
        {
            get
            {
                return this.DatePosted.ToString("MMM") + " " + this.DatePosted.ToString("dd") + "'" + this.DatePosted.ToString("yy");
            }
        }

        public IList<Answer> Answers { get; set; }
             
        public string NumberOfAnswers {
            get
            {
                var count = Answers == null ? 0 : Answers.Count;
                return count.ToString() + " answers";
            }
        }

    }
}