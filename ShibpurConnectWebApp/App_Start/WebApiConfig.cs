using MongoDB.Driver;
using System.Configuration;
using System.Web.Http;
using WebApi.OutputCache.V2;
using WebAPI.OutputCache.MongoDb;

namespace ShibpurConnectWebApp.App_Start
{
    public class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Enable Cors
            config.EnableCors();

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new {id = RouteParameter.Optional}
                );

            // To enable bearer token authentication for the web api
            config.SuppressDefaultHostAuthentication();
            config.Filters.Add(new HostAuthenticationFilter("Bearer"));  

            // WebAPI when dealing with JSON & JavaScript!
            // Setup json serialization to serialize classes to camel (std. Json format)
            var formatter = GlobalConfiguration.Configuration.Formatters.JsonFormatter;
            formatter.SerializerSettings.ContractResolver =
                new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();

            // persistent cache with mongodb for cacheoutput
            var client = new MongoClient(ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString);
            var db = client.GetServer().GetDatabase("shibpurconnect");
            MongoCollection mongocollection = db.GetCollection("cache");
            GlobalConfiguration.Configuration.CacheOutputConfiguration().RegisterCacheOutputProvider(() => new MongoDbApiOutputCache(db));
        }
    }
}