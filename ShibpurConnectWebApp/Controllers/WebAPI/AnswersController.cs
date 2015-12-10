using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Http.Results;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models;
using ShibpurConnectWebApp.Models.WebAPI;
using System.Security.Claims;
using System.Threading.Tasks;
using Hangfire;
using WebApi.OutputCache.V2;
using MongoDB.Driver.Builders;
using MongoDB.Driver;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    public class AnswersController : ApiController
    {
        private MongoHelper<Answer> _mongoHelper;
        private const int PAGESIZE = 20;
        private static string hostName = string.Empty;

        public AnswersController()
        {
            _mongoHelper = new MongoHelper<Answer>();
        }

        // GET: api/Questions
        /// <summary>
        /// Will return all available Answers
        /// </summary>
        /// <returns></returns>
        public IList<Answer> GetAnswers()
        {
            var result = _mongoHelper.Collection.FindAll().OrderBy(a => a.PostedOnUtc).ToList();
            return result;
        }

        /// <summary>
        /// API to get details about a specific answer
        /// </summary>
        /// <param name="answerId">answerid to search</param>
        /// <returns></returns>
        [HttpGet]
        [CacheOutput(ServerTimeSpan = 864000, ExcludeQueryStringFromCacheKey = true, NoCache = true)]
        public async Task<IHttpActionResult> GetAnswer(string answerId)
        {
            // validate questionId is valid hex string
            try
            {
                ObjectId.Parse(answerId);
            }
            catch (Exception)
            {
                return BadRequest(String.Format("Supplied answerId: {0} is not a valid object id", answerId));
            }


            var answer = _mongoHelper.Collection.AsQueryable().FirstOrDefault(m => m.AnswerId == answerId);
            if (answer == null)
            {
                return NotFound();
            }

            return Ok(answer);
        }

        // GET: api/Questions/5
        // Will return all the answers for a specific question
        [ResponseType(typeof(Answer))]
        [CacheOutput(ServerTimeSpan = 864000, ExcludeQueryStringFromCacheKey = true, NoCache = true)]
        public IHttpActionResult GetAnswers(string questionId)
        {
            // validate questionId is valid hex string
            try
            {
                ObjectId.Parse(questionId);
            }
            catch (Exception)
            {
                return BadRequest(String.Format("Supplied questionId: {0} is not a valid object id", questionId));
            }


            var questions = _mongoHelper.Collection.AsQueryable().Where(m => m.QuestionId == questionId).OrderBy(a => a.MarkedAsAnswer).ThenBy(b => b.PostedOnUtc);
            if (questions.Count() == 0)
            {
                return NotFound();
            }

            return Ok(questions.ToList());
        }

        /// <summary>
        /// Get the total answer count of a user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet]
        [CacheOutput(ServerTimeSpan = 864000, ExcludeQueryStringFromCacheKey = true, NoCache = true)]
        public async Task<IHttpActionResult> GetUserAnswerCount(string userId)
        {
            try
            {
                var answerCount = _mongoHelper.Collection.AsQueryable().Count(m => m.UserId == userId);
                return Ok(answerCount);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// API to get list of answers posted by an user
        /// </summary>
        /// <param name="userId">userId for whom we want this list</param>
        /// <param name="page">page index</param>
        /// <returns></returns>
        [CacheOutput(ServerTimeSpan = 864000, ExcludeQueryStringFromCacheKey = true, NoCache = true)]
        public async Task<IHttpActionResult> GetAnswersByUser(string userId, int page)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest("userId can't be null or empty string");

            try
            {
                var allAnswers = _mongoHelper.Collection.AsQueryable().Where(m => m.UserId == userId).ToList();
                var answers = allAnswers.OrderByDescending(a => a.PostedOnUtc).Skip(page * PAGESIZE).Take(PAGESIZE).ToList();

                // get the question title using the question id
                List<AnswerWithQuestionTitle> finalList = new List<AnswerWithQuestionTitle>();
                // hashset to check unique questionid, one user can have submitted multiple answers to same question
                HashSet<string> hash = new HashSet<string>();
                foreach (var answer in answers)
                {
                    if (!hash.Contains(answer.QuestionId))
                    {
                        QuestionsController questionsController = new QuestionsController();
                        IHttpActionResult actionresult = await questionsController.GetQuestionInfo(answer.QuestionId);
                        var questionObj = actionresult as OkNegotiatedContentResult<Question>;

                        if (questionObj != null)
                        {
                            finalList.Add(new AnswerWithQuestionTitle()
                            {
                                QuestionId = questionObj.Content.QuestionId,
                                QuestionTitle = questionObj.Content.Title,
                                AnswerText = answer.AnswerText,
                                PostedOnUtc = answer.PostedOnUtc

                            });
                        }

                        hash.Add(answer.QuestionId);
                    }
                }
                return Ok(finalList);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: api/Questions
        [Authorize]
        [ResponseType(typeof(Answer))]
        [InvalidateCacheOutput("GetQuestions", typeof(QuestionsController))]
        [InvalidateCacheOutput("GetPopularQuestions", typeof(QuestionsController))]
        [InvalidateCacheOutput("GetLeaderBoard", typeof(UsersController))]
        public async Task<IHttpActionResult> PostAnswer(AnswerDTO answerdto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (answerdto == null)
                return BadRequest("Request body is null. Please send a valid Answer object");

            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
            var email = principal.Identity.Name;

            Helper.Helper helper = new Helper.Helper();
            var userResult = helper.FindUserByEmail(email);
            var userInfo = await userResult;
            if (userInfo == null)
            {
                return BadRequest("No UserId is found");
            }

            try
            {
                // validate given questionid is valid
                var questionMongoHelper = new MongoHelper<Question>();
                var question = questionMongoHelper.Collection.AsQueryable().Where(m => m.QuestionId == answerdto.QuestionId).ToList().FirstOrDefault();
                if (question == null)
                    return BadRequest("Supplied questionid is invalid");

                // create the Answer object to save to database
                Answer answer = new Answer()
                {
                    AnswerId = ObjectId.GenerateNewId().ToString(),
                    AnswerText = answerdto.AnswerText,
                    PostedOnUtc = DateTime.UtcNow,
                    QuestionId = answerdto.QuestionId,
                    UserId = userInfo.Id,
                };

                // save the answer to the database
                var result = _mongoHelper.Collection.Save(answer);

                // post user activity for this new answer
                var userActivityLog = new UserActivityLog
                {
                    Activity = 2,
                    UserId = userInfo.Id,
                    ActedOnObjectId = answer.AnswerId,
                    ActedOnUserId = string.Empty
                };

                UpdateUserActivityLog(userActivityLog);

                // if mongo failed to save the data then send error
                if (!result.Ok)
                    return InternalServerError();

                // change the "HasAnswered' column of the question. If it is false then change to true
                if (question.HasAnswered == false)
                {
                    question.HasAnswered = true;
                    questionMongoHelper.Collection.Save(question);
                }

                // check if this user has been requested to answer this question. Then we have to 
                // update the 'AskToAnswer' collection to mark 'HasAnswered' as true
                AskToAnswerController askToAnswerController = new AskToAnswerController();
                AskToAnswer askToAnswer = askToAnswerController.GetAskToAnswer(answerdto.QuestionId, userInfo.Id);
                if (askToAnswer != null)
                    askToAnswerController.UpdateHasAnswered(askToAnswer);

                // get the hostname
                Uri myuri = new Uri(System.Web.HttpContext.Current.Request.Url.AbsoluteUri);
                string pathQuery = myuri.PathAndQuery;
                hostName = myuri.ToString().Replace(pathQuery, "");

                // send notification to the user who posted the question
                EmailsController emailsController = new EmailsController();
                NotificationsController notificationsController = new NotificationsController();
                if (question.UserId != userInfo.Id)
                {
                    await emailsController.SendEmail(new Email()
                    {
                        UserId = question.UserId,
                        Body = "<a href='" + hostName + "/Account/Profile?userId=" + userInfo.Id + "' style='text-decoration:none'>" + userInfo.FirstName + " " + userInfo.LastName + "</a>" + " posted an answer to your question <a href='" + hostName + "/feed/" + question.UrlSlug + "' style='text-decoration:none'>" + question.Title + "</a><i>" + answerdto.AnswerText + "</i>",
                        Subject = "ShibpurHub | New answer to your question \"" + question.Title + "\""
                    });

                    notificationsController.PostNotification(new Notifications()
                    {
                        UserId = question.UserId,
                        PostedOnUtc = DateTime.UtcNow,
                        NewNotification = true,
                        NotificationType = NotificationTypes.ReceivedAnswer,
                        NotificationContent = "{\"answeredBy\":\"" + userInfo.Id + "\",\"displayName\":\"" + userInfo.FirstName + " " + userInfo.LastName + "\",\"questionId\":\"" + answerdto.QuestionId + "\",\"profileImage\":\"" + userInfo.ProfileImageURL + "\",\"questionTitle\":\"" + question.UrlSlug + "\"}"
                    });
                }

                // send notification to all the followers of this question. This will be done as a background task
                BackgroundJob.Enqueue(() => NotificationToAllFollowers(question, userInfo, answerdto.AnswerText));

                // invalidate the cache for the action those will get impacted due to this new answer post
                var cache = Configuration.CacheOutputConfiguration().GetCacheOutputProvider(Request);

                // invalidate the getquestion api call for the question associated with this answer
                cache.RemoveStartsWith("questions-getquestion-questionId=" + answerdto.QuestionId);

                // invalidate the responserate api for the user who posted this answer
                cache.RemoveStartsWith("asktoanswer-getresponserate-userId=" + userInfo.Id);

                // invalidate the GetAnswersCount api for this question
                cache.RemoveStartsWith("questions-getanswerscount-questionId=" + answerdto.QuestionId);

                // invalidate the GetAnswers api for this answer
                cache.RemoveStartsWith("answers-getanswers-answerId=" + answer.AnswerId);

                // invalidate the GetAnswersByUser api for the user who is posting this answer
                cache.RemoveStartsWith("answers-getanswersbyuser-userId=" + userInfo.Id);

                // invalidate the GetProfileByUserId api for the user who is posting this answer
                cache.RemoveStartsWith("profile-getprofilebyuserid-userId=" + userInfo.Id);

                // invalidate the notification cache for this user
                cache.RemoveStartsWith("notifications-getnewnotifications-userId=" + question.UserId);
                cache.RemoveStartsWith("notifications-getnotifications-userId=" + question.UserId);

                //Invalidate personalized feed cache
                var userIdToInvalidate = question.Followers == null ? new List<string>() : question.Followers;
                userIdToInvalidate.Add(userInfo.Id);
                BackgroundJob.Enqueue(() => WebApiCacheHelper.InvalidatePersonalizedFeedCache(userIdToInvalidate));

                return CreatedAtRoute("DefaultApi", new { id = answer.QuestionId }, answer);
            }
            catch (MongoDB.Driver.MongoConnectionException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        public async Task<int> UpdateUpVoteCount(Answer answer)
        {
            try
            {
                ObjectId.Parse(answer.AnswerId);
            }
            catch (Exception)
            {
                return 0;
            }

            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
            var email = principal.Identity.Name;

            Helper.Helper helper = new Helper.Helper();
            var userResult = helper.FindUserByEmail(email);
            var userInfo = await userResult;

            if (userInfo == null)
            {
                return 0;
            }

            var answerInDB = _mongoHelper.Collection.AsQueryable().Where(m => m.AnswerId == answer.AnswerId).FirstOrDefault();
            if (answerInDB != null)
            {
                var upCount = answerInDB.UpVoteCount;
                if(answerInDB.UpvotedByUserIds != null && answerInDB.UpvotedByUserIds.Any(a => a == userInfo.Id))
                {
                    upCount = upCount == 0 ? 0 : upCount - 1;
                    answerInDB.UpvotedByUserIds.Remove(userInfo.Id);
                }
                else
                {
                    upCount += 1;
                    if (answerInDB.UpvotedByUserIds == null)
                    {
                        answerInDB.UpvotedByUserIds = new List<string>();
                    }
                    answerInDB.UpvotedByUserIds.Add(userInfo.Id);
                    
                    //var userActivityLog = new UserActivityLog
                    //{
                    //    Activity = 3,
                    //    UserId = userInfo.Id,
                    //    ActedOnObjectId = answer.AnswerId,
                    //    ActedOnUserId = answer.UserId
                    //};
                    
                    ////UpdateUserActivityLog(userActivityLog);
                    //BackgroundJob.Enqueue(() => UpdateUserActivityLog(userActivityLog));
                }
                
                answerInDB.UpVoteCount = upCount;
                _mongoHelper.Collection.Save(answerInDB);
                return upCount;
            }

            return 0;
        }

        public int UpdateDownVoteCount(Answer answer)
        {
            try
            {
                ObjectId.Parse(answer.AnswerId);
            }
            catch (Exception)
            {
                return 0;
            }

            var answerInDB = _mongoHelper.Collection.AsQueryable().Where(m => m.AnswerId == answer.AnswerId).FirstOrDefault();
            if (answerInDB != null)
            {
                var downCount = answerInDB.DownVoteCount + 1;
                answerInDB.DownVoteCount = downCount;
                _mongoHelper.Collection.Save(answerInDB);
                return downCount;
            }

            return 0;
        }

        [Authorize]
        public async Task<IHttpActionResult> UpdateMarkAsAnswer(List<Answer> answers)
        {
            if (answers == null || answers.Count == 0)
            {
                return BadRequest(ModelState);
            }

            // retrieve the user information who triggered this
            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
            var email = principal.Identity.Name;

            Helper.Helper helper = new Helper.Helper();
            var userResult = helper.FindUserByEmail(email);
            var userInfo = await userResult;
            if (userInfo == null)
            {
                return BadRequest("No UserId is found");
            }

            foreach (var answer in answers)
            {
                var answerInDB = _mongoHelper.Collection.AsQueryable().Where(m => m.AnswerId == answer.AnswerId).FirstOrDefault();
                if (answerInDB != null)
                {
                    answerInDB.MarkedAsAnswer = answer.MarkedAsAnswer;
                }
                _mongoHelper.Collection.Save(answerInDB);

                // post user activity for this new answer
                var userActivityLog = new UserActivityLog
                {
                    Activity = 5,
                    UserId = userInfo.Id,
                    ActedOnObjectId = answer.AnswerId,
                    ActedOnUserId = answer.UserId
                };

                UpdateUserActivityLog(userActivityLog);
            }

            return CreatedAtRoute("DefaultApi", null, true);
        }

        [Authorize]
        [ResponseType(typeof(Answer))]
        public async Task<IHttpActionResult> EditAnswer(Answer answer)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (answer == null)
                return BadRequest("Request body is null. Please send a valid Answer object");

            try
            {
                var answerInDB = _mongoHelper.Collection.AsQueryable().Where(m => m.AnswerId == answer.AnswerId).FirstOrDefault();
                if (answerInDB == null)
                {
                    return NotFound();
                }

                answerInDB.AnswerText = answer.AnswerText;
                answerInDB.LastEditedOnUtc = DateTime.UtcNow;
                _mongoHelper.Collection.Save(answerInDB);

                // invalidate the cache for the action those will get impacted due to this new answer post
                var cache = Configuration.CacheOutputConfiguration().GetCacheOutputProvider(Request);

                // invalidate the getquestion api call for the question associated with this answer
                cache.RemoveStartsWith("questions-getquestion-questionId=" + answer.QuestionId);

                return CreatedAtRoute("DefaultApi", new { id = answer.AnswerId }, answer);

            }
            catch (MongoDB.Driver.MongoConnectionException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Method to send email notification to all the followers of the question
        /// We will send notification whenever there will be a new answer post in the question
        /// </summary>
        [DisableConcurrentExecution(3600)]
        public async void NotificationToAllFollowers(Question question, CustomUserInfo userInfo, string answerText)
        {
            EmailsController emailsController = new EmailsController();
            if (question.Followers != null)
            {
                foreach (var follower in question.Followers)
                {
                    // we don't want to send the notification to the user who posted the question if he again a follower. 
                    // As we are already sending that user separate notification. Also we don't want to send notification if the follower and who posted the answer is the same user
                    if (follower != question.UserId && userInfo.Id != follower)
                    {
                        await emailsController.SendEmail(new Email()
                        {
                            UserId = follower,
                            Body =
                                "<a href='" + hostName + "/Account/Profile?userId=" + userInfo.Id +
                                "' style='text-decoration:none'>" + userInfo.FirstName + " " + userInfo.LastName +
                                "</a>" +
                                " posted an answer to the question <a href='" + hostName + "/feed/" + question.UrlSlug +
                                "' style='text-decoration:none'>" + question.Title + "</a><i>" + answerText + "</i>",
                            Subject = "ShibpurHub | New answer to the question \"" + question.Title + "\""
                        });
                    }
                }
            }
        }

        //[Authorize]
        [HttpPost]
        public bool DeleteAnswer(Answer answer)
        {
            try
            {                
                //var query = Query.EQ("answerId", answer.AnswerId);
                var result = _mongoHelper.Collection.Remove(Query.EQ("answerId", answer.AnswerId), RemoveFlags.Single);

                var cache = Configuration.CacheOutputConfiguration().GetCacheOutputProvider(Request);
                cache.RemoveStartsWith("answers-getanswers-answerId=" + answer.AnswerId);

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private void UpdateUserActivityLog(UserActivityLog log)
        {
            //Call WebApi to log activity
            var userActivityController = new UserActivityController();
            userActivityController.PostAnActivity(log);
        }        
        
    }
}

