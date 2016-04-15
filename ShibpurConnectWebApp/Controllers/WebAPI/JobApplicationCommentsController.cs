using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Http.Results;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using ServiceStack;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models;
using ShibpurConnectWebApp.Models.WebAPI;
using WebApi.OutputCache.V2;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{

    public class JobApplicationCommentsController : ApiController
    {
        private MongoHelper<JobApplicationComment> _mongoHelper;

        public JobApplicationCommentsController()
        {
            _mongoHelper = new MongoHelper<JobApplicationComment>();
        }

        /// <summary>
        /// Get list of comments available in a job application
        /// </summary>
        /// <param name="applicationId">applicationId to search</param>
        /// <returns></returns>
        [CacheOutput(ServerTimeSpan = 864000, ExcludeQueryStringFromCacheKey = true, NoCache = true)]
        public IList<JobApplicationComment> GetCommentsForJobApplication(string applicationId)
        {
            var result = _mongoHelper.Collection.AsQueryable().Where(a => a.ApplicationId == applicationId).OrderBy(a => a.PostedOnUtc).ToList();
            return result;
        }

        /// <summary>
        /// Add a new comment for a job application
        /// </summary>
        /// <param name="comment">JobApplicationCommentDTO object</param>
        /// <returns></returns>
        [Authorize]
        [ResponseType(typeof(Comment))]
        public async Task<IHttpActionResult> PostComment(JobApplicationCommentDTO comment)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (comment == null)
            {
                return BadRequest("Request body is null. Please send a valid Questions object");
            }
            if(String.IsNullOrEmpty(comment.CommentText))
                return BadRequest("Commment can't be empty");

            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
            var email = principal.Identity.Name;

            Helper.Helper helper = new Helper.Helper();
            var userResult = helper.FindUserByEmail(email);
            var userInfo = await userResult;
            if (userInfo == null)
            {
                return BadRequest("No UserId is found");
            }

            // save the comment to the database
            JobApplicationComment commentToPost = new JobApplicationComment()
            {
                UserId = userInfo.Id,
                PostedOnUtc = DateTime.UtcNow,
                ApplicationId = comment.ApplicationId,
                CommentId = ObjectId.GenerateNewId().ToString(),
                CommentText = comment.CommentText
            };
            var result = _mongoHelper.Collection.Save(commentToPost);
            // if mongo failed to save the data then send error
            if (!result.Ok)
                return InternalServerError();

            // send notification to the user who posted the corresponding application
            // get the hostname
            Uri myuri = new Uri(System.Web.HttpContext.Current.Request.Url.AbsoluteUri);
            string pathQuery = myuri.PathAndQuery;
            string hostName = myuri.ToString().Replace(pathQuery, "");

            // get details about the associated application
            var mongoHelper = new MongoHelper<JobApplication>();
            var applicationDetails = mongoHelper.Collection.AsQueryable().FirstOrDefault(m => m.ApplicationId == comment.ApplicationId);

            // get details about the associated job
            var mongoHelper2 = new MongoHelper<Job>();
            var jobDetails = mongoHelper2.Collection.AsQueryable().FirstOrDefault(m => m.JobId == applicationDetails.JobId);
           
            EmailsController emailsController = new EmailsController();
            // send notification for this new comment, bubble comment in the nav bar
            NotificationsController notificationsController = new NotificationsController();
            // sent notification to the user who posted the answer, only if the user who posted the comment is a different person
            if (applicationDetails.UserId != userInfo.Id)
            {
                await emailsController.SendEmail(new Email()
                {
                    UserId = applicationDetails.UserId,
                    Body =
                        "<a href='" + hostName + "/Account/Profile?userId=" + userInfo.Id +
                        "' style='text-decoration:none'>" + userInfo.FirstName + " " + userInfo.LastName + "</a>" +
                        " posted a comment to your job application <a href='" + hostName + "/career/jobdetails?jobid=" +
                        jobDetails.JobId + "' style='text-decoration:none'>" + jobDetails.JobTitle + "</a><i>" + comment.CommentText + "</i>",
                    Subject = "ShibpurHub | New comment to your job application \"" + jobDetails.JobTitle + "\""
                });

                notificationsController.PostNotification(new Notifications()
                {
                    UserId = applicationDetails.UserId,
                    PostedOnUtc = DateTime.UtcNow,
                    NewNotification = true,
                    NotificationByUser = userInfo.Id,
                    NotificationType = NotificationTypes.ReceivedCommentInJobApplication,
                    NotificationContent =
                        "{\"commentedBy\":\"" + userInfo.Id + "\",\"displayName\":\"" + userInfo.FirstName + " " +
                        userInfo.LastName + "\",\"jobId\":\"" + jobDetails.JobId +
                        "\",\"profileImage\":\"" + userInfo.ProfileImageURL + "\",\"jobTitle\":\"" +
                        jobDetails.JobTitle + "\"}"
                });
            }

            return CreatedAtRoute("DefaultApi", new { id = commentToPost.CommentId }, commentToPost);
        }

        /// <summary>
        /// Edit a new comment for a question
        /// </summary>
        /// <param name="comment">Comment object</param>
        /// <returns></returns>
        [Authorize]
        [ResponseType(typeof(JobApplicationComment))]
        public async Task<IHttpActionResult> EditComment(JobApplicationComment comment)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (comment == null)
            {
                return BadRequest("Request body is null. Please send a valid Comment object");
            }

            try
            {
                var commentInDB = _mongoHelper.Collection.AsQueryable().Where(a => a.CommentId == comment.CommentId).FirstOrDefault();
                if (commentInDB == null)
                {
                    return NotFound();
                }

                commentInDB.CommentText = comment.CommentText;
                commentInDB.LastEditedOnUtc = DateTime.UtcNow;

                _mongoHelper.Collection.Save(commentInDB);

                return CreatedAtRoute("DefaultApi", new { id = comment.CommentId }, comment);
            }
            catch (MongoDB.Driver.MongoConnectionException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}