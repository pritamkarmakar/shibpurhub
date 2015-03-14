using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    /// <summary>
    /// Use this model for populating the user profile page with educational history and employment history
    /// </summary>
    public class UserProfile
    {
        public CustomUserInfo UserInfo { get; set; }
        public List<EducationalHistories> EducationalHistories { get; set; }
        public List<EmploymentHistories> EmploymentHistories { get; set; } 
    }
}