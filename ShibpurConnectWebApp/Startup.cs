using Microsoft.Owin;
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
