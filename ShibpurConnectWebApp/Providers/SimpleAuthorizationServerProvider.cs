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
using System.Web;
using System.Net.Http;

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
            _elasticSearchHelper = new ElasticSearchHelper();
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
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Location = user.Location,
                    RegisteredOn = user.RegisteredOn,
                    ReputationCount = user.ReputationCount,
                    ProfileImageURL = user.ProfileImageURL,
                    AboutMe = user.AboutMe
                });
            }

            return result;
        }

        /// <summary>
        /// Method to change the password for an user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="passwordViewModel"></param>
        /// <returns></returns>
        public async Task<IdentityResult> ChangePassword(string userId, ChangePasswordViewModel passwordViewModel)
        {
            var result = await _userManager.ChangePasswordAsync(userId, passwordViewModel.OldPassword, passwordViewModel.NewPassword);

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

        /// <summary>
        /// Method to update a user profile data
        /// </summary>
        /// <param name="applicationUser">ApplicationUser object</param>
        /// <returns></returns>
        public async Task<IdentityResult> UpdateUser(ApplicationUser applicationUser)
        {
            var result = await _userManager.UpdateAsync(applicationUser);

            // update same user details in elastic search
            var client = _elasticSearchHelper.ElasticClient();
            dynamic updateUser = new System.Dynamic.ExpandoObject();

            var response = client.Update<CustomUserInfo, object>(u => u
                .Index("my_index")
                .Id(applicationUser.Id)
                .Type("customuserinfo")
                .Doc(new
                {
                    FirstName = applicationUser.FirstName,
                    LastName = applicationUser.LastName,
                    AboutMe = applicationUser.AboutMe,
                    Location = applicationUser.Location
                }));

            return result;
        }

        /// <summary>
        /// Method to update user reputation
        /// This will update both in mongo and elastic search
        /// </summary>
        /// <param name="userId">userid</param>
        /// <param name="deltaReputation">reputation change amount</param>
        /// <param name="addReputaion">add/subtract reputation</param>
        /// <returns></returns>
        public ApplicationUser UpdateReputationCount(string userId, int deltaReputation, bool addReputaion = true)
        {
            ApplicationUser user = _userManager.FindById(userId);
            if (user != null)
            {
                var userReputaion = user.ReputationCount;
                userReputaion = addReputaion ? (userReputaion + deltaReputation) : (userReputaion - deltaReputation);
                user.ReputationCount = userReputaion;
                var updatedUser = _userManager.Update(user);

                // update same reputation in elastic search
                var client = _elasticSearchHelper.ElasticClient();
                dynamic updateUser = new System.Dynamic.ExpandoObject();


                var response = client.Update<CustomUserInfo, object>(u => u
                    .Index("my_index")
                    .Id(user.Id)
                    .Type("customuserinfo")
                    .Doc(new { ReputationCount = userReputaion }));
            }

            return user;
        }

        /// <summary>
        /// Method to update user profile image
        /// This method will update both in Mongodb and ES
        /// </summary>
        /// <param name="userId">user userid</param>
        /// <param name="url">profile image url</param>
        /// <returns></returns>
        public ApplicationUser UpdateProfileImageURL(string userId, string url)
        {
            ApplicationUser user = _userManager.FindById(userId);
            if (user != null)
            {
                user.ProfileImageURL = url;
                var updatedUser = _userManager.Update(user);

                // update same profile image in elastic search
                var client = _elasticSearchHelper.ElasticClient();
                dynamic updateUser = new System.Dynamic.ExpandoObject();

                var response = client.Update<CustomUserInfo, object>(u => u
                    .Index("my_index")
                    .Id(userId)
                    .Type("customuserinfo")
                    .Doc(new { ProfileImageURL = url }));
            }

            return user;
        }

        public void Dispose()
        {
            _ctx.Dispose();
            _userManager.Dispose();

        }
    }
}