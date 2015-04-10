using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Security.Claims;
using System.Threading.Tasks;
using AspNet.Identity.MongoDB;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security.OAuth;
using MongoDB.Driver;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models;
using ShibpurConnectWebApp.Models.WebAPI;

namespace ShibpurConnectWebApp.Providers
{
    public class SimpleAuthorizationServerProvider : OAuthAuthorizationServerProvider
    {
        public override async Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            context.Validated();
        }

        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {

            context.OwinContext.Response.Headers.Add("Access-Control-Allow-Origin", new[] { "*" });

            using (AuthRepository _repo = new AuthRepository())
            {
                IdentityUser user = await _repo.FindUser(context.UserName, context.Password);

                if (user == null)
                {
                    context.SetError("invalid_grant", "The user name or password is incorrect.");
                    return;
                }
            }

            var identity = new ClaimsIdentity(context.Options.AuthenticationType);
            identity.AddClaim(new Claim("sub", context.UserName));
            identity.AddClaim(new Claim("role", "user"));

            context.Validated(identity);

        }
    }

    public class AuthRepository : IDisposable
    {
        private ApplicationIdentityContext _ctx;
        private ElasticSearchHelper _elasticSearchHelper;

        private UserManager<ApplicationUser> _userManager;

        public AuthRepository()
        {
            var client = new MongoClient(ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString);
            var db = client.GetServer().GetDatabase("shibpurconnect");

            _ctx = new ApplicationIdentityContext(db.GetCollection<IdentityUser>("users"), db.GetCollection<IdentityUser>("roles"));
            _userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(_ctx));
        }

        public async Task<IdentityResult> RegisterUser(RegisterViewModel userModel)
        {
            ApplicationUser user = new ApplicationUser
            {
                UserName = userModel.Email.ToLower(),
                Email = userModel.Email.ToLower(),
                FirstName = userModel.FirstName,
                LastName = userModel.LastName,
                Location = userModel.Location,
                RegisteredOn = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, userModel.Password);
            
            // add the new user in the elastic search index
            if (result.Succeeded)
            {
                var client = _elasticSearchHelper.ElasticClient();
                var index = client.Index(new CustomUserInfo
                {
                    Email = user.Email,
                    UserId = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Location = user.Location
                });
            }

            return result;
        }

        public async Task<ApplicationUser> FindUser(string userName, string password)
        {
            ApplicationUser user = await _userManager.FindAsync(userName, password);

            return user;
        }

        public async Task<ApplicationUser> FindUserByEmail(string userName)
        {
            ApplicationUser user = await _userManager.FindByEmailAsync(userName);

            return user;
        }

        public async Task<ApplicationUser> FindUserById(string userId)
        {
            ApplicationUser user = await _userManager.FindByIdAsync(userId);

            return user;
        }

        public ApplicationUser UpdateReputationCount(string userId, int deltaReputation, bool addReputaion = true)
        {
            ApplicationUser user = _userManager.FindById(userId);
            var userReputaion = user.ReputationCount;
            userReputaion = addReputaion ? (userReputaion + deltaReputation) : (userReputaion - deltaReputation);
            user.ReputationCount = userReputaion;
            var updatedUser = _userManager.Update(user);
            return user;
        }

        public void Dispose()
        {
            _ctx.Dispose();
            _userManager.Dispose();

        }
    }
}