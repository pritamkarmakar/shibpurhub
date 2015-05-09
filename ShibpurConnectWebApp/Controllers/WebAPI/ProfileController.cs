using Microsoft.AspNet.Identity;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models;
using ShibpurConnectWebApp.Models.WebAPI;
using ShibpurConnectWebApp.Providers;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;

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
        /// Get the entire user profile (education, employment etc) for a particular user. Usage GET: api/profile/getprofile?useremail=<email>
        /// </summary>
        /// <param name="userEmail">user email</param>
        /// <returns></returns>
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
            IHttpActionResult actionResult2 = await educationalHistoriesController.GetEducationalHistories(userInfo.Email);
            var education = actionResult2 as OkNegotiatedContentResult<List<EducationalHistories>>;

            // get the user employment details
            EmploymentHistoriesController employmentHistoriesController = new EmploymentHistoriesController();
            IHttpActionResult actionResult3 = await employmentHistoriesController.GetEmploymentHistories(userInfo.Email);
            var employment = actionResult3 as OkNegotiatedContentResult<List<EmploymentHistories>>;

            // now form the UserProfile object
            UserProfile userProfile = new UserProfile();
            if (education != null) userProfile.EducationalHistories = education.Content;
            if (employment != null) userProfile.EmploymentHistories = employment.Content;
            userProfile.UserInfo = userInfo;

            return Ok(userProfile);
        }

        /// <summary>
        /// API to get user info using user id
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<IHttpActionResult> GetProfileByUserId(string userId)
        {
            if(string.IsNullOrEmpty(userId))
            {
                ModelState.AddModelError("","userId can't be null or empty string");
                return BadRequest(ModelState);
            }

            // validate userEmail is valid and get the userid
            Task<CustomUserInfo> actionResult = helper.FindUserById(userId);
            var userInfo = await actionResult;

            if (userInfo == null)
                return NotFound();

            // get the user education details
            EducationalHistoriesController educationalHistoriesController = new EducationalHistoriesController();
            IHttpActionResult actionResult2 = await educationalHistoriesController.GetEducationalHistories(userInfo.Email);
            var education = actionResult2 as OkNegotiatedContentResult<List<EducationalHistories>>;

            // get the user employment details
            EmploymentHistoriesController employmentHistoriesController = new EmploymentHistoriesController();
            IHttpActionResult actionResult3 = await employmentHistoriesController.GetEmploymentHistories(userInfo.Email);
            var employment = actionResult3 as OkNegotiatedContentResult<List<EmploymentHistories>>;

            // now form the UserProfile object
            UserProfile userProfile = new UserProfile();
            if (education != null) userProfile.EducationalHistories = education.Content;
            if (employment != null) userProfile.EmploymentHistories = employment.Content;
            userProfile.UserInfo = userInfo;

            return Ok(userProfile);
        }

        /// <summary>
        /// Get all users by reputation
        /// </summary>
        /// <returns></returns>
        public async Task<IHttpActionResult> GetUsersByReputation()
        {
            return null;
        }

        /// <summary>
        /// Get details about about an user from only users collection
        /// </summary>
        /// <param name="userEmail">user email</param>
        /// <returns></returns>
        public async Task<IHttpActionResult> GetUserInfo(string userId)
        {
            // validate userEmail is valid and get the userid
            Helper.Helper helper = new Helper.Helper();
            Task<CustomUserInfo> actionResult = helper.FindUserById(userId);
            var userInfo = await actionResult;

            if (userInfo == null)
                return NotFound();

            return Ok(userInfo);
        }

        public async void UpdateProfileImage(ImageInfo imageInfo)
        {
            if (string.IsNullOrEmpty(imageInfo.UserId) || string.IsNullOrEmpty(imageInfo.ImageBase64))
            {
                return;
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

                var helper = new Helper.Helper();

                Task<CustomUserInfo> actionResult = helper.FindUserById(imageInfo.UserId);
                var userInfo = await actionResult;
                if (!string.IsNullOrEmpty(userInfo.ProfileImageURL) && userInfo.ProfileImageURL.IndexOf('#') > 0)
                {

                    deleteHash = userInfo.ProfileImageURL.Substring(userInfo.ProfileImageURL.IndexOf('#') + 1);
                    using (var httpClient = new HttpClient())
                    {
                        await httpClient.DeleteAsync("https://api.imgur.com/3/image/" + deleteHash);
                    }
                }

                helper.UpdateProfileImageURL(imageInfo.UserId, imageName + "#" + deleteHash);
            }
        }

        // GET: api/Profile/5
        public string Get(int id)
        {
            return "value";
        }

        /// <summary>
        /// API to update user personal profile info
        /// </summary>
        /// <param name="userInfo">CustomUserInfo object</param>
        [HttpPost]
        [Authorize]
        public async Task<IHttpActionResult> UpdateProfile(string firstName, string lastName, string location, string aboutMe)
        {       
           using (AuthRepository _repo = new AuthRepository())
            {
               // get user identity
                ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
                var claim = principal.FindFirst("sub");

                ApplicationUser user = await _repo.FindUserByEmail(claim.Value);

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

        // PUT: api/Profile/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/Profile/5
        public void Delete(int id)
        {
        }
    }
}
