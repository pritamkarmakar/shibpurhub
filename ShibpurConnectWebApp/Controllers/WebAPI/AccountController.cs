using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using AspNet.Identity.MongoDB;
using Microsoft.AspNet.Identity;
using ShibpurConnectWebApp.Models;
using ShibpurConnectWebApp.Models.WebAPI;
using ShibpurConnectWebApp.Providers;
using System;
using ShibpurConnectWebApp.Helper;
using System.Security.Claims;
using System.Net.Http;
using ShibpurConnectWebApp.Helper;
using WebApi.OutputCache.V2;
using Hangfire;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    [RoutePrefix("api/Account")]
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class AccountController : ApiController
    {
        private AuthRepository _repo = null;       

        public AccountController()
        {
            _repo = new AuthRepository();
        }

        // POST api/Register
        /// <summary>
        /// POST: Register a new user
        /// </summary>
        /// <param name="userModel"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [Route("Register")]
        public async Task<IHttpActionResult> Register(RegisterViewModel userModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IdentityResult result = await _repo.RegisterUser(userModel);         
            IHttpActionResult errorResult = GetErrorResult(result);

            if (errorResult != null)
            {
                return errorResult;
            }          

            return Ok();
        }

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

        // GET api/Finduser
        /// <summary>
        /// Find user, you have to provide your email and password. This API will help you to get userid, that require for making any POST call
        /// </summary>
        /// <param name="userEmail">user email</param>
        /// <param name="password">password</param>
        /// <returns></returns>
        [AllowAnonymous]
        [Route("FindUser")]
        [HttpGet]
        public async Task<CustomUserInfo> FindUser(string userEmail, string password)
        {
            using (AuthRepository _repo = new AuthRepository())
            {
                ApplicationUser user = await _repo.FindUser(userEmail.ToLower(), password);

                if (user == null)
                {
                    return null;
                }
                else
                {
                    return new CustomUserInfo
                    {
                        Email = user.Email,
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Location = user.Location,
                        ReputationCount = user.ReputationCount,
                        AboutMe = user.AboutMe,
                        ProfileImageURL = user.ProfileImageURL,
                        RegisteredOn = user.RegisteredOn
                    };
                }
            }
        }

        /// <summary>
        /// API to find the current logged-in user
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<CustomUserInfo> Me()
        {
            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
            var email = principal.Identity.Name;

            Helper.Helper helper = new Helper.Helper();
            var userResult = helper.FindUserByEmail(email);
            var userInfo = await userResult;
            if (userInfo == null)
            {
                return null;
            }

            return userInfo;
        }

        [HttpPost]
        [Authorize]
        public async Task<IHttpActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
            var email = principal.Identity.Name;

            Helper.Helper helper = new Helper.Helper();
            var actionResult = helper.FindUserByEmail(email);
            var userInfo = await actionResult;

            IdentityResult result = await _repo.ChangePassword(userInfo.Id, model);
            IHttpActionResult errorResult = GetErrorResult(result);

            if (errorResult != null)
            {
                return errorResult;
            }     
        

            return Ok("{'status': 'successfullychangedpassword'}");
        }

        /// <summary>
        /// Delete the user account
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public async Task<IHttpActionResult> DeleteAccount()
        {
            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
            var email = principal.Identity.Name;

            Helper.Helper helper = new Helper.Helper();
            var userResult = helper.FindUserByEmail(email);
            var userInfo = await userResult;
            if (userInfo == null)
            {
                return BadRequest("No UserId is found");
            }           

            // Delete all records associated with this user
            BackgroundJob.Enqueue(() => DeleteAllRecords(userInfo.Id));

            // invalidate the cache for the action those will get impacted due to this new answer post
            var cache = Configuration.CacheOutputConfiguration().GetCacheOutputProvider(Request);

            // invalidate the getemploymenthistories api call for this user       
            cache.RemoveStartsWith("profile-getprofilebyuserid-userId=" + userInfo.Id);
            cache.RemoveStartsWith("users-getleaderboard");

            return CreatedAtRoute("DefaultApi", new { id = userInfo.Id }, "User account deleted");
        }

        /// <summary>
        /// This is where Hangfire will do the background task
        /// We will remove all the educational, employment, answer, comments details associated with this user, when user will delete his/her account
        /// </summary>
        /// <param name="id"></param>
        [DisableConcurrentExecution(3600)]
        public async void DeleteAllRecords(string userId)
        {
            // delete all educational histories from database
            EducationalHistoriesController educationalHistoryController = new EducationalHistoriesController();
            educationalHistoryController.DeleteAllEducationalHistories(userId);

            // delete all employment histories from database
            EmploymentHistoriesController employmentHistoriesController = new EmploymentHistoriesController();
            employmentHistoriesController.DeleteAllEmploymentHistories(userId);

            // delete all questions posted by this user
            QuestionsController questionController = new QuestionsController();
            questionController.DeleteAllQuestionsPostedByAUser(userId);

            // delete all answers posted by this user
            AnswersController answerController = new AnswersController();
            answerController.DeleteAllAnswerPostedByAUser(userId);

            // delete all comments posted by this user
            CommentsController commentController = new CommentsController();
            commentController.DeleteAllCommentPostedByAUser(userId);

            // delete all jobs posted by this user
            CareerController careerController = new CareerController();
            careerController.DeleteAllJobsAndApplicationaPostedByAUser(userId);

            // delete all job applications by this user (not required at this time, as the API which retrieve the job applications validate whether user exist or not)

            // delete the user from the database, keeping it at the end to make sure previous methods get execute successfully
            ApplicationUser result = await _repo.DeleteUserAccount(userId);

            // delete all notifications caused by this user
            NotificationsController notificationController = new NotificationsController();
            notificationController.DeleteAllNotificationsPostedByAUser(userId);              
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _repo.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
