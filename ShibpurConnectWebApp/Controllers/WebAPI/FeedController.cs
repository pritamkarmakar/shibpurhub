using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;
using System.Web.Http.Results;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models.WebAPI;
using System.Text;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Security.Claims;
using ShibpurConnectWebApp.Providers;
using WebApi.OutputCache.V2;
using System.Text.RegularExpressions;
using System.Configuration;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class FeedController : ApiController
    {
        private const int PAGESIZE = 10;

        private MongoHelper<UserActivityLog> _mongoHelper;

        private MongoHelper<Question> _mongoQustionHelper;
        private QuestionsController _questionController;

        private MongoHelper<Answer> _mongoAnswerHelper;
        private AnswersController _answerController;

        private EmploymentHistoriesController _employmentHistoriesController;

        private EducationalHistoriesController _educationalHistoriesController;

        public FeedController()
        {
            _mongoHelper = new MongoHelper<UserActivityLog>();

            _mongoQustionHelper = new MongoHelper<Question>();
            _questionController = new QuestionsController();

            _mongoAnswerHelper = new MongoHelper<Answer>();
            _answerController = new AnswersController();


            _employmentHistoriesController = new EmploymentHistoriesController();
            _educationalHistoriesController = new EducationalHistoriesController();
        }

        /// <summary>
        /// Gets my feeds.
        /// </summary>
        /// <param name="userId">My user identifier.</param>
        /// <param name="page">The page.</param>
        /// <returns></returns>
        [CacheControl()]
        [CacheOutput(ServerTimeSpan = 20, ExcludeQueryStringFromCacheKey = true, NoCache = true)]
        public async Task<IHttpActionResult> GetPersonalizedFeeds(int page = 0)
        {
            try
            {
                ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
                if (principal == null || principal.Identity == null || string.IsNullOrEmpty(principal.Identity.Name))
                {
                    return BadRequest("No UserId is found");
                }

                var email = principal.Identity.Name;

                var helper = new Helper.Helper();
                var userResult = helper.FindUserByEmail(email);
                var userDetail = await userResult;
                if (userDetail == null)
                {
                    return BadRequest("No UserId is found");
                }
                
                var userId = userDetail.Id;

                var allFeeds = _mongoHelper.Collection.FindAll().OrderByDescending(a => a.HappenedAtUTC).Skip(page * PAGESIZE).Take(50).ToList();                

                
                //Task<CustomUserInfo> actionResult = helper.FindUserById(userId);
                //var userDetail = await actionResult;

                //if(userDetail == null)
                //{
                    //return NotFound();
                //}

                var followedUsers = userDetail.Following ?? new List<string>();
                var followedQuestions = userDetail.FollowedQuestions ?? new List<string>();

                var feedsFollowedByMe = from x in allFeeds
                                        where followedUsers.Contains(x.UserId) || 
                                              (followedQuestions.Contains(x.ActedOnObjectId) && x.UserId != userId)
                                        select x;

                //foreach(var feed in feedsFollowedByMe)
                //{
                //    allFeeds.Remove(feed);
                //}
                //allFeeds.AddRange(feedsFollowedByMe);

                //var feeds = feedsFollowedByMe.Skip(page * PAGESIZE).Take(PAGESIZE).ToList();

                var feedResults = new List<PersonalizedFeedItem>();
                string previousObjectId = string.Empty;
                var distinctUserId = new List<string>();

                var lstLogsWithContent = new List<UserActivityLogWithContent>();
                foreach (var feedFollowedByMe in feedsFollowedByMe)
                {
                    var logWithContent = new UserActivityLogWithContent(feedFollowedByMe);
                    lstLogsWithContent.Add(logWithContent);
                }

                var allfeedsWithContentResult = await GetAllFeedContents(lstLogsWithContent);
                var z = allfeedsWithContentResult as OkNegotiatedContentResult<IList<FeedContentDetail>>;
                var allfeedsWithContent = z.Content ?? new List<FeedContentDetail>();

                for(var i = 0; feedResults.Count < PAGESIZE && i < feedsFollowedByMe.ToList().Count; i++)
                {
                //foreach(var feed in feeds)
                //{
                    var feeds = feedsFollowedByMe.ToList();
                    var feed = feeds[i];
                    
                    //Ignore Upvote
                    if(feed.Activity == 3)
                    {
                        continue;
                    }
                    
                    var feedItem = new PersonalizedFeedItem();
                    feedItem.UserId = feed.UserId;
                    if(!distinctUserId.Contains(feed.UserId))
                    {
                        distinctUserId.Add(feed.UserId);
                    }

                    FeedContentDetail feedContent = null;

                    if (feed.Activity == 6 || feed.Activity == 7 || feed.Activity == 9)
                    {
                        var feedContentResult = await GetFeedContent(feed.Activity, feed.UserId, feed.ActedOnObjectId, feed.ActedOnUserId);
                        var y = feedContentResult as OkNegotiatedContentResult<FeedContentDetail>;
                        feedContent = y.Content;
                    }
                    else
                    {
                        feedContent = allfeedsWithContent.FirstOrDefault(a => a.LogId == feed.ActivityLogId);
                    }


                    feedItem.ItemHeader = feedContent == null ? string.Empty : feedContent.Header;
                    feedItem.ItemDetail = feedContent == null ? string.Empty : feedContent.SimpleDetail;
                    feedItem.TargetAction = feedContent == null ? string.Empty : feedContent.ActionName;
                    feedItem.TargetActionUrl = feedContent == null ? string.Empty : feedContent.ActionUrl;

                    feedItem.ActionText = GetActionText(feed.Activity);
                    feedItem.ActivityType = feed.Activity;
                    
                    feedItem.ViewCount = feedContent == null ? 0 : feedContent.ViewCount;
                    feedItem.AnswersCount = feedContent == null ? 0 : feedContent.AnswersCount;
                    feedItem.PostedDateInUTC = feedContent == null ? DateTime.Now : feedContent.PostedDateInUTC;
                    
                    
                    
                    if(previousObjectId == feed.ActedOnObjectId)
                    {
                        if(feedItem.ChildItems == null)
                        {
                            feedItem.ChildItems = new List<PersonalizedFeedItem>();
                        }
                        
                        feedItem.ChildItems.Add(feedItem);
                    }
                    else
                    {
                        feedResults.Add(feedItem);
                    }
                    
                    previousObjectId = feed.ActedOnObjectId;
                //}
                }
                
                //var userIds = feeds.Select(a => a.UserId).Distinct();
                var userDetails = new Dictionary<string, FeedUserDetail>();
                
                foreach (var id in distinctUserId)
                {
                    Task<CustomUserInfo> result = helper.FindUserById(id);
                    var userDetailInList = await result;
                    var user = new FeedUserDetail
                    {
                        FullName = userDetailInList.FirstName + " " + userDetailInList.LastName,                        
                        ImageUrl = userDetailInList.ProfileImageURL
                    };
                    var careerTextResult = await GetDesignationText(userDetailInList.Email);
                    var careerText = careerTextResult as OkNegotiatedContentResult<string>;
                    user.CareerDetail = careerText == null ? string.Empty : careerText.Content;

                    userDetails.Add(id, user);
                }
                
                foreach(var feedResult in feedResults)
                {
                    var matchedUser = userDetails[feedResult.UserId];
                    if(matchedUser != null)
                    {
                        feedResult.UserName = matchedUser.FullName;
                        feedResult.UserDesignation = matchedUser.CareerDetail;
                        feedResult.UserProfileUrl = string.Format("Account/Profile?userId={0}", feedResult.UserId);
                        feedResult.UserProfileImageUrl = matchedUser.ImageUrl;
                    }
                    
                    // TO-DO: Do the same for child items
                }
                
                var orderedFeedResults = feedResults.OrderBy(a => a.ActivityType).ThenByDescending(b => b.PostedDateInUTC).ToList();

                return Ok(orderedFeedResults);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message + System.Environment.NewLine + ex.StackTrace);
            }
        }

        private async Task<IHttpActionResult> GetAllFeedContents(IList<UserActivityLogWithContent> logs)
        {
            try
            {
                var answerIds = logs.Where(a => a.Activity == 2 || a.Activity == 4 || a.Activity == 5).Select(a => a.ActedOnObjectId).ToList();
                var answers = from a in _mongoAnswerHelper.Collection.AsQueryable()
                              where answerIds.Contains(a.AnswerId)
                              select a;

                var questionIds = logs.Where(a => a.Activity == 1 || a.Activity == 8).Select(a => a.ActedOnObjectId).ToList();
                var questionIdsFromAnswers = from o in answers.ToList()
                                             select o.QuestionId;
                questionIds.AddRange(questionIdsFromAnswers.ToList());

                var questions = from b in _mongoQustionHelper.Collection.AsQueryable()
                                where questionIds.Contains(b.QuestionId)
                                select b;

                var feedContents = new List<FeedContentDetail>();
                foreach (var feed in logs)
                {
                    var feedContent = new FeedContentDetail();
                    feedContent.ActionName = GetActionName(feed.Activity);
                    feedContent.LogId = feed.ActivityLogId;

                    Answer answer = null;
                    Question question = null;
                    var isFeedAnAnswer = (feed.Activity == 2 || feed.Activity == 4 || feed.Activity == 5); // Else its a Question

                    if (isFeedAnAnswer)
                    {
                        answer = answers.FirstOrDefault(a => a.AnswerId == feed.ActedOnObjectId);
                        if (answer != null)
                        {
                            question = questions.FirstOrDefault(b => b.QuestionId == answer.QuestionId);
                        }
                    }

                    if (feed.Activity == 1 || feed.Activity == 8)
                    {
                        question = questions.FirstOrDefault(b => b.QuestionId == feed.ActedOnObjectId);
                    }

                    if (question != null)
                    {
                        feedContent.Header = question.Title;
                        var questionUrl = "/feed/" + question.QuestionId;
                        feedContent.ActionUrl = isFeedAnAnswer ? questionUrl + "/" + feed.ActedOnObjectId : questionUrl;

                        feedContent.ViewCount = question.ViewCount;

                        var answersCountResult = _questionController.GetAnswersCount(question.QuestionId);
                        var answersCount = answersCountResult as OkNegotiatedContentResult<int>;
                        feedContent.AnswersCount = answersCount.Content;

                        feedContent.SimpleDetail = question.Description;
                        feedContent.PostedDateInUTC = question.PostedOnUtc;
                    }

                    if (isFeedAnAnswer && answer != null)
                    {
                        feedContent.SimpleDetail = answer.AnswerText;
                        feedContent.PostedDateInUTC = answer.PostedOnUtc;
                    }

                    feedContents.Add(feedContent);
                }

                return Ok<IList<FeedContentDetail>>(feedContents);
            }
            catch(Exception e)
            {
                return BadRequest(e.Message + System.Environment.NewLine + e.StackTrace);
            }
        }

        // 1: Ask question, 2: Answer, 3: Upvote, 4: Comment, 5: Mark as Answer, 
        // 6: Register as new user, 7: Follow an user, 8: Follow a question, 9: Update profile image
        private async Task<IHttpActionResult> GetFeedContent(int type, string userId, string objectId = "", string objectUserId = "")
        {
            //int type = feed.Activity;
            //string userId = feed.UserId; 
            //string objectId = feed.ActedOnObjectId; 
            //string objectUserId = feed.ActedOnUserId;

            var feedContent = new FeedContentDetail();
            feedContent.ActionName = GetActionName(type);

            try
            {
                //if (type == 1 || type == 2 || type == 4 || type == 5 || type == 8)
                //{
                //    if (string.IsNullOrEmpty(objectId))
                //    {
                //        return NotFound();
                //    }

                //    var isFeedAnAnswer = (type == 2 || type == 4 || type == 5); // Else its a Question
                //    Answer answer = null;
                //    Question question = null;

                //    if (isFeedAnAnswer)
                //    {
                //        //answer = _mongoAnswerHelper.Collection.AsQueryable().FirstOrDefault(m => m.AnswerId == objectId);
                //        if (answer == null)
                //        {
                //            return NotFound();
                //        }
                //    }

                //    var questionId = isFeedAnAnswer ? answer.QuestionId : objectId;

                //    //var questionInfo = await _questionController.GetQuestionInfo(questionId);
                //    //var questionResult = questionInfo as OkNegotiatedContentResult<Question>;
                //    //question = questionResult.Content;

                //    feedContent.Header = question.Title;
                //    var questionUrl = "/feed/" + questionId;
                //    feedContent.ActionUrl = isFeedAnAnswer ? questionUrl + "/" + objectId : questionUrl;

                //    feedContent.ViewCount = question.ViewCount;

                //    var answersCountResult = _questionController.GetAnswersCount(questionId);
                //    var answersCount = answersCountResult as OkNegotiatedContentResult<int>;
                //    feedContent.AnswersCount = answersCount.Content;

                //    if (isFeedAnAnswer)
                //    {
                //        feedContent.SimpleDetail = answer.AnswerText;
                //        feedContent.PostedDateInUTC = answer.PostedOnUtc;
                //    }
                //    else
                //    {
                //        feedContent.SimpleDetail = question.Description;
                //        feedContent.PostedDateInUTC = question.PostedOnUtc;
                //    }


                //    return Ok<FeedContentDetail>(feedContent);
                //}

                var helper = new Helper.Helper();
                Task<CustomUserInfo> actionResult = helper.FindUserById(userId);
                var userDetail = await actionResult;

                if (type == 6 || type == 7 || type == 9)
                {
                    feedContent.Header = userDetail.FirstName + " " + userDetail.LastName;
                    return Ok<FeedContentDetail>(feedContent);
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                return NotFound();
            }
        }

        private string GetActionText(int type)
        {
            if(type == 1)
            {
                return " asked a ";
            }

            if (type == 2)
            {
                return " answered a ";
            }

            if (type == 3)
            {
                return " upvoted a ";
            }

            if (type == 4)
            {
                return " commented on a ";
            }

            if (type == 5)
            {
                return " marked an answer for ";
            }

            if (type == 6)
            {
                return " joined ";
            }

            if (type == 7 || type == 8)
            {
                return " started following ";
            }

            if (type == 9)
            {
                return " updated profile image ";
            }

            return string.Empty;
        }

        private string GetActionName(int type)
        {
            if (type == 1 || type == 2 || type == 5 || type == 8)
            {
                return "Question";
            }

            if (type == 3 || type == 4)
            {
                return "Answer";
            }

            return string.Empty;
        }

        private async Task<IHttpActionResult> GetDesignationText(string email)
        {
            var text = string.Empty;
            if(string.IsNullOrEmpty(email))
            {
                return NotFound();
            }

            try
            {
                var result = await _employmentHistoriesController.GetEmploymentHistories(email);
                var allEmployments = result as OkNegotiatedContentResult<List<EmploymentHistories>>;
                if (allEmployments != null)
                {
                    var current = allEmployments.Content.Where(a => !a.To.HasValue).FirstOrDefault();
                    if (current == null)
                    {
                        current = allEmployments.Content.OrderByDescending(a => a.From).First();
                    }

                    text = current == null ? "" : current.Title + ", " + current.CompanyName;
                }

                result = await _educationalHistoriesController.GetEducationalHistories(email);
                var allEducations = result as OkNegotiatedContentResult<List<EducationalHistories>>;
                if (allEducations != null)
                {
                    var currentEducation = allEducations.Content.FirstOrDefault();
                    var educationText = string.Empty;
                    if (currentEducation != null)
                    {
                        educationText = currentEducation.GraduateYear + " " + currentEducation.Department;
                    }

                    return string.IsNullOrEmpty(text) ? Ok<string>(educationText) : Ok<string>(text + " (" + educationText + ")");
                }

                return Ok<string>(text);
            }
            catch(Exception ex)
            {
                return NotFound();
            }
        }
    }
}