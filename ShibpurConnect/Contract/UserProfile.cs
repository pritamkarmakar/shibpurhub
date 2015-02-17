using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShibpurConnect.Contract
{
    public class UserProfile
    {
        public int UserId { get; set; }

        public string FirstName { get; set; }

        public string lastName { get; set; }

        public string Address { get; set; }
    }
}