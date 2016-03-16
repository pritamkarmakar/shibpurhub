using Elmah.Contrib.WebApi;
using MongoDB.Driver;
using System.Configuration;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using WebApi.OutputCache.V2;
using WebAPI.OutputCache.MongoDb;

namespace ShibpurConnectWebApp.App_Start
{
    public class WebApiConfig
    {
        private static string databaseName = ConfigurationManager.AppSettings["databasename"];
        public static void Register(HttpConfiguration config)
        {
            // enable elmah
            config.Services.Add(typeof(IExceptionLogger), new ElmahExceptionLogger());

            // Enable Cors
            config.EnableCors();

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new {id = RouteParameter.Optional}
                );

            // catch all route mapped to ErrorController so 404 errors
            // can be logged in elmah
            //config.Routes.MapHttpRoute(
            //    name: "NotFound",
            //    routeTemplate: "{*path}",
            //    defaults: new { controller = "Error", action = "NotFound" }
            //);

            // To enable bearer token authentication for the web api
            config.SuppressDefaultHostAuthentication();
            config.Filters.Add(new HostAuthenticationFilter("Bearer"));  

            // WebAPI when dealing with JSON & JavaScript!
            // Setup json serialization to serialize classes to camel (std. Json format)
            var formatter = GlobalConfiguration.Configuration.Formatters.JsonFormatter;
            formatter.SerializerSettings.ContractResolver =
                new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();

            try
            {
                // persistent cache with mongodb for cacheoutput
                var client = new MongoClient(ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString);
                var db = client.GetServer().GetDatabase(databaseName);
                MongoCollection mongocollection = db.GetCollection("cache");
                GlobalConfiguration.Configuration.CacheOutputConfiguration().RegisterCacheOutputProvider(() => new MongoDbApiOutputCache(db));
            }
            catch(System.Exception ex)
            {

            }
        }
    }
}