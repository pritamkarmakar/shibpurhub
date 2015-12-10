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
using WebApi.OutputCache.Core.Cache;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class FeedController : ApiController
    {
        private const int PAGESIZE = 10;

        private MongoHelper<UserActivityLog> _mongoHelper;

        private MongoHelper<Question> _mongoQustionHelper;
        private QuestionsController _questionController;
        private MongoHelper<Job> _mongoJobHelper;

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

            _mongoJobHelper = new MongoHelper<Job>();

            _employmentHistoriesController = new EmploymentHistoriesController();
            _educationalHistoriesController = new EducationalHistoriesController();
        }

        /// <summary>
        /// Gets my feeds.
        /// </summary>
        /// <param name="loggedInUserId">The logged in user identifier.</param>
        /// <param name="page">The page.</param>
        /// <param name="alreadyShown">The alrady shown.</param>
        /// <returns></returns>
        [CacheControl()]
        [CacheOutput(ServerTimeSpan = 864000, ExcludeQueryStringFromCacheKey = true, NoCache = true)]
        public async Task<IHttpActionResult> GetPersonalizedFeeds(string loggedInUserId, int page = 0, int alreadyShown = 0)
        {
            try
            {
                //ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
                //if (principal == null || principal.Identity == null || string.IsNullOrEmpty(principal.Identity.Name))
                //{
                //    return BadRequest("No UserId is found");
                //}

                //var email = principal.Identity.Name;

                if (string.IsNullOrEmpty(loggedInUserId))
                {
                    return BadRequest("No UserId is found");
                }

                var helper = new Helper.Helper();
                var userResult = helper.FindUserById(loggedInUserId);
                var userDetail = await userResult;
                if (userDetail == null)
                {
                    return BadRequest("No User is found");
                }

                var userId = userDetail.Id;

                var allFeeds = _mongoHelper.Collection.FindAll().OrderByDescending(a => a.HappenedAtUTC).Skip(alreadyShown).Take(100).ToList();


                //Task<CustomUserInfo> actionResult = helper.FindUserById(userId);
                //var userDetail = await actionResult;

                //if(userDetail == null)
                //{
                //return NotFound();
                //}

                var followedUsers = userDetail.Following ?? new List<string>();
                var followedQuestions = userDetail.FollowedQuestions ?? new List<string>();

                var allFeedsFollowedByMe = from x in allFeeds
                                           where followedUsers.Contains(x.UserId) ||
                                                 (followedQuestions.Contains(x.ActedOnObjectId) && x.UserId != userId)
                                                 || x.Activity == 10
                                           select x;

                var feedsFollowedByMe = allFeedsFollowedByMe.Skip(alreadyShown).Take(PAGESIZE * 2).ToList();

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

                var allfeedsWithContentResult = await GetAllFeedContents(lstLogsWithContent, userId);
                var z = allfeedsWithContentResult as OkNegotiatedContentResult<IList<FeedContentDetail>>;
                var allfeedsWithContent = z.Content ?? new List<FeedContentDetail>();

                var processedItemCount = page == 0 ? 0 : alreadyShown;
                for (var i = 0; feedResults.Count < PAGESIZE && i < feedsFollowedByMe.ToList().Count; i++)
                {
                    //foreach(var feed in feeds)
                    //{
                    processedItemCount += 1;

                    var feeds = feedsFollowedByMe.ToList();
                    var feed = feeds[i];

                    if (feed == null)
                    {
                        continue;
                    }

                    //Ignore Upvote, Update profile image
                    if (feed.Activity == 3 || feed.Activity == 9)
                    {
                        continue;
                    }

                    var feedItem = new PersonalizedFeedItem();
                    feedItem.UserId = feed.UserId;
                    if (!distinctUserId.Contains(feed.UserId))
                    {
                        distinctUserId.Add(feed.UserId);
                    }

                    FeedContentDetail feedContent = null;

                    if (feed.Activity == 6 || feed.Activity == 7)
                    {
                        var feedContentResult = await GetFeedContent(feed.Activity, feed.UserId, feed.ActedOnObjectId, feed.ActedOnUserId);
                        var y = feedContentResult as OkNegotiatedContentResult<FeedContentDetail>;
                        if (y == null)
                        {
                            continue;
                        }

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

                    feedItem.FollowedByCount = feedContent == null ? 0 : feedContent.FollowedByCount;
                    feedItem.IsFollowedByme = feedContent == null ? false : feedContent.IsFollowedByme;
                    feedItem.UpvoteCount = feedContent == null ? 0 : feedContent.UpvoteCount;
                    feedItem.IsUpvotedByme = feedContent == null ? false : feedContent.IsUpvotedByme;

                    if (!string.IsNullOrEmpty(previousObjectId) && previousObjectId == feed.ActedOnObjectId)
                    {
                        if (feedItem.ChildItems == null)
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
                    Task<CustomUserInfo> result = helper.FindUserById(id, true);
                    var userDetailInList = await result;
                    var user = new FeedUserDetail
                    {
                        FullName = userDetailInList.FirstName + " " + userDetailInList.LastName,
                        ImageUrl = userDetailInList.ProfileImageURL
                    };
                    //var careerTextResult = await GetDesignationText(userDetailInList.Email);
                    //var careerText = careerTextResult as OkNegotiatedContentResult<string>;
                    //user.CareerDetail = careerText == null ? string.Empty : careerText.Content;

                    user.CareerDetail = userDetailInList.Designation + " " +
                        (string.IsNullOrEmpty(userDetailInList.EducationInfo) ? string.Empty : (
                        string.IsNullOrEmpty(userDetailInList.Designation) ? userDetailInList.EducationInfo :
                            "(" + userDetailInList.EducationInfo + ")")
                        );

                    userDetails.Add(id, user);
                }

                foreach (var feedResult in feedResults)
                {
                    var matchedUser = userDetails[feedResult.UserId];
                    if (matchedUser != null)
                    {
                        feedResult.UserName = matchedUser.FullName;
                        feedResult.UserDesignation = matchedUser.CareerDetail;
                        feedResult.UserProfileUrl = "/Account/Profile?userId=" + feedResult.UserId;
                        feedResult.UserProfileImageUrl = matchedUser.ImageUrl;
                    }

                    // TO-DO: Do the same for child items
                }

                //var orderedFeedResults = feedResults.OrderBy(a => a.ActivityType).ThenByDescending(b => b.PostedDateInUTC).ToList();

                var feedresultSet = new FeedReult
                {
                    AlreadyProcessedItemCount = processedItemCount,
                    FeedItems = feedResults
                };

                return Ok(feedresultSet);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message + System.Environment.NewLine + ex.StackTrace);
            }
        }

        private async Task<IHttpActionResult> GetAllFeedContents(IList<UserActivityLogWithContent> logs, string loggedInUserId)
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

                var jobIds = logs.Where(a => a.Activity == 10).Select(a => a.ActedOnObjectId).ToList();
                var jobs = from c in _mongoJobHelper.Collection.AsQueryable()
                           where jobIds.Contains(c.JobId)
                           select c;

                var feedContents = new List<FeedContentDetail>();
                foreach (var feed in logs)
                {
                    var feedContent = new FeedContentDetail();
                    feedContent.ActionName = GetActionName(feed.Activity);
                    feedContent.LogId = feed.ActivityLogId;

                    Answer answer = null;
                    Question question = null;
                    Job job = null;
                    var isFeedAnAnswer = (feed.Activity == 2 || feed.Activity == 4 || feed.Activity == 5); // Else its a Question

                    if (isFeedAnAnswer)
                    {
                        answer = answers.FirstOrDefault(a => a.AnswerId == feed.ActedOnObjectId);
                        if (answer != null)
                        {
                            question = questions.FirstOrDefault(b => b.QuestionId == answer.QuestionId);
                            if (answer.UpvotedByUserIds == null)
                            {
                                feedContent.UpvoteCount = 0;
                                feedContent.IsUpvotedByme = false;
                            }
                            else
                            {
                                feedContent.UpvoteCount = answer.UpvotedByUserIds.Count;
                                feedContent.IsUpvotedByme = answer.UpvotedByUserIds.Any(a => a == loggedInUserId);
                            }
                        }
                    }

                    if (feed.Activity == 1 || feed.Activity == 8)
                    {
                        question = questions.FirstOrDefault(b => b.QuestionId == feed.ActedOnObjectId);
                    }

                    if (feed.Activity == 10)
                    {
                        job = jobs.FirstOrDefault(b => b.JobId == feed.ActedOnObjectId);

                        feedContent.Header = (string.IsNullOrEmpty(job.JobCompany) ? "" : job.JobCompany + " requires ") 
                                             + "<span class='job-title'>" + job.JobTitle + "</span>" +
                                             (string.IsNullOrEmpty(job.JobCity) ? "" : " in " + job.JobCity) +
                                             (string.IsNullOrEmpty(job.JobCountry) ? "" : ", " + job.JobCountry);
                        feedContent.SimpleDetail = job.JobDescription;
                        feedContent.PostedDateInUTC = job.PostedOnUtc;
                        feedContent.ActionUrl = "/career/jobdetails?jobid=" + job.JobId;
                    }

                    if (question != null)
                    {
                        feedContent.Header = question.Title;
                        var questionUrl = "/discussion/" + question.UrlSlug ?? question.QuestionId;
                        feedContent.ActionUrl = isFeedAnAnswer ? "/discussion/" + question.QuestionId + "/" + feed.ActedOnObjectId : questionUrl;

                        feedContent.ViewCount = question.ViewCount;

                        var answersCountResult = _questionController.GetAnswersCount(question.QuestionId);
                        var answersCount = answersCountResult as OkNegotiatedContentResult<int>;
                        feedContent.AnswersCount = answersCount.Content;

                        feedContent.SimpleDetail = question.Description;
                        feedContent.PostedDateInUTC = question.PostedOnUtc;

                        if (question.Followers == null)
                        {
                            feedContent.FollowedByCount = 0;
                            feedContent.IsFollowedByme = false;
                        }
                        else
                        {
                            feedContent.FollowedByCount = question.Followers.Count;
                            feedContent.IsFollowedByme = question.Followers.Any(a => a == loggedInUserId);
                        }
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
            catch (Exception e)
            {
                return BadRequest(e.Message + System.Environment.NewLine + e.StackTrace);
            }
        }

        // 1: Ask question, 2: Answer, 3: Upvote, 4: Comment, 5: Mark as Answer, 
        // 6: Register as new user, 7: Follow an user, 8: Follow a question, 9: Update profile image
        private async Task<IHttpActionResult> GetFeedContent(int type, string userId, string objectId = "", string objectUserId = "")
        {
            var feedContent = new FeedContentDetail();

            try
            {
                if (type == 6 || type == 7)
                {
                    if (string.IsNullOrEmpty(objectUserId))
                    {
                        return NotFound();
                    }

                    var helper = new Helper.Helper();
                    Task<CustomUserInfo> actionResult = helper.FindUserById(objectUserId);
                    var userDetail = await actionResult;

                    feedContent.Header = userDetail.FirstName + " " + userDetail.LastName;
                    feedContent.ActionName = userDetail.FirstName + " " + userDetail.LastName;
                    feedContent.ActionUrl = "/Account/Profile?userId=" + userDetail.Id;

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

            if (type == 10)
            {
                return " posted a new ";
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

            if (type == 10)
            {
                return "Job";
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
            catch (Exception ex)
            {
                return NotFound();
            }
        }

        public async Task<IHttpActionResult> GetPersonalizedQAStatus(string userId, [FromUri] IList<string> questionIds = null, [FromUri] IList<string> answerIds = null)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("No UserId is found");
            }

            try
            {
                var statuses = new List<PersonalizedQAStatus>();

                if (questionIds != null && questionIds.Count > 0)
                {
                    var questions = from q in _mongoQustionHelper.Collection.AsQueryable<Question>().ToList()
                                    where questionIds.Contains(q.QuestionId)
                                    select q;
                    foreach(var question in questions.ToList())
                    {
                        var qstatus = new PersonalizedQAStatus(question.QuestionId, true);
                        qstatus.IsAskedByMe = question.UserId == userId;
                        qstatus.IsFollowedByMe = question.Followers != null && question.Followers.Contains(userId);

                        statuses.Add(qstatus);
                    }
                }

                if (answerIds != null && answerIds.Count > 0)
                {
                    var answers = from a in _mongoAnswerHelper.Collection.AsQueryable<Answer>().ToList()
                                  where answerIds.Contains(a.AnswerId)
                                  select a;

                    foreach (var answer in answers.ToList())
                    {
                        var astatus = new PersonalizedQAStatus(answer.AnswerId, false);
                        astatus.IsAnsweredByMe = answer.UserId == userId;
                        astatus.IsUpvotedByMe = answer.UpvotedByUserIds != null && answer.UpvotedByUserIds.Contains(userId);

                        statuses.Add(astatus);
                    }
                }

                return Ok<IList<PersonalizedQAStatus>>(statuses);
            }
            catch (Exception ex)
            {
                return NotFound();
            }
        }
    }

    public class FeedReult
    {
        public int AlreadyProcessedItemCount { get; set; }

        public IList<PersonalizedFeedItem> FeedItems { get; set; }
    }

    /// <summary>
    /// Question and Answer status of an user
    /// </summary>
    public class PersonalizedQAStatus
    {
        public PersonalizedQAStatus(string id, bool isQuestion)
        {
            this.Id = id;
            this.IsQuestion = isQuestion;
        }

        public string Id { get; set; }

        public bool IsFollowedByMe { get; set; }

        public bool IsAskedByMe { get; set; }

        public bool IsAnsweredByMe { get; set; }

        public bool IsUpvotedByMe { get; set; }

        public bool IsQuestion { get; set; }
    }
}