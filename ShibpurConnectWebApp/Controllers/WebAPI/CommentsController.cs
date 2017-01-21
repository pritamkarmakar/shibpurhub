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
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{

    public class CommentsController : ApiController
    {
        private MongoHelper<Comment> _mongoHelper;

        public CommentsController()
        {
            _mongoHelper = new MongoHelper<Comment>();
        }

        /// <summary>
        /// Get list of comments available in an answer
        /// </summary>
        /// <param name="answerId">answerid to search</param>
        /// <returns></returns>
       [CacheOutput(ServerTimeSpan = 864000, ExcludeQueryStringFromCacheKey = true, NoCache = true)]
        public IList<Comment> GetCommentsForAnswer(string answerId)
        {
            var result = _mongoHelper.Collection.AsQueryable().Where(a => a.AnswerId == answerId).OrderBy(a => a.PostedOnUtc).ToList();
            return result;
        }

        public int GetCommentCountForAnswer(string answerId)
        {
            var result = _mongoHelper.Collection.AsQueryable().Where(a => a.AnswerId == answerId);
            return result.Count();
        }

        /// <summary>
        /// Add a new comment for a question
        /// </summary>
        /// <param name="comment">Comment object</param>
        /// <returns></returns>
        [Authorize]
        [ResponseType(typeof(Comment))]
        public async Task<IHttpActionResult> PostComment(CommentDTO comment)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (comment == null)
            {
                return BadRequest("Request body is null. Please send a valid Questions object");
            }

            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
            var email = principal.Identity.Name;

            Helper.Helper helper = new Helper.Helper();
            var userResult = helper.FindUserByEmail(email);
            var userInfo = await userResult;
            if (userInfo == null)
            {
                return BadRequest("No UserId is found");
            }

            // save the question to the database
            Comment commentToPost = new Comment()
            {
                UserId = userInfo.Id,
                PostedOnUtc = DateTime.UtcNow,
                AnswerId = comment.AnswerId,
                CommentId = ObjectId.GenerateNewId().ToString(),
                CommentText = comment.CommentText
            };
            var result = _mongoHelper.Collection.Save(commentToPost);
            // if mongo failed to save the data then send error
            if (!result.Ok)
                return InternalServerError();

            // send notification to the user who posted the corresponding answer and who posted the actual question
            // get the hostname
            Uri myuri = new Uri(System.Web.HttpContext.Current.Request.Url.AbsoluteUri);
            string pathQuery = myuri.PathAndQuery;
            string hostName = myuri.ToString().Replace(pathQuery, "");

            // get details about the associated answer
            AnswersController answersController = new AnswersController();
            var actionresult = await answersController.GetAnswer(comment.AnswerId);
            var answer = actionresult as OkNegotiatedContentResult<Answer>;

            // get details about associated question
            QuestionsController questionsController = new QuestionsController();
            var actionresult2 = await questionsController.GetQuestionInfo(answer.Content.QuestionId);
            var question = actionresult2 as OkNegotiatedContentResult<Question>;

            // invalidate the cache for the action those will get impacted due to this new answer post
            var cache = Configuration.CacheOutputConfiguration().GetCacheOutputProvider(Request);

            // invalidate the getquestion api call for the question associated with this answer
            cache.RemoveStartsWith("comments-getcommentsforanswer-answerId=" + comment.AnswerId);
            cache.RemoveStartsWith("questions-getquestion-questionId=" + question.Content.QuestionId);
            cache.RemoveStartsWith("notifications-getnewnotifications-userId=" + answer.Content.UserId);
            cache.RemoveStartsWith("notifications-getnotifications-userId=" + answer.Content.UserId);

             // post user activity for this new answer
            var userActivityLog = new UserActivityLog
            {
                Activity = 4,
                UserId = userInfo.Id,
                ActedOnObjectId = commentToPost.CommentId,
                PatentObjectId = question.Content.QuestionId
            };

            UpdateUserActivityLog(userActivityLog);

            EmailsController emailsController = new EmailsController();
            // send notification for this new comment, bubble comment in the nav bar
            NotificationsController notificationsController = new NotificationsController();
            // sent notification to the user who posted the answer, only if the user who posted the comment is a different person
            if (answer.Content.UserId != userInfo.Id)
            {
                await emailsController.SendEmail(new Email()
                {
                    UserId = answer.Content.UserId,
                    Body =
                        "<a href='" + hostName + "/Account/Profile?userId=" + userInfo.Id +
                        "' style='text-decoration:none'>" + userInfo.FirstName + " " + userInfo.LastName + "</a>" +
                        " posted a comment to your answer in question <a href='" + hostName + "/feed/" +
                        question.Content.UrlSlug + "' style='text-decoration:none'>" + question.Content.Title + "</a><i>" + comment.CommentText + "</i>",
                    Subject = "SnapResponses | New comment to your answer \"" + question.Content.Title + "\""
                });

                notificationsController.PostNotification(new Notifications()
                {
                    UserId = answer.Content.UserId,
                    PostedOnUtc = DateTime.UtcNow,
                    NewNotification = true,
                    NotificationType = NotificationTypes.ReceivedComment,
                    NotificationByUser = userInfo.Id,
                    NotificationContent =
                        "{\"commentedBy\":\"" + userInfo.Id + "\",\"displayName\":\"" + userInfo.FirstName + " " +
                        userInfo.LastName + "\",\"questionId\":\"" + question.Content.QuestionId +
                        "\",\"profileImage\":\"" + userInfo.ProfileImageURL + "\",\"questionTitle\":\"" +
                        question.Content.UrlSlug + "\"}"
                });
            }

            // sent a notification to the user who posted the question, only if the commenter is a different person
            // also if the user who posted the question and the user who submitted the answer are same then we don't want to send same notification again
            if (question.Content.UserId != userInfo.Id && answer.Content.UserId != question.Content.UserId)
            {
                await emailsController.SendEmail(new Email()
                {
                    UserId = question.Content.UserId,
                    Body = "<a href='" + hostName + "/Account/Profile?userId=" + userInfo.Id + "' style='text-decoration:none'>" + userInfo.FirstName + " " + userInfo.LastName + "</a>" + " posted a comment to your question <a href='" + hostName + "/feed/" + question.Content.UrlSlug + "' style='text-decoration:none'>" + question.Content.Title + "</a><i>" + comment.CommentText + "</i>",
                    Subject = "SnapResponses | New comment to your question \"" + question.Content.Title + "\""
                });

                notificationsController.PostNotification(new Notifications()
                {
                    UserId = question.Content.UserId,
                    PostedOnUtc = DateTime.UtcNow,
                    NewNotification = true,
                    NotificationByUser = userInfo.Id,
                    NotificationType = NotificationTypes.ReceivedCommentInQuestion,
                    NotificationContent =
                        "{\"commentedBy\":\"" + userInfo.Id + "\",\"displayName\":\"" + userInfo.FirstName + " " +
                        userInfo.LastName + "\",\"questionId\":\"" + question.Content.QuestionId +
                        "\",\"profileImage\":\"" + userInfo.ProfileImageURL + "\",\"questionTitle\":\"" +
                        question.Content.UrlSlug + "\"}"
                });
            }




            return CreatedAtRoute("DefaultApi", new { id = commentToPost.CommentId }, commentToPost);
        }

        [Authorize]
        internal async void DeleteAllCommentPostedByAUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("null userId supplied");

            // find list of answers those will be affected due to this deletion, we will then remove the cache with associted questions
            var answerIds = _mongoHelper.Collection.AsQueryable().Where(m => m.UserId == userId).Select(x => x.AnswerId).Distinct().ToList();

            var keys = new List<string>();
            foreach (string answer in answerIds)
            {
                // get details about the associated answer and retrieve the questionId
                AnswersController answersController = new AnswersController();
                var actionresult = await answersController.GetAnswer(answer);
                var answerObj = actionresult as OkNegotiatedContentResult<Answer>;

                var key = "questions-getquestion-questionId=" + answerObj.Content.QuestionId;
                keys.Add(key);
            }
            WebApiCacheHelper.InvalidateCacheByKeys(keys);

            // delete all the records in database
            try
            {
                var result = _mongoHelper.Collection.Remove(Query.EQ("UserId", userId));

                // if mongo failed to save the data then send error
                if (!result.Ok)
                    throw new MongoException("failed to delete the comments");
            }
            catch (MongoConnectionException ex)
            {
                throw new MongoException("failed to delete the comments");
            }
        }

        /// <summary>
        /// Edit a new comment for a question
        /// </summary>
        /// <param name="comment">Comment object</param>
        /// <returns></returns>
        [Authorize]
        [ResponseType(typeof(Comment))]
        public async Task<IHttpActionResult> EditComment(Comment comment)
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

                // get associated questionid to invalidate cache for that
                // get details about the associated answer
                AnswersController answersController = new AnswersController();
                var actionresult = await answersController.GetAnswer(commentInDB.AnswerId);
                var answer = actionresult as OkNegotiatedContentResult<Answer>;

                // invalidate the cache for the action those will get impacted due to this new answer post
                var cache = Configuration.CacheOutputConfiguration().GetCacheOutputProvider(Request);

                // invalidate the getquestion api call for the question associated with this answer
                cache.RemoveStartsWith("questions-getquestion-questionId=" + answer.Content.QuestionId);


                return CreatedAtRoute("DefaultApi", new { id = comment.CommentId }, comment);
            }
            catch (MongoDB.Driver.MongoConnectionException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // method to update the useractivitylog collection
        private void UpdateUserActivityLog(UserActivityLog log)
        {
            //Call WebApi to log activity
            var userActivityController = new UserActivityController();
            userActivityController.PostAnActivity(log);
        }
    }
}