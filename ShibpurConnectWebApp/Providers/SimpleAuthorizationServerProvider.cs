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
using System.Collections.Generic;
using Nest;

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

        internal async Task<IdentityResult> RegisterUser(RegisterViewModel userModel)
        {
            ApplicationUser user = new ApplicationUser
            {
                UserName = userModel.Email.ToLower(),
                Email = userModel.Email.ToLower(),
                FirstName = userModel.FirstName,
                LastName = userModel.LastName,
                Location = userModel.Location,
                RegisteredOn = DateTime.UtcNow,
                // set default profile image
                ProfileImageURL = "/Content/images/profile-image.jpg"
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
        internal async Task<IdentityResult> ChangePassword(string userId, ChangePasswordViewModel passwordViewModel)
        {
            var result = await _userManager.ChangePasswordAsync(userId, passwordViewModel.OldPassword, passwordViewModel.NewPassword);

            return result;
        }

        internal async Task<ApplicationUser> FindUser(string userName, string password)
        {
            ApplicationUser user = await _userManager.FindAsync(userName, password);

            return user;
        }

        internal async Task<ApplicationUser> FindUserByEmail(string userName)
        {
            ApplicationUser user = await _userManager.FindByEmailAsync(userName);

            return user;
        }

        internal async Task<ApplicationUser> FindUserById(string userId)
        {
            ApplicationUser user = await _userManager.FindByIdAsync(userId);

            return user;
        }

        /// <summary>
        /// Method to update a user profile data
        /// </summary>
        /// <param name="applicationUser">ApplicationUser object</param>
        /// <returns></returns>
        internal async Task<IdentityResult> UpdateUser(ApplicationUser applicationUser)
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
                    Location = applicationUser.Location,
                    LastSeenOn = applicationUser.LastSeenOn
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
        internal ApplicationUser UpdateReputationCount(string userId, int deltaReputation, bool addReputaion = true)
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


        internal ApplicationUser UpdateFollowQuestion(string userId, string questionId, bool follow = true)
        {
            ApplicationUser user = _userManager.FindById(userId);
            if (user != null)
            {
                var followedQuestions = user.FollowedQuestions;
                if(followedQuestions == null)
                {
                    followedQuestions = new List<string>();
                }
                
                if(follow && !followedQuestions.Contains(questionId))
                {
                    followedQuestions.Add(questionId);
                }
                else
                {
                    followedQuestions.Remove(questionId);
                }

                user.FollowedQuestions = followedQuestions;
                var updatedUser = _userManager.Update(user);

                var client = _elasticSearchHelper.ElasticClient();
                dynamic updateUser = new System.Dynamic.ExpandoObject();


                var response = client.Update<CustomUserInfo, object>(u => u
                    .Index("my_index")
                    .Id(user.Id)
                    .Type("customuserinfo")
                    .Doc(new { FollowedQuestions = followedQuestions }));
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
        internal ApplicationUser UpdateProfileImageURL(string userId, string url)
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
    
        /// <summary>
        /// Delete the user account
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        internal async Task<ApplicationUser> DeleteUserAccount(string userId)
        {
            ApplicationUser user = _userManager.FindById(userId);
            if (user != null)
            {
                // delete from the local database
                var updatedUser = _userManager.Delete(user);

                // delete from elastic search database as well
                var client = _elasticSearchHelper.ElasticClient();
                dynamic updateUser = new System.Dynamic.ExpandoObject();

                // Be explicit with type and index
                var response = client.Delete("my_index", "customuserinfo", user.Id);
            }

            return user;
        }

        internal ApplicationUser UpdateCareerInfo(string userId, string designation, string education)
        {
            ApplicationUser user = _userManager.FindById(userId);
            if (user != null)
            {
                user.Designation = designation;
                user.EducationInfo = education;
                var updatedUser = _userManager.Update(user);

                // update same profile image in elastic search
                var client = _elasticSearchHelper.ElasticClient();
                dynamic updateUser = new System.Dynamic.ExpandoObject();

                client.Update<CustomUserInfo, object>(u => u
                            .Index("my_index")
                            .Id(userId)
                            .Type("customuserinfo")
                            .Doc(new { Designation = designation, EducationInfo = education }));
            }

            return user;
        }

        /// <summary>
        /// Method to add a new tag in users collection for a particular user
        /// </summary>
        /// <param name="userId">userid for whom we will do this update</param>
        /// <param name="tagname">new tag to be added</param>
        /// <returns></returns>
        internal async Task<IdentityResult> FollowNewTag(string userId, string tagname)
        {
            ApplicationUser user = _userManager.FindById(userId);
            if (user != null)
            {
                if (user.Tags == null)
                {
                    List<string> tags = new List<string>();
                    tags.Add(tagname);

                    user.Tags = tags;
                }
                else
                {
                    user.Tags.Add(tagname);
                }
                // save this new tag in database
                var updatedUser = _userManager.Update(user);

                // update same tags in elastic search
                var client = _elasticSearchHelper.ElasticClient();
                // get the tags from previous user object
                List<string> tagList = user.Tags;

                client.Update<CustomUserInfo, object>(u => u
                    .Index("my_index")
                    .Id(userId)
                    .Type("customuserinfo")
                    .Doc(new { Tags = tagList }));

                return updatedUser;
            }
            return null;
        }
              
        /// <summary>
        /// Method to unfollow a tag
        /// </summary>
        /// <param name="userId">userid for whom to remove the tag</param>
        /// <param name="tagName">tag to unfollow</param>
        /// <returns></returns>
        internal async Task<IdentityResult> UnfollowTag(string userId, string tagName)
        {
             ApplicationUser user = _userManager.FindById(userId);
            if (user != null)
            {
                // remove the tag from user object
                user.Tags.Remove(tagName);
                // save this new user in database
                var updatedUser = _userManager.Update(user);

                // update same in elastic search
                var client = _elasticSearchHelper.ElasticClient();
                // get the tags from previous user object
                List<string> tagList = user.Tags;

                client.Update<CustomUserInfo, object>(u => u
                    .Index("my_index")
                    .Id(userId)
                    .Type("customuserinfo")
                    .Doc(new { Tags = tagList }));

                return updatedUser;
            }

            return null;
        }

        /// <summary>
        /// Method to follow a user
        /// </summary>
        /// <param name="userId">user who wants to follow another user</param>
        /// <param name="userIdToFollow">to whom userid wants to follow</param>
        /// <returns></returns>
        internal async Task<IdentityResult> FollowUser(string userId, string userIdToFollow)
        {
            // one user can't follow himself/herself
            if(userId == userIdToFollow)
            {
                return null;
            }

            // user is the one who will follow another user
            ApplicationUser user = _userManager.FindById(userId);
            // user2 is the one who will be followed by user1
            ApplicationUser user2 = _userManager.FindById(userIdToFollow);
            if (user != null && user2 != null)
            {                
                // first complete processing for user to update its following list
                if (user.Following == null)
                {
                    List<string> following = new List<string>();
                    following.Add(userIdToFollow);

                    user.Following = following;
                }
                else
                {
                    user.Following.Add(userIdToFollow);
                }
                // save this new following in database
                var updatedUser = _userManager.Update(user);

                // update same following in elastic search
                var client = _elasticSearchHelper.ElasticClient();
                // get the tags from previous user object                

                client.Update<CustomUserInfo, object>(u => u
                    .Index("my_index")
                    .Id(userId)
                    .Type("customuserinfo")
                    .Doc(new { Following = user.Following }));

                // now process profile for user2 to update its follower list, if the list doesn't contains the follower userid
                if (user2.Followers == null)
                {
                    List<string> followers = new List<string>();
                    followers.Add(userId);

                    user2.Followers = followers;
                }
                else
                {
                    if (!user2.Followers.Contains(userId))
                        user2.Followers.Add(userId);
                }
                // save this new following in database
                var updatedUser2 = _userManager.Update(user2);

                // update same following in elastic search                
                // get the followers list from previous user object
                
                client.Update<CustomUserInfo, object>(u => u
                    .Index("my_index")
                    .Id(userIdToFollow)
                    .Type("customuserinfo")
                    .Doc(new { Followers = user2.Followers }));

                return updatedUser;
            }
            return null;
        }

        /// <summary>
        /// Method to unfollow user
        /// </summary>
        /// <param name="userId">user who wants to unfollow another user</param>
        /// <param name="userIdToUnFollow">user to unfollow</param>
        /// <returns></returns>
        internal async Task<IdentityResult> UnFollowUser(string userId, string userIdToUnFollow)
        {
            // one user can't follow himself/herself
            if (userId == userIdToUnFollow)
            {
                return null;
            }

            // user is the one who will unfollow another user
            ApplicationUser user = _userManager.FindById(userId);
            // user2 is the one who will be unfollowed by user1
            ApplicationUser user2 = _userManager.FindById(userIdToUnFollow);
            if (user != null && user2 != null)
            {
                // first complete processing for user to update its following list
                user.Following.Remove(userIdToUnFollow);
                
                // save this new following in database
                var updatedUser = _userManager.Update(user);

                // update same following in elastic search
                var client = _elasticSearchHelper.ElasticClient();
                // get the tags from previous user object                

                client.Update<CustomUserInfo, object>(u => u
                    .Index("my_index")
                    .Id(userId)
                    .Type("customuserinfo")
                    .Doc(new { Following = user.Following }));

                // now process profile for user2 to update its follower list               
                user2.Followers.Remove(userId);
                
                // save this new following in database
                var updatedUser2 = _userManager.Update(user2);

                // update same following in elastic search                
                // get the followers list from previous user object

                client.Update<CustomUserInfo, object>(u => u
                    .Index("my_index")
                    .Id(userIdToUnFollow)
                    .Type("customuserinfo")
                    .Doc(new { Followers = user2.Followers }));

                return updatedUser;
            }
            return null;
        }

        public void Dispose()
        {
            _ctx.Dispose();
            _userManager.Dispose();

        }
    }
}