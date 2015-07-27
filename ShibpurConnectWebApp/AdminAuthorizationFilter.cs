using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Http.Filters;

namespace ShibpurConnectWebApp
{
    /// <summary>
    /// Hangfire authorization filter
    /// </summary>
    public class HangfireAuthorizationFilter : Hangfire.Dashboard.IAuthorizationFilter
    {
        public bool Authorize(IDictionary<string, object> owinEnvironment)
        {
            bool boolAuthorizeCurrentUserToAccessHangFireDashboard = false;

            if (HttpContext.Current.User.Identity.IsAuthenticated)
            {
                if (HttpContext.Current.User.IsInRole("admin"))
                    boolAuthorizeCurrentUserToAccessHangFireDashboard = true;
            }

            return boolAuthorizeCurrentUserToAccessHangFireDashboard;
        }
       
    }
}
