using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models.WebAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using MongoDB.Driver.Linq;
using MongoDB.Bson;
using WebApi.OutputCache.V2;
using System.Threading.Tasks;
using System.Web.Http.Results;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    public class NotificationsController : ApiController
    {
        private MongoHelper<Notifications> _mongoHelper;

        private MongoHelper<UserActivityLog> _mongoActivityHelper;

        private QuestionsController _questionController;

        private AnswersController _answerController;

        private EmploymentHistoriesController _employmentHistoriesController;

        private EducationalHistoriesController _educationalHistoriesController;

        public NotificationsController()
        {
            _mongoHelper = new MongoHelper<Notifications>();
            _mongoActivityHelper = new MongoHelper<UserActivityLog>();

            _questionController = new QuestionsController();
            _answerController = new AnswersController();
            _employmentHistoriesController = new EmploymentHistoriesController();
            _educationalHistoriesController = new EducationalHistoriesController();
        }

        /// <summary>
        /// Get all the notifications for a particular user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [Authorize]
        [CacheOutput(ServerTimeSpan = 864000, ExcludeQueryStringFromCacheKey = true, NoCache = true)]
        public IHttpActionResult GetNotifications(string userId)
        {
            try
            {
                var result = (from e in _mongoHelper.Collection.AsQueryable<Notifications>()
                              where e.UserId == userId
                              orderby e.PostedOnUtc descending
                              select e).ToList();

                return Ok(result);
            }
            catch (MongoDB.Driver.MongoConnectionException ex)
            {
                return BadRequest(ex.Message);
            }
            catch(MongoDB.Driver.MongoQueryException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get all the new notifications for a user to show the bubble count in the navigation bar
        /// </summary>
        /// <param name="userId">userid to search for new notifications</param>
        /// <returns></returns>
        [Authorize]
        [CacheOutput(ServerTimeSpan = 864000, ExcludeQueryStringFromCacheKey = true, NoCache = true)]
        public IHttpActionResult GetNewNotifications(string userId)
        {
            try
            {
                var result = (from e in _mongoHelper.Collection.AsQueryable<Notifications>()
                              where e.UserId == userId && e.NewNotification == true
                              select e).ToList();

                return Ok(result);
            }
            catch(MongoDB.Driver.MongoConnectionException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (MongoDB.Driver.MongoQueryException ex)
            {
                return BadRequest(ex.Message);
            }
        }
       
        // POST: api/Notification
        /// <summary>
        /// Submit a new notification
        /// </summary>
        /// <param name="notification">Notification object</param>
        [Authorize]
        [ResponseType(typeof(Notifications))]
        public IHttpActionResult PostNotification(Notifications notification)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (notification == null)
                return BadRequest("Request body is null. Please send a valid Questions object");

            // add the datetime stamp for this notification
            notification.PostedOnUtc = DateTime.UtcNow;
            // make it as new notification
            notification.NewNotification = true;

            // save the notification to the database collection
            var result = _mongoHelper.Collection.Save(notification);

            // if mongo failed to save the data then send error
            if (!result.Ok)
                return InternalServerError();

            return CreatedAtRoute("DefaultApi", new { id = notification.NotificationId }, notification);
        }

        /// <summary>
        /// Mark all notifications for a user as Old. This api will remove the bubble notification that we get for all new notification in the navigation bar
        /// </summary>
        /// <param name="userId">userid</param>
        /// <returns></returns>
        [Authorize]
        public IHttpActionResult MarkAllNewNotificationsAsOld(string userId)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                if (string.IsNullOrEmpty(userId))
                    return BadRequest("Request body is null. Please send a valid email adress");

                // retrieve all the notification those are new
                var result = (from e in _mongoHelper.Collection.AsQueryable<Notifications>()
                              where e.UserId == userId && e.NewNotification == true
                              select e).ToList();

                // mark all these as old and save back to database
                foreach (var notification in result)
                {
                    notification.NewNotification = false;
                    _mongoHelper.Collection.Save(notification);
                }

                // invalidate the cache for the action those will get impacted due to this new notification
                var cache = Configuration.CacheOutputConfiguration().GetCacheOutputProvider(Request);

                // invalidate the getnotification cache for the user
                cache.RemoveStartsWith("notifications-getnotifications-userId=" + userId);
                cache.RemoveStartsWith("notifications-getnewnotifications-userId=" + userId);

                return Ok();
            }
            catch(MongoDB.Driver.MongoConnectionException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Mark a notification as visited. We use this method in the Notification pane when user click on a particular notification to mark that as visited
        /// </summary>
        /// <param name="notificationId">Notification Id</param>
        /// <returns></returns>
        [Authorize]
        public IHttpActionResult MarkNotificationsAsVisited(string notificationId)
        {
            if (string.IsNullOrEmpty(notificationId) || notificationId == "undefined")
            {
                return BadRequest(ModelState);
            }

            // retrieve the notification
            var result = (from e in _mongoHelper.Collection.AsQueryable<Notifications>()
                          where e.NotificationId == notificationId
                          select e).ToList();

            // mark this as visited
            result[0].HasVisited = true;
            _mongoHelper.Collection.Save(result[0]);

            // invalidate the cache for the action those will get impacted due to this new notification
            var cache = Configuration.CacheOutputConfiguration().GetCacheOutputProvider(Request);

            // invalidate the getnotification cache for the user
            cache.RemoveStartsWith("notifications-getnotifications-userId=" + result[0].UserId);

            return Ok();
        }

        /// <summary>
        /// Gets my feeds.
        /// </summary>
        /// <param name="myUserId">My user identifier.</param>
        /// <param name="page">The page.</param>
        /// <returns></returns>
        [CacheControl()]
        [CacheOutput(ServerTimeSpan = 120, ExcludeQueryStringFromCacheKey = true, NoCache = true)]
        public async Task<IHttpActionResult> GetMyFeeds(string myUserId, int page = 0)
        {
            try
            {
                if (string.IsNullOrEmpty(myUserId))
                {
                    return NotFound();
                }

                var allFeeds = _mongoActivityHelper.Collection.FindAll().OrderByDescending(a => a.HappenedAtUTC).ToList();

                var helper = new Helper.Helper();
                Task<CustomUserInfo> actionResult = helper.FindUserById(myUserId);
                var userDetail = await actionResult;

                if (userDetail == null)
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

                var feeds = feedsFollowedByMe.Skip(page * 10).Take(10).ToList();

                var userIds = feeds.Select(a => a.UserId).Distinct();
                var userDetails = new Dictionary<string, FeedUserDetail>();

                foreach (var userId in userIds)
                {
                    Task<CustomUserInfo> result = helper.FindUserById(userId);
                    var userDetailInList = await result;
                    var user = new FeedUserDetail
                    {
                        FullName = userDetailInList.FirstName + " " + userDetailInList.LastName,
                        ImageUrl = userDetailInList.ProfileImageURL
                    };
                    var careerTextResult = await GetDesignationText(userDetailInList.Email);
                    var careerText = careerTextResult as OkNegotiatedContentResult<string>;
                    user.CareerDetail = careerText.Content;

                    userDetails.Add(userId, user);
                }

                var feedResults = new List<PersonalizedFeedItem>();
                foreach (var feed in feeds)
                {
                    var feedItem = new PersonalizedFeedItem();

                    var feedContentResult = await GetFeedContent(feed.Activity, feed.ActedOnObjectId, feed.ActedOnUserId);
                    var feedContent = feedContentResult as OkNegotiatedContentResult<FeedContentDetail>;
                    feedItem.ItemHeader = feedContent.Content.Header;
                    feedItem.ItemDetail = feedContent.Content.SimpleDetail;

                    feedItem.ActionText = GetActionText(feed.Activity);

                    var matchedUser = userDetails[feed.UserId];
                    if (matchedUser != null)
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
            if (type == 1 || type == 2 || type == 3 || type == 4 || type == 5 || type == 8)
            {
                if (string.IsNullOrEmpty(objectId))
                {
                    return NotFound();
                }

                var result = await _questionController.GetQuestionInfo(objectId);
                var question = result as OkNegotiatedContentResult<Question>;
                feedContent.Header = question.Content.Title;

                feedContent.ViewCount = question.Content.ViewCount;
                feedContent.PostedDateInUTC = question.Content.PostedOnUtc;
                var answersCountResult = _questionController.GetAnswersCount(question.Content.QuestionId);
                var answersCount = answersCountResult as OkNegotiatedContentResult<int>;
                feedContent.AnswersCount = answersCount.Content;

                if (type == 2 || type == 3 || type == 4 || type == 5 || type == 8)
                {
                    var answerResult = await _answerController.GetAnswer(objectId);
                    var answer = result as OkNegotiatedContentResult<Answer>;
                    feedContent.SimpleDetail = answer.Content.AnswerText;
                }
                else
                {
                    feedContent.SimpleDetail = question.Content.Description;
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
            if (type == 1)
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

        private async Task<IHttpActionResult> GetDesignationText(string email)
        {
            var text = string.Empty;
            if (string.IsNullOrEmpty(email))
            {
                return NotFound();
            }

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
    }
}
