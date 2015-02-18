using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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

        public IList<Answer> Answers { get; set; }
             
        public int NumberOfAnswers {
            get
            {
                return Answers.Count;
            }
        }

    }
}