using System;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Facebook;
using Microsoft.Owin.Security.Google;
using Microsoft.Owin.Security.OAuth;
using Owin;
using ShibpurConnectWebApp.Models;
using ShibpurConnectWebApp.Providers;

namespace ShibpurConnectWebApp
{
    public partial class Startup
    {
        public static string PublicClientId { get; private set; }

        // For more information on configuring authentication, please visit http://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app) {
            // Configure the db context, user manager and role manager to use a single instance per request
            app.CreatePerOwinContext(ApplicationIdentityContext.Create);
            app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);
            app.CreatePerOwinContext<ApplicationRoleManager>(ApplicationRoleManager.Create);

            // Token Generation
            // Configure the application for OAuth based flow
            OAuthAuthorizationServerOptions OAuthServerOptions = new OAuthAuthorizationServerOptions()
            {
                AllowInsecureHttp = true,
                TokenEndpointPath = new PathString("/token"),
                AccessTokenExpireTimeSpan = TimeSpan.FromDays(1),
                Provider = new SimpleAuthorizationServerProvider()
            };

            // Token Generation
            app.UseOAuthAuthorizationServer(OAuthServerOptions);
            app.UseOAuthBearerAuthentication(new OAuthBearerAuthenticationOptions());

            // Enable the application to use a cookie to store information for the signed in user
            // and to use a cookie to temporarily store information about a user logging in with a third party login provider
            // Configure the sign in cookie
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login"),
                Provider = new CookieAuthenticationProvider
                {
                    // Enables the application to validate the security stamp when the user logs in.
                    // This is a security feature which is used when you change a password or add an external login to your account.  
                    OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<ApplicationUserManager, ApplicationUser>(
                        validateInterval: TimeSpan.FromMinutes(30),
                        regenerateIdentity: (manager, user) => user.GenerateUserIdentityAsync(manager))
                }
            });            
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            // Enables the application to temporarily store user information when they are verifying the second factor in the two-factor authentication process.
            app.UseTwoFactorSignInCookie(DefaultAuthenticationTypes.TwoFactorCookie, TimeSpan.FromMinutes(5));

            // Enables the application to remember the second login verification factor such as phone or email.
            // Once you check this option, your second step of verification during the login process will be remembered on the device where you logged in from.
            // This is similar to the RememberMe option when you log in.
            app.UseTwoFactorRememberBrowserCookie(DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie);

            // Uncomment the following lines to enable logging in with third party login providers
            //app.UseMicrosoftAccountAuthentication(
            //    clientId: "",
            //    clientSecret: "");

            //app.UseTwitterAuthentication(
            //   consumerKey: "edEikbYXMqP5Vljayvb9CMzI5",
            //   consumerSecret: "Jw30S2EqfacEOb7hP9Et7eeOPSclmmpaBbWIh19I9zbTjltIPo");

            // added email in the scope
            FacebookAuthenticationOptions fbao = new FacebookAuthenticationOptions();
            fbao.AppId = "1561377064127021";
            fbao.AppSecret = "3c8a3bc7ee6151ae4b341f502e8a13f3";
            fbao.Scope.Add("email");
            fbao.Scope.Add("public_profile");
            //fbao.Scope.Add("last_name");
            fbao.SignInAsAuthenticationType = Microsoft.Owin.Security.AppBuilderSecurityExtensions.GetDefaultSignInAsAuthenticationType(app);
            
            app.UseFacebookAuthentication(fbao);

            app.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions()
            {
                ClientId = "108349931194-2h3peq298hndtckune1h1tqi96dbh8bg.apps.googleusercontent.com",
                ClientSecret = "xKb09OnMBqPZMVnHKCOTPfcJ"
            });

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