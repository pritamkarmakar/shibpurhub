using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.SqlServer;
using Microsoft.Owin;
using MongoDB.Driver;
using Owin;

[assembly: OwinStartupAttribute(typeof(ShibpurConnectWebApp.Startup))]
namespace ShibpurConnectWebApp
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // hangfire configuration
            GlobalConfiguration.Configuration.UseMemoryStorage();
            app.UseHangfireDashboard();
            app.UseHangfireServer();
            ConfigureAuth(app);
        }
    }
}
