using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace ShibpurConnectWebApp.Routes
{
    class SubdomainRoute : RouteBase
    {
        public override RouteData GetRouteData(HttpContextBase httpContext)
        {
            var url = httpContext.Request.Headers["HOST"];
            var index = url.IndexOf(".");
            Trace.Write("Hey, this is nice");
            if (index < 0)
                return null;

            var subDomain = url.Substring(0, index);

            if (subDomain == "blogs")
            {
                var routeData = new RouteData(this, new MvcRouteHandler());

                routeData.Values.Add("controller", "Blogs");
                routeData.Values.Add("action", "Index");
                //routeData.Values.Add("deptName", subDomain);

                return routeData;
            }

            return null;
        }

        public override VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary values)
        {
            //Implement your formating Url formating here
            return null;
        }
    }
}