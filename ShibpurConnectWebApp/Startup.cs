using System.Collections.Generic;
using System.Web.Security;
using Hangfire;
using Hangfire.Dashboard;
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
            
            ConfigureAuth(app);
        }
    }
}
