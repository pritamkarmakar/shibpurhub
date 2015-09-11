using MongoDB.Driver.Builders;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models.WebAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Results;
using WebApi.OutputCache.V2;
using MongoDB.Bson;
using MongoDB.Driver.Linq;

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
        [CacheOutput(ServerTimeSpan = 0, ExcludeQueryStringFromCacheKey = true, NoCache = true)]
        public async Task<IHttpActionResult> GetPersonalizedFeeds(string userId, int page = 0)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return NotFound();
                }

                var allFeeds = _mongoHelper.Collection.FindAll().OrderByDescending(a => a.HappenedAtUTC).Take((page + 1) * 50).ToList();                

                var helper = new Helper.Helper();
                Task<CustomUserInfo> actionResult = helper.FindUserById(userId);
                var userDetail = await actionResult;

                if(userDetail == null)
                {
                    return NotFound();
                }

                var followedUsers = userDetail.Following ?? new List<string>();
                var followedQuestions = userDetail.FollowedQuestions ?? new List<string>();

                var feedsFollowedByMe = from x in allFeeds
                                        where followedUsers.Contains(x.UserId) || followedQuestions.Contains(x.ActedOnObjectId) 
                                        select x;

                //foreach(var feed in feedsFollowedByMe)
                //{
                //    allFeeds.Remove(feed);
                //}
                //allFeeds.AddRange(feedsFollowedByMe);

                var feeds = feedsFollowedByMe.Skip(page * PAGESIZE).Take(PAGESIZE).ToList();

                var userIds = feeds.Select(a => a.UserId).Distinct();
                var userDetails = new Dictionary<string, FeedUserDetail>();
                
                foreach (var id in userIds)
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

                var feedResults = new List<PersonalizedFeedItem>();
                foreach(var feed in feeds)
                {
                    var feedItem = new PersonalizedFeedItem();

                    var feedContentResult = await GetFeedContent(feed.Activity, feed.ActedOnObjectId, feed.ActedOnUserId);
                    var feedContent = feedContentResult as OkNegotiatedContentResult<FeedContentDetail>;
                    feedItem.ItemHeader = feedContent == null ? string.Empty : feedContent.Content.Header;
                    feedItem.ItemDetail = feedContent == null ? string.Empty : feedContent.Content.SimpleDetail;
                    feedItem.TargetAction = feedContent == null ? string.Empty : feedContent.Content.ActionName;
                    feedItem.TargetActionUrl = feedContent == null ? string.Empty : feedContent.Content.ActionUrl;

                    feedItem.ActionText = GetActionText(feed.Activity);
                    feedItem.ActivityType = feed.Activity;
                    
                    feedItem.ViewCount = feedContent == null ? 0 : feedContent.Content.ViewCount;
                    feedItem.AnswersCount = feedContent == null ? 0 : feedContent.Content.AnswersCount;
                    feedItem.PostedDateInUTC = feedContent == null ? 0 : feedContent.PostedDateInUTC;
                    
                    var matchedUser = userDetails[feed.UserId];
                    if(matchedUser != null)
                    {
                        feedItem.UserName = matchedUser.FullName;
                        feedItem.UserDesignation = matchedUser.CareerDetail;
                        feedItem.UserProfileUrl = string.Format("Account/Profile?userId={0}", feed.UserId);
                        feedItem.UserProfileImageUrl = matchedUser.ImageUrl;
                    }

                    feedResults.Add(feedItem);
                }

                return Ok(feedResults);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // 1: Ask question, 2: Answer, 3: Upvote, 4: Comment, 5: Mark as Answer, 
        // 6: Register as new user, 7: Follow an user, 8: Follow a question, 9: Update profile image
        private async Task<IHttpActionResult> GetFeedContent(int type, string objectId = "", string objectUserId = "")
        {
            var feedContent = new FeedContentDetail();
            feedContent.ActionName = GetActionName(type);
            
            if(type == 1 || type == 2 || type == 3 || type == 4 || type == 5 || type == 8)
            {
                if(string.IsNullOrEmpty(objectId))
                {
                    return NotFound();
                }
                
                var isFeedAnAnswer = (type == 2 || type == 3 || type == 4 || type == 5); // Else its a Question
                Answer answer = null;
                Question question = null;
                
                if(isFeedAnAnswer)
                {
                    answer = _mongoAnswerHelper.Collection.AsQueryable().FirstOrDefault(m => m.AnswerId == objectId);
                }
                
                var questionId = isFeedAnAnswer ? answer.QuestionId : objectId;

                var questionInfo = await _questionController.GetQuestionInfo(questionId);
                var questionResult = questionInfo as OkNegotiatedContentResult<Question>;
                question = questionResult.Content;
                
                feedContent.Header = question.Title;
                feedContent.ActionUrl = "/feed/" + questionId;

                feedContent.ViewCount = question.ViewCount;

                var answersCountResult = _questionController.GetAnswersCount(questionId);
                var answersCount = answersCountResult as OkNegotiatedContentResult<int>;
                feedContent.AnswersCount = answersCount.Content;
                
                if(isFeedAnAnswer)
                {
                    feedContent.SimpleDetail = answer.AnswerText;
                    feedContent.PostedDateInUTC = answer.PostedOnUtc; 
                }
                else
                {
                    feedContent.SimpleDetail = question.Description;
                    feedContent.PostedDateInUTC = question.PostedOnUtc;
                }

                
                return Ok<FeedContentDetail>(feedContent);
            }

            var helper = new Helper.Helper();
            Task<CustomUserInfo> actionResult = helper.FindUserById(objectUserId);
            var userDetail = await actionResult;

            if (type == 6 || type == 7 || type == 9)
            {
                feedContent.Header = userDetail.FirstName + " " + userDetail.LastName;
                return Ok<FeedContentDetail>(feedContent);
            }

            return NotFound();
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
                var current = allEmployments.Content.Where(a => !a.To.HasValue).FirstOrDefault();
                if (current == null)
                {
                    current = allEmployments.Content.OrderByDescending(a => a.From).First();
                }

                text = current == null ? "" : current.Title + ", " + current.CompanyName;

                result = await _educationalHistoriesController.GetEducationalHistories(email);
                var allEducations = result as OkNegotiatedContentResult<List<EducationalHistories>>;
                var currentEducation = allEducations.Content.FirstOrDefault();
                var educationText = string.Empty;
                if (currentEducation != null)
                {
                    educationText = currentEducation.GraduateYear + " " + currentEducation.Department;
                }

                return string.IsNullOrEmpty(text) ? Ok<string>(educationText) : Ok<string>(text + " (" + educationText + ")");
            }
            catch(Exception ex)
            {
                return NotFound();
            }
        }
    }
}