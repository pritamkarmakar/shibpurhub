using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models;
using ShibpurConnectWebApp.Models.WebAPI;
using ShibpurConnectWebApp.Providers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using WebApi.OutputCache.Core.Cache;
using WebApi.OutputCache.V2;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    public class ProfileController : ApiController
    {
        private readonly MongoHelper<EducationalHistories> _mongoHelper;
        private Helper.Helper helper = new Helper.Helper();

        private IHttpActionResult GetErrorResult(IdentityResult result)
        {
            if (result == null)
            {
                return InternalServerError();
            }

            if (!result.Succeeded)
            {
                if (result.Errors != null)
                {
                    foreach (string error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }

                if (ModelState.IsValid)
                {
                    // No ModelState errors are available to send, so just return an empty BadRequest.
                    return BadRequest();
                }

                return BadRequest(ModelState);
            }

            return null;
        }

        /// <summary>
        /// Get the entire user profile (education, employment etc) for a particular user using user email. Usage GET: api/profile/getprofile?useremail=<email>
        /// </summary>
        /// <param name="userEmail">user email</param>
        /// <returns></returns>
        [CacheOutput(ServerTimeSpan = 864000, ExcludeQueryStringFromCacheKey = true, NoCache = true)]
        public async Task<IHttpActionResult> GetProfile(string userEmail)
        {
            // validate userEmail is valid and get the userid
            Helper.Helper helper = new Helper.Helper();
            Task<CustomUserInfo> actionResult = helper.FindUserByEmail(userEmail);
            var userInfo = await actionResult;

            if (userInfo == null)
                return NotFound();

            // get the user education details
            EducationalHistoriesController educationalHistoriesController = new EducationalHistoriesController();
            IHttpActionResult actionResult2 = await educationalHistoriesController.GetEducationalHistories(userInfo.Id);
            var education = actionResult2 as OkNegotiatedContentResult<List<EducationalHistories>>;

            // get the user employment details
            EmploymentHistoriesController employmentHistoriesController = new EmploymentHistoriesController();
            IHttpActionResult actionResult3 = await employmentHistoriesController.GetEmploymentHistories(userInfo.Id);
            var employment = actionResult3 as OkNegotiatedContentResult<List<EmploymentHistories>>;

            // now form the UserProfile object
            UserProfile userProfile = new UserProfile();
            if (education != null) userProfile.EducationalHistories = education.Content;
            if (employment != null) userProfile.EmploymentHistories = employment.Content;
            userProfile.UserInfo = userInfo;

            return Ok(userProfile);
        }

        /// <summary>
        /// API to get complete user profile (including educational and employment background) using user id
        /// We are using this API for the user profile page '/account/profile'
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [CacheOutput(ServerTimeSpan = 864000, ExcludeQueryStringFromCacheKey = true, NoCache = true)]
        public async Task<IHttpActionResult> GetProfileByUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                ModelState.AddModelError("", "userId can't be null or empty string");
                return BadRequest(ModelState);
            }

            // validate userId is valid and get the userid
            Task<CustomUserInfo> actionResult = helper.FindUserById(userId);
            var userInfo = await actionResult;

            if (userInfo == null)
                return NotFound();

            // see if we have the user information in-memory
            UserProfile userProfile = (UserProfile)CacheManager.GetCachedData("completeuserprofile-" + userId);
            if (userProfile == null)
            {
                userProfile = new UserProfile();

                // get the user education details
                EducationalHistoriesController educationalHistoriesController = new EducationalHistoriesController();
                IHttpActionResult actionResult2 = await educationalHistoriesController.GetEducationalHistories(userInfo.Id);
                var education = actionResult2 as OkNegotiatedContentResult<List<EducationalHistories>>;

                // get the user employment details
                EmploymentHistoriesController employmentHistoriesController = new EmploymentHistoriesController();
                IHttpActionResult actionResult3 = await employmentHistoriesController.GetEmploymentHistories(userInfo.Id);
                var employment = actionResult3 as OkNegotiatedContentResult<List<EmploymentHistories>>;

                // now form the UserProfile object            
                if (education != null) userProfile.EducationalHistories = education.Content;
                if (employment != null) userProfile.EmploymentHistories = employment.Content;
                userProfile.UserInfo = userInfo;

                // save this userprofile in-memory
                CacheManager.SetCacheData("completeuserprofile-" + userId, userProfile);
            }
            return Ok(userProfile);            
        }

        public async void UpdateLastSeenTime()
        {
            try
            {
                ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
                var email = principal.Identity.Name;
                if (string.IsNullOrEmpty(email))
                {
                    return;
                }

                using (AuthRepository _repo = new AuthRepository())
                {
                    ApplicationUser user = await _repo.FindUserByEmail(email);
                    user.LastSeenOn = DateTime.UtcNow;
                    await _repo.UpdateUser(user);

                    var cache = Configuration.CacheOutputConfiguration().GetCacheOutputProvider(Request);
                    cache.Remove("profile-getprofilebyuserid-userId=" + user.Id);
                }
            }
            catch (Exception e)
            {
            }
        }

        /// <summary>
        /// Get details about an user from only users collection
        /// </summary>
        /// <param name="userEmail">user email</param>
        /// <returns></returns>
        [CacheOutput(ServerTimeSpan = 864000, ExcludeQueryStringFromCacheKey = true, NoCache = true)]
        public async Task<IHttpActionResult> GetUserInfo(string userId)
        {
            // check if we have the userinfo in in-memory cache
            var userInfo = CacheManager.GetCachedData(userId);

            if (userInfo == null)
            {
                Helper.Helper helper = new Helper.Helper();
                Task<CustomUserInfo> actionResult = helper.FindUserById(userId);
                userInfo = await actionResult;

                if (userInfo == null)
                    return NotFound();
                // save the userInfo in in-memory cache
                else
                    CacheManager.SetCacheData(userId, userInfo);
            }

            return Ok(userInfo);
        }

        /// <summary>
        /// API to update user profile image
        /// </summary>
        /// <param name="imageInfo"></param>
        [HttpPost]
        [Authorize]
        [InvalidateCacheOutput("SearchUsers", typeof(SearchController))]
        public async Task<IHttpActionResult> UpdateProfileImage(ImageInfo imageInfo)
        {
            if (string.IsNullOrEmpty(imageInfo.ImageBase64))
            {
                ModelState.AddModelError("", "image can't be null or empty string");
                return BadRequest(ModelState);
            }

            // get user identity
            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
            var email = principal.Identity.Name;

            Helper.Helper helper = new Helper.Helper();
            var userResult = helper.FindUserByEmail(email);
            var userInfo = await userResult;
            if (userInfo == null)
            {
                return BadRequest("No UserId is found");
            }


            using (var webClient = new WebClient())
            {
                webClient.Headers.Add("Authorization", "Client-ID b079e1d3167ca54");
                //webClient.ResponseHeaders.Add("Content-Type", "application/json");
                var values = new NameValueCollection
                {
                    //{ "key", "Client-ID b079e1d3167ca54" },
                    { "image", imageInfo.ImageBase64 }
                    //{ "type", "base64" },
                };
                byte[] response = webClient.UploadValues("https://api.imgur.com/3/image", "POST", values);
                dynamic result = Encoding.ASCII.GetString(response);

                Regex reg = new Regex("link\":\"(.*?)\"");
                Match match = reg.Match(result);
                string url = match.ToString().Replace("link\":\"", "").Replace("\"", "").Replace("\\/", "/");
                var imageName = url.Substring(url.LastIndexOf('/') + 1, url.Length - (url.LastIndexOf('/') + 1));

                reg = new Regex("deletehash\":\"(.*?)\"");
                match = reg.Match(result);
                string deleteHash = match.ToString().Replace("deletehash\":\"", "").Replace("\"", "").Replace("\\/", "/");

                if (!string.IsNullOrEmpty(userInfo.ProfileImageURL) && userInfo.ProfileImageURL.IndexOf('#') > 0)
                {

                    deleteHash = userInfo.ProfileImageURL.Substring(userInfo.ProfileImageURL.IndexOf('#') + 1);
                    using (var httpClient = new HttpClient())
                    {
                        await httpClient.DeleteAsync("https://api.imgur.com/3/image/" + deleteHash);
                    }
                }

                helper.UpdateProfileImageURL(userInfo.Id, imageName + "#" + deleteHash);

                // invalidate the cache for the action those will get impacted due to this new answer post
                var cache = Configuration.CacheOutputConfiguration().GetCacheOutputProvider(Request);
                // invalidate the getprofilebyuserid api for the user who is updating profile image
                cache.RemoveStartsWith("profile-getprofilebyuserid-userId=" + userInfo.Id);
            }

            //Call WebApi to log activity
            var userActivityController = new UserActivityController();
            var userActivityLog = new UserActivityLog
            {
                Activity = 9,
                UserId = userInfo.Id,
                ActedOnObjectId = string.Empty,
                ActedOnUserId = string.Empty
            };
            userActivityController.PostAnActivity(userActivityLog);

            return Ok();
        }

        /// <summary>
        /// API to update user personal profile info
        /// </summary>
        /// <param name="userInfo">CustomUserInfo object</param>
        [HttpPost]
        [Authorize]
        [InvalidateCacheOutput("SearchUsers", typeof(SearchController))]
        public async Task<IHttpActionResult> UpdateProfile(string firstName, string lastName, string location, string aboutMe)
        {
            using (AuthRepository _repo = new AuthRepository())
            {
                // get user identity
                ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
                var email = principal.Identity.Name;
                ApplicationUser user = await _repo.FindUserByEmail(email);

                if (!string.IsNullOrEmpty(firstName))
                    user.FirstName = firstName;
                if (!string.IsNullOrEmpty(lastName))
                    user.LastName = lastName;
                if (!string.IsNullOrEmpty(location))
                    user.Location = location;
                if (!string.IsNullOrEmpty(aboutMe))
                    user.AboutMe = aboutMe;
                IdentityResult result = await _repo.UpdateUser(user);
                IHttpActionResult errorResult = GetErrorResult(result);
                if (errorResult != null)
                {
                    return errorResult;
                }

                // invalidate the cache for the action those will get impacted due to this new answer post
                var cache = Configuration.CacheOutputConfiguration().GetCacheOutputProvider(Request);

                // invalidate the getprofile api call for the user who updating profile
                cache.RemoveStartsWith("profile-getprofile-userEmail=" + user.Email);
                // invalidate the getprofilebyuserid api call for the user who updating profile
                cache.RemoveStartsWith("profile-getprofilebyuserid-userId=" + user.Id);
                // invalidate the getuserinfo api call for the user who updating profile
                cache.RemoveStartsWith("profile-getuserinfo-userId=" + user.Id);


                return Ok(new CustomUserInfo
                {
                    Email = user.Email,
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Location = user.Location,
                    ReputationCount = user.ReputationCount,
                    AboutMe = user.AboutMe,
                    ProfileImageURL = user.ProfileImageURL
                });
            }
        }

        /// <summary>
        /// API to follow a user
        /// </summary>
        /// <param name="userIdToFollow">userid that we want to follow</param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<IHttpActionResult> FollowUser(string userIdToFollow)
        {
            if (string.IsNullOrEmpty(userIdToFollow))
                return BadRequest("supplied userid is null or empty");

            // get user identity from the supplied bearer token
            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
            var email = principal.Identity.Name;

            // check if userIdToFollow is a valid userid or not
            Helper.Helper helper = new Helper.Helper();
            if (helper.FindUserById(userIdToFollow) == null)
                return BadRequest("supplied userIdToFollow is not valid userid");

            var userResult = helper.FindUserByEmail(email);
            var userInfo = await userResult;
            if (userInfo == null)
                return BadRequest("userid not found with the suuplied bearer token");

            // one user can't follow himself
            if (userIdToFollow == userInfo.Id)
                return BadRequest("you can't follow yourself");

            // process this only if userIdToFollow is not present in the current user profile collection
            if (userInfo.Following != null && userInfo.Following.Contains(userIdToFollow))
            {
                return Ok("you are already following this user");
            }

            // if we are here that means this user is not following this user so we have to add it
            AuthRepository _repo = new AuthRepository();
            IdentityResult result = await _repo.FollowUser(userInfo.Id, userIdToFollow);
            IHttpActionResult errorResult = GetErrorResult(result);
            if (errorResult != null)
            {
                return errorResult;
            }

            // invalidate the cache for the action those will get impacted due to this new answer post
            var cache = Configuration.CacheOutputConfiguration().GetCacheOutputProvider(Request);
            // invalidate the getuserfollowers api 
            cache.RemoveStartsWith("profile-getuserfollowers-userId=" + userIdToFollow);
            // invalidate the getuserfollowing api 
            cache.RemoveStartsWith("profile-getuserfollowing-userId=" + userInfo.Id);
            // invalidate the notification cache for this user
            cache.RemoveStartsWith("notifications-getnewnotifications-userId=" + userIdToFollow);
            cache.RemoveStartsWith("notifications-getnotifications-userId=" + userIdToFollow);

            // send notification to the user who is getting followed
            Uri myuri = new Uri(System.Web.HttpContext.Current.Request.Url.AbsoluteUri);
            string pathQuery = myuri.PathAndQuery;
            string hostName = myuri.ToString().Replace(pathQuery, "");
            EmailsController emailController = new EmailsController();
            await emailController.SendEmail(new Email()
            {
                UserId = userIdToFollow,
                Body = "<a href='" + hostName + "/Account/Profile?userId=" + userInfo.Id + "'>" + userInfo.FirstName + " " + userInfo.LastName + "</a> now following you. Check all your followers <a href=" + hostName + "/Account/Profile> here</a>",
                Subject = "ShibpurHub: You have a new follower"

            });

            // save this into notification collection so that user will get the bubble notice in the header
            NotificationsController notificationsController = new NotificationsController();
            notificationsController.PostNotification(new Notifications()
            {
                UserId = userIdToFollow,
                PostedOnUtc = DateTime.UtcNow,
                NewNotification = true,
                NotificationByUser = userInfo.Id,
                NotificationType = NotificationTypes.Following,
                NotificationContent =
                    "{\"followedBy\":\"" + userInfo.Id + "\",\"displayName\":\"" + userInfo.FirstName + " " +
                    userInfo.LastName + "\",\"profileImage\":\"" + userInfo.ProfileImageURL + "\"}"
            });

            //Call WebApi to log activity
            var userActivityController = new UserActivityController();
            var userActivityLog = new UserActivityLog
            {
                Activity = 7,
                UserId = userInfo.Id,
                ActedOnObjectId = string.Empty,
                ActedOnUserId = userIdToFollow
            };
            userActivityController.PostAnActivity(userActivityLog);

            return Ok("now you are following this user");

        }

        /// <summary>
        /// API to unfollow a user
        /// </summary>
        /// <param name="userIdToFollow">user to unfollow</param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<IHttpActionResult> UnfollowUser(string userToUnfollow)
        {
            if (string.IsNullOrEmpty(userToUnfollow))
                return BadRequest("supplied userid is null or empty");

            // get user identity from the supplied bearer token
            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
            var email = principal.Identity.Name;

            // check if userToUnfollow is a valid userid or not
            Helper.Helper helper = new Helper.Helper();
            if (helper.FindUserById(userToUnfollow) == null)
                return BadRequest("supplied userIdToFollow is not valid userid");

            var userResult = helper.FindUserByEmail(email);
            var userInfo = await userResult;
            if (userInfo == null)
                return BadRequest("userid not found with the suuplied bearer token");

            //process this only if the userToUnfollow is present in the current user profile collection
            if (userInfo.Following != null && userInfo.Following.Contains(userToUnfollow))
            {
                // if we are here that means this user is not following this user so we have to add it
                AuthRepository _repo = new AuthRepository();
                IdentityResult result = await _repo.UnFollowUser(userInfo.Id, userToUnfollow);
                IHttpActionResult errorResult = GetErrorResult(result);
                if (errorResult != null)
                {
                    return errorResult;
                }

                // invalidate the cache for the action those will get impacted due to this new answer post
                var cache = Configuration.CacheOutputConfiguration().GetCacheOutputProvider(Request);
                // invalidate the getuserfollowers api 
                cache.RemoveStartsWith("profile-getuserfollowers-userId=" + userToUnfollow);
                // invalidate the getuserfollowing api 
                cache.RemoveStartsWith("profile-getuserfollowing-userId=" + userInfo.Id);

                return Ok("suceessfully unfollowed this user");
            }

            // if we are here that means the user is not following the user
            return Ok("you are not following this user");
        }

        /// <summary>
        /// API to get list of followers of a user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet]
        [CacheOutput(ServerTimeSpan = 864000, ExcludeQueryStringFromCacheKey = true, NoCache = true)]
        public async Task<IHttpActionResult> GetUserFollowers(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest("userId can't be null or empty");

            // validate given userId is valid
            Helper.Helper helper = new Helper.Helper();
            Task<CustomUserInfo> actionResult = helper.FindUserById(userId);
            var userInfo = await actionResult;

            if (userInfo == null)
                return BadRequest("supplied userId is not valid userid");

            // list to keep the final follower list
            List<CustomUserInfo> followerList = new List<CustomUserInfo>();

            // if user don't have any follower list then return null
            if (userInfo.Followers == null)
                return Ok(followerList);

            foreach (string followerUserId in userInfo.Followers)
            {
                // see if we have the user information in-memory
                CustomUserInfo customUserInfo = (CustomUserInfo)CacheManager.GetCachedData(followerUserId);

                if (customUserInfo == null)
                {
                    Task<CustomUserInfo> actionResult2 = helper.FindUserById(followerUserId);
                    customUserInfo = await actionResult2;
                }
                if (customUserInfo != null)
                {
                    // add the user info in-memory
                    CacheManager.SetCacheData(followerUserId, customUserInfo);
                    followerList.Add(customUserInfo);
                }
            }

            return Ok(followerList);
        }

        /// <summary>
        /// API to get following list of current user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet]
        [CacheOutput(ServerTimeSpan = 864000, ExcludeQueryStringFromCacheKey = true, NoCache = true)]
        public async Task<IHttpActionResult> GetUserFollowing(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest("userId can't be null or empty");

            // validate given userId is valid
            Helper.Helper helper = new Helper.Helper();
            Task<CustomUserInfo> actionResult = helper.FindUserById(userId);
            var userInfo = await actionResult;

            if (userInfo == null)
                return BadRequest("supplied userId is not valid userid");

            // list to keep the final follower list
            List<CustomUserInfo> followingList = new List<CustomUserInfo>();

            // if user following list is null then return null
            if (userInfo.Following == null)
                return Ok(followingList);

            foreach (string followerUserId in userInfo.Following)
            {
                // see if we have the user information in-memory
                CustomUserInfo customUserInfo = (CustomUserInfo)CacheManager.GetCachedData(followerUserId);

                if (customUserInfo == null)
                {
                    Task<CustomUserInfo> actionResult2 = helper.FindUserById(followerUserId);
                    customUserInfo = await actionResult2;

                }

                if (customUserInfo != null)
                {
                    // add this user info in-memory
                    CacheManager.SetCacheData(followerUserId, customUserInfo);
                    followingList.Add(customUserInfo);
                }
            }

            return Ok(followingList);
        }

        /// <summary>
        /// API to check if user following another user or not
        /// </summary>
        /// <param name="userId">userid to check if current user is following this user to or</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet]
        public async Task<IHttpActionResult> CheckUserFollow(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest("supplied userid is null or empty");

            // get user identity from the supplied bearer token
            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
            var email = principal.Identity.Name;

            // check if userIdToFollow is a valid userid or not
            Helper.Helper helper = new Helper.Helper();
            if (helper.FindUserById(userId) == null)
                return BadRequest("supplied userIdToFollow is not valid userid");

            var userResult = helper.FindUserByEmail(email);
            var userInfo = await userResult;
            if (userInfo == null)
                return BadRequest("userid not found with the suuplied bearer token");

            //process this only if the userIdToFollow is not present in the current user profile collection
            if (userInfo.Following != null && userInfo.Following.Contains(userId))
            {
                return Ok(true);
            }

            return Ok(false);
        }
    }
}
