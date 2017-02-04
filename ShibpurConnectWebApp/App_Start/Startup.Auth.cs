using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Facebook;
using Microsoft.Owin.Security.Google;
using Microsoft.Owin.Security.OAuth;
using Owin.Security.Providers.LinkedIn;
using Owin;
using ShibpurConnectWebApp.Models;
using ShibpurConnectWebApp.Providers;
using System;
using System.Configuration;

namespace ShibpurConnectWebApp
{
    public partial class Startup
    {
        public static string PublicClientId { get; private set; }
        public static OAuthAuthorizationServerOptions OAuthOptions { get; private set; }
        private static string fbAppID = ConfigurationManager.AppSettings["fbAppID"];
        private static string fbAppSecret = ConfigurationManager.AppSettings["fbAppSecret"];

        private static string googleAppID = ConfigurationManager.AppSettings["googleAppID"];
        private static string googleAppSecret = ConfigurationManager.AppSettings["googleAppSecret"];

        private static string linkedinAppID = ConfigurationManager.AppSettings["linkedinAppID"];
        private static string linkedinAppSecret = ConfigurationManager.AppSettings["linkedinAppSecret"];

        // Enable the application to use OAuthAuthorization. You can then secure your Web APIs
        static Startup()
        {
            PublicClientId = "web";

            OAuthOptions = new OAuthAuthorizationServerOptions
            {
                TokenEndpointPath = new PathString("/Token"),
                AuthorizeEndpointPath = new PathString("/Account/Authorize"),
                Provider = new ApplicationOAuthProvider(PublicClientId),
                AccessTokenExpireTimeSpan = TimeSpan.FromDays(1),
                AllowInsecureHttp = true
            };
        }

        // For more information on configuring authentication, please visit http://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app) {
            // Configure the db context, user manager and signin manager to use a single instance per request
            app.CreatePerOwinContext(ApplicationIdentityContext.Create);
            app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);
            app.CreatePerOwinContext<ApplicationSignInManager>(ApplicationSignInManager.Create);

            // Enable the application to use a cookie to store information for the signed in user
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login"),
                Provider = new CookieAuthenticationProvider
                {
                    // Enables the application to validate the security stamp when the user logs in.
                    // This is a security feature which is used when you change a password or add an external login to your account.  
                    OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<ApplicationUserManager, ApplicationUser>(
                        validateInterval: TimeSpan.FromMinutes(20),
                        regenerateIdentity: (manager, user) => user.GenerateUserIdentityAsync(manager))
                }
            });
            // Use a cookie to temporarily store information about a user logging in with a third party login provider
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            // Enables the application to temporarily store user information when they are verifying the second factor in the two-factor authentication process.
            app.UseTwoFactorSignInCookie(DefaultAuthenticationTypes.TwoFactorCookie, TimeSpan.FromMinutes(5));

            // Enables the application to remember the second login verification factor such as phone or email.
            // Once you check this option, your second step of verification during the login process will be remembered on the device where you logged in from.
            // This is similar to the RememberMe option when you log in.
            app.UseTwoFactorRememberBrowserCookie(DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie);

            // Enable the application to use bearer tokens to authenticate users
            app.UseOAuthBearerTokens(OAuthOptions);

            // added email in the scope
            FacebookAuthenticationOptions fbao = new FacebookAuthenticationOptions();
            fbao.AppId = fbAppID;
            fbao.AppSecret = fbAppSecret;
            fbao.Scope.Add("email");
            fbao.Scope.Add("public_profile");
            //fbao.Scope.Add("last_name");
            fbao.SignInAsAuthenticationType = Microsoft.Owin.Security.AppBuilderSecurityExtensions.GetDefaultSignInAsAuthenticationType(app);
            
            app.UseFacebookAuthentication(fbao);

            app.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions()
            {
                ClientId =googleAppID,
                ClientSecret = googleAppSecret
            });

            // LinkedIn login
            app.UseLinkedInAuthentication(linkedinAppID, linkedinAppSecret);

            // hangfire configuration
            GlobalConfiguration.Configuration.UseMemoryStorage();

            var options = new DashboardOptions
            {
                AuthorizationFilters = new[]
                {
                    new HangfireAuthorizationFilter()

                }
            };
            app.UseHangfireDashboard("/hangfire", options);
            app.UseHangfireServer();
        }
    }
}