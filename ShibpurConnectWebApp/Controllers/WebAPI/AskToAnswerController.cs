using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models.WebAPI;
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
using MongoDB.Driver.Linq;
using WebApi.OutputCache.V2;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    public class AskToAnswerController : ApiController
    {
        private MongoHelper<AskToAnswer> _mongoHelper;

        public AskToAnswerController()
        {
            _mongoHelper = new MongoHelper<AskToAnswer>();
        }

        /// <summary>
        ///  API to check if we have asked a user to answer a question
        /// </summary>
        /// <param name="questionId">question id</param>
        /// <param name="userId">User to whom we have asked to answer the question</param>
        /// <returns>AskToAnswer object; if not found then null</returns>
        public AskToAnswer GetAskToAnswer(string questionId, string userId)
        {
            var result = (from e in _mongoHelper.Collection.AsQueryable<AskToAnswer>()
                          where e.AskedTo == userId && e.QuestionId == questionId
                          select e).ToList();

            if (result.Count == 0)
                return null;
            else
                return result[0];
        }

        /// <summary>
        /// Get the response rate for a user. We will calculate this based on how many request for answer he/she has received 
        /// and how many he/she responded
        /// </summary>
        /// <param name="userId">user id</param>
        /// <returns></returns>
       [CacheOutput(ServerTimeSpan = 864000, ExcludeQueryStringFromCacheKey = true)]
        public string GetResponseRate(string userId)
        {
            // total response received
            int responseReceived = 0;
            var result = (from e in _mongoHelper.Collection.AsQueryable<AskToAnswer>()
                          where e.AskedTo == userId
                          select e).ToList();

            if (result.Count > 0)
                responseReceived = result.Count;
            else
                return "NA";


            // response given
            var responseGiven = (from e in _mongoHelper.Collection.AsQueryable<AskToAnswer>()
                                 where e.AskedTo == userId && e.HasAnswered == true
                                 select e).ToList();

            if (responseGiven.Count == 0)
                return "0%";
            else
            {
                // calculate response rate
                double rRate = ((double)responseGiven.Count / (double)responseReceived) * 100;

                return Convert.ToInt32(rRate).ToString() + "%";
            }

        }

        /// <summary>
        /// Update the 'HasAnswered' column for a particular record
        /// </summary>
        /// <param name="askToAnswer"></param>
        internal void UpdateHasAnswered(AskToAnswer askToAnswer)
        {
            if(askToAnswer == null)
                return;

            askToAnswer.HasAnswered = true;

            _mongoHelper.Collection.Save(askToAnswer);
        }


        /// <summary>
        /// API to save the Ask to answer request 
        /// </summary>
        /// <param name="askToAnswer"></param>
        /// <returns></returns>
        [Authorize]
        [ResponseType(typeof(AskToAnswer))]
        public async Task<IHttpActionResult> PostAskToAnswer(AskToAnswerDTO askToAnswerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (askToAnswerDto == null)
                return BadRequest("Request body is null. Please send a valid AskToAnswer object");

            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
            var claim = principal.FindFirst("sub");

            Helper.Helper helper = new Helper.Helper();
            var userResult = helper.FindUserByEmail(claim.Value);
            var userInfo = await userResult;
            if (userInfo == null)
            {
                return BadRequest("No UserId is found");
            }

            // send a notification to user to answer the question, only if we haven't sent the same notification to that user before
            if (GetAskToAnswer(askToAnswerDto.QuestionId, askToAnswerDto.AskedTo) == null)
            {
                // get the hostname
                Uri myuri = new Uri(System.Web.HttpContext.Current.Request.Url.AbsoluteUri);
                string pathQuery = myuri.PathAndQuery;
                string hostName = myuri.ToString().Replace(pathQuery, "");

                // get details about associated question
                QuestionsController questionsController = new QuestionsController();
                var actionresult = await questionsController.GetQuestionInfo(askToAnswerDto.QuestionId);
                var question = actionresult as OkNegotiatedContentResult<Question>;

                EmailsController emailsController = new EmailsController();
                await emailsController.SendEmail(new Email()
                {
                    UserId = askToAnswerDto.AskedTo,
                    Body =
                        "<a href='" + hostName + "/Account/Profile?userId=" + askToAnswerDto.AskedBy +
                        "' style='text-decoration:none'>" + userInfo.FirstName + " " + userInfo.LastName + "</a>" +
                        " requested you to answer <a href='" + hostName + "/feed/"  + question.Content.UrlSlug + "' style='text-decoration:none'>" +
                        question.Content.Title + "</a>",
                    Subject = "ShibpurHub | You have a new request to answer a question"
                });

                // save this new request in the notification collection, so that user will get that bubble notification in the header
                NotificationsController notificationsController = new NotificationsController();
                string notificationContent = "{\"askedBy\":\"" + askToAnswerDto.AskedBy + "\",\"displayName\":\"" + userInfo.FirstName + " " + userInfo.LastName + "\",\"profileImage\":\"" + userInfo.ProfileImageURL + "\",\"questionId\":\"" + askToAnswerDto.QuestionId + "\",\"questionTitle\":\"" + question.Content.Title + "\"}";
                notificationsController.PostNotification(new Notifications()
                {
                    UserId = askToAnswerDto.AskedTo,
                    NotificationContent = notificationContent,
                    NotificationType = NotificationTypes.AskToAnswer
                });

                // object that we will save in the database
                AskToAnswer askToAnswer = new AskToAnswer();
                askToAnswer.AskedBy = askToAnswerDto.AskedBy;
                askToAnswer.AskedTo = askToAnswerDto.AskedTo;
                askToAnswer.QuestionId = askToAnswerDto.QuestionId;
                askToAnswer.AskedOnUtc = DateTime.UtcNow;

                // save the notification to the database collection
                var result = _mongoHelper.Collection.Save(askToAnswer);

                // if mongo failed to save the data then send error
                if (!result.Ok)
                    return InternalServerError();

                return CreatedAtRoute("DefaultApi", new { id = askToAnswer.Id }, askToAnswer);
            }

            return BadRequest("This user already requested to answer this question");
        }
    }
}
