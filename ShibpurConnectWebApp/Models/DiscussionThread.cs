﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShibpurConnectWebApp.Models
{
    public class DiscussionThread
    {
        public string Question { get; set; }

        public string AskedBy { get; set; }

        public string DetailText { get; set; }

        public List<string> Categories { get; set; }

        public DateTime DatePosted { get; set; }

        public List<Answer> Answers { get; set; }
             
        public int NumberOfAnswers {
            get
            {
                return Answers.Count;
            }
        }

    }
}