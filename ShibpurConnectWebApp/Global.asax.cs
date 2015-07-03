using System;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using ShibpurConnectWebApp.App_Start;
using ShibpurConnectWebApp.Controllers;
using System.Web;
using ShibpurConnectWebApp.Controllers.WebAPI;
using System.Configuration;
using ShibpurConnectWebApp.Models.WebAPI;

namespace ShibpurConnectWebApp
{
    public class MvcApplication : System.Web.HttpApplication
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.MapRoute(
                "Categories",                                           // Route name
                "Feed/Categories/{category}",                            // URL with parameters
                new { controller = "Feed", action = "Categories" }  // Parameter defaults
            );

            routes.MapRoute(
                "FeedDetails",                                           // Route name
                "Feed/{id}",                            // URL with parameters
                new { controller = "Feed", action = "Details" }  // Parameter defaults
            );
            
            routes.MapRoute(
                "Default",                                              // Route name
                "{controller}/{action}/{id}",                           // URL with parameters
                new { controller = "Home", action = "Index", id = "" }  // Parameter defaults
            );
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            // For the Web API
            GlobalConfiguration.Configure(WebApiConfig.Register);

            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            //RouteConfig.RegisterRoutes(RouteTable.Routes);
            RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // to solve the EF error "The model backing the ‘ctx’ context has changed since the database was created. Consider using Code First Migrations to update the database"
            //Database.SetInitializer<ApplicationDbContext>(null);

            GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
        }

        protected void Application_Error()
        {
            var exception = Server.GetLastError();
            var httpException = exception as HttpException;
            Response.Clear();
            Server.ClearError();
            var routeData = new RouteData();
            routeData.Values["controller"] = "Errors";

            WebsiteAlertController alertController = new WebsiteAlertController();
            WebsiteAlert webSiteAlert = new WebsiteAlert()
            {
                AlertId = Guid.NewGuid().ToString(),
                AlertTime = DateTime.UtcNow,
                Source = exception.Source,
                EmailSentTo = ConfigurationManager.AppSettings["adminsEmail"]
            };

            routeData.Values["action"] = "General";

            routeData.Values["exception"] = exception;
            int statusCode = 500;
            if (httpException != null)
            {
                statusCode = httpException.GetHttpCode();
                switch (statusCode)
                {
                    case 403:
                        routeData.Values["action"] = "Http403";
                        break;
                    case 404:
                        routeData.Values["action"] = "Http404";
                        break;
                }
            }           

            // if the error is coming from Mongodb then go to Maintenance view
            if (exception.Source == "MongoDB.Driver")
            {
                routeData.Values["action"] = "Maintenance";
                webSiteAlert.Content = "<h2 style='color:red;'>ShibpurConnect is down!!!</h2>Detaill Error message: <br>" + exception.Message;
                // send email notifications to admin as this is critical                
                alertController.SendEmailNotificationForOutage(webSiteAlert);
            }

            // setting error content
            webSiteAlert.Content = exception.Message;

            // save this error in the local log
            alertController.SaveNewAlert(webSiteAlert);

            // Avoid IIS7 getting in the middle
            //Response.TrySkipIisCustomErrors = true;
            //IController errorsController = new ErrorsController();
            //HttpContextWrapper wrapper = new HttpContextWrapper(Context);
            //var rc = new RequestContext(wrapper, routeData);
            //errorsController.Execute(rc);
        }
    }
}
