using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Results;
using MongoDB.Driver.Linq;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models.WebAPI;
using System.Threading.Tasks;
using WebApi.OutputCache.V2;
using System.Security.Claims;
using System.Net.Http;

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
        [Authorize]
        public async Task<IHttpActionResult> GetPersonalizedFeeds(int page = 0, int alreadyShown = 0)
        {
            try
            {
                ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
                var email = principal.Identity.Name;

                Helper.Helper helper = new Helper.Helper();
                var userResult = helper.FindUserByEmail(email, true);
                var userDetail = await userResult;
                if (userDetail == null)
                {
                    return BadRequest("No UserId is found");
                }

                var userId = userDetail.Id;
                // taking activities that hasn't been processed, but all these activities are not applicable to this user
                var allFeeds = _mongoHelper.Collection.FindAll().OrderByDescending(a => a.HappenedAtUTC).Skip(alreadyShown).Take(100).ToList();

                // users that current user following
                var followedUsers = userDetail.Following ?? new List<string>();
                // questions that current user following
                var followedQuestions = userDetail.FollowedQuestions ?? new List<string>();
                List<UserActivityLog> allapplicablefeeds;

                // get user BEC education year
                string becGraduateYear = string.Empty;
                if (userDetail.EducationalHistories != null)
                {
                    var becEducation = userDetail.EducationalHistories.FindLast(m => m.IsBECEducation == true);
                    if (becEducation != null)
                        becGraduateYear = becEducation.GraduateYear.ToString();
                }

                // also if user if doing a page refresh or making first time call to the feed api then make db query to get new content if there are any
                if (alreadyShown == 0)
                {
                    allapplicablefeeds = (from m in _mongoHelper.Collection.FindAll()
                                          where (m.Activity == 1 && followedUsers.Contains(m.UserId)) ||
                                           (m.Activity == 2 && followedUsers.Contains(m.UserId)) ||
                                           (m.Activity == 4 && followedUsers.Contains(m.UserId)) ||
                                           (m.Activity == 6 && m.UserId != userDetail.Id) ||
                                              // when one user will follow another user, we have to make sure the user who is getting followed is not receiving this notification in his feed
                                           (m.Activity == 7 && followedUsers.Contains(m.UserId) && m.ActedOnUserId != userDetail.Id) ||
                                           (m.Activity == 8 && followedUsers.Contains(m.UserId)) ||
                                           (m.Activity == 10 && m.UserId != userDetail.Id)
                                          orderby m.HappenedAtUTC descending
                                          select m).ToList();

                    // save the activity log for this user in in-memory cache
                    CacheManager.SetCacheData("feed-" + userDetail.Id, allapplicablefeeds);
                }
                else
                {
                    // get the list of activities from in-memory
                    allapplicablefeeds = (List<UserActivityLog>)CacheManager.GetCachedData("feed-" + userDetail.Id);

                    // if for some reason in-memory cache is missing then get the list from database
                    if (allapplicablefeeds == null)
                    {
                        allapplicablefeeds = (from m in _mongoHelper.Collection.FindAll()
                                              where (m.Activity == 1 && followedUsers.Contains(m.UserId)) ||
                                               (m.Activity == 2 && followedUsers.Contains(m.UserId)) ||
                                               (m.Activity == 4 && followedUsers.Contains(m.UserId)) ||
                                               (m.Activity == 6 && m.UserId != userDetail.Id) ||
                                                  // when one user will follow another user, we have to make sure the user who is getting followed is not receiving this notification in his feed
                                               (m.Activity == 7 && followedUsers.Contains(m.UserId) && m.ActedOnUserId != userDetail.Id) ||
                                               (m.Activity == 8 && followedUsers.Contains(m.UserId)) ||
                                               (m.Activity == 10 && m.UserId != userDetail.Id)
                                              orderby m.HappenedAtUTC descending
                                              select m).ToList();
                    }

                }


                var feedsFollowedByMe = allapplicablefeeds.Skip(alreadyShown).Take(PAGESIZE * 2).ToList();


                var feedResults = new List<PersonalizedFeedItem>();
                //string previousObjectId = string.Empty;
                var distinctUserId = new List<string>();

                var lstLogsWithContent = new List<UserActivityLogWithContent>();
                foreach (var feedFollowedByMe in feedsFollowedByMe)
                {
                    var logWithContent = new UserActivityLogWithContent(feedFollowedByMe);
                    lstLogsWithContent.Add(logWithContent);
                }

                var allfeedsWithContentResult = await GetAllFeedContents(lstLogsWithContent, userId);
                var z = allfeedsWithContentResult as OkNegotiatedContentResult<IList<PersonalizedFeedItem>>;
                var allfeedsWithContent = z.Content ?? new List<PersonalizedFeedItem>();

                var processedItemCount = page == 0 ? 0 : alreadyShown;
                for (var i = 0; feedResults.Count < PAGESIZE && i < feedsFollowedByMe.ToList().Count; i++)
                {
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

                    PersonalizedFeedItem feedContent = null;
                    if (feed.Activity == 6 || feed.Activity == 7)
                    {
                        var feedContentResult = await GetFeedContent(feed.Activity, feed.UserId, becGraduateYear, feed.ActedOnObjectId, feed.ActedOnUserId);
                        var y = feedContentResult as OkNegotiatedContentResult<PersonalizedFeedItem>;
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

                    if (feedContent == null)
                    {
                        continue;
                    }

                    processedItemCount += 1;

                    feedContent.ActivityType = feed.Activity;
                    feedContent.ActionText = GetActionText(feed.Activity);
                    feedContent.UserId = feed.UserId;
                    if (!distinctUserId.Contains(feed.UserId))
                    {
                        distinctUserId.Add(feed.UserId);
                    }

                    feedResults.Add(feedContent);
                }

                //var userIds = feeds.Select(a => a.UserId).Distinct();
                var userDetails = new Dictionary<string, FeedUserDetail>();

                foreach (var id in distinctUserId)
                {
                    // check in-memory
                    CustomUserInfo userDetailInList = (CustomUserInfo)CacheManager.GetCachedData("completeuserprofile-" + id);

                    if (userDetailInList == null)
                    {
                        Task<CustomUserInfo> result = helper.FindUserById(id, true);
                        userDetailInList = await result;

                        if (userDetailInList == null)
                            continue;

                        // set the profile in in-memory
                        CacheManager.SetCacheData("completeuserprofile-" + id, userDetailInList);

                    }

                    var user = new FeedUserDetail
                    {
                        FullName = userDetailInList.FirstName + " " + userDetailInList.LastName,
                        ImageUrl = userDetailInList.ProfileImageURL
                    };

                    user.CareerDetail = userDetailInList.Designation + " " +
                        (string.IsNullOrEmpty(userDetailInList.EducationInfo) ? string.Empty : (
                        string.IsNullOrEmpty(userDetailInList.Designation) ? userDetailInList.EducationInfo :
                            "(" + userDetailInList.EducationInfo + ")")
                        );

                    userDetails.Add(id, user);
                }

                foreach (var feedResult in feedResults)
                {
                    if (userDetails.ContainsKey(feedResult.UserId))
                    {
                        var matchedUser = userDetails[feedResult.UserId];
                        if (matchedUser != null)
                        {
                            feedResult.UserName = matchedUser.FullName;
                            if (feedResult.ActivityType != 10)
                            {
                                feedResult.ItemSubHeader = matchedUser.CareerDetail;
                            }

                            feedResult.UserProfileUrl = "/Account/Profile?userId=" + feedResult.UserId;
                            feedResult.UserProfileImageUrl = matchedUser.ImageUrl;
                        }
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

        // Method to retrieve feed content for following activity types
        // 1: Ask question, 2: Answer, 3: Upvote, 4: Comment, 5: Mark as Answer, 
        // 8: Follow a question
        private async Task<IHttpActionResult> GetAllFeedContents(IList<UserActivityLogWithContent> logs, string loggedInUserId)
        {
            try
            {
                var answerIds = logs.Where(a => a.Activity == 2 || a.Activity == 4 || a.Activity == 5).Select(a => a.ActedOnObjectId).ToList();
                // create the answer list which are in the user activity log for this user
                List<Answer> _answers = new List<Answer>();

                foreach (string answerid in answerIds)
                {
                    // check if in-memory cache has this answer or retrive from db and save it
                    Answer answerobj = (Answer)CacheManager.GetCachedData(answerid);
                    if (answerobj == null)
                    {
                        answerobj = _mongoAnswerHelper.Collection.AsQueryable().Where(m => m.AnswerId == answerid).FirstOrDefault();
                        if (answerobj != null)
                        {
                            CacheManager.SetCacheData(answerid, answerobj);
                        }
                    }
                    // save the obj in the list if it not null
                    if (answerobj != null)
                        _answers.Add(answerobj);
                }

                // create the question list which are in the user activity log for this user
                var questionIds = logs.Where(a => a.Activity == 1 || a.Activity == 8).Select(a => a.ActedOnObjectId).ToList();
                var questionIdsFromAnswers = from o in _answers.ToList()
                                             select o.QuestionId;
                questionIds.AddRange(questionIdsFromAnswers.ToList());

                List<Question> questions = new List<Question>();

                foreach (string questionid in questionIds)
                {
                    // check if in-memory cache has this question or retrive from db and save it
                    Question questionobj = (Question)CacheManager.GetCachedData(questionid);
                    if (questionobj == null)
                    {
                        questionobj = _mongoQustionHelper.Collection.AsQueryable().Where(m => m.QuestionId == questionid).FirstOrDefault();
                        if (questionobj != null)
                        {
                            CacheManager.SetCacheData(questionid, questionobj);

                        }
                    }
                    // save the obj in the list if it not null
                    if (questionobj != null)
                        questions.Add(questionobj);
                }

                // create the job list which are in the user activity log for this user
                var jobIds = logs.Where(a => a.Activity == 10).Select(a => a.ActedOnObjectId).ToList();
                List<Job> jobs = new List<Job>();

                foreach (string jobid in jobIds)
                {
                    // check if in-memory cache has this job or retrive from db and save it
                    Job jobobj = (Job)CacheManager.GetCachedData(jobid);
                    if (jobobj == null)
                    {
                        jobobj = _mongoJobHelper.Collection.AsQueryable().Where(m => m.JobId == jobid).FirstOrDefault();
                        if (jobobj != null)
                            CacheManager.SetCacheData(jobid, jobobj);
                    }

                    // save the obj in the list if it is not null
                    if (jobobj != null)
                        jobs.Add(jobobj);

                }

                var feedContents = new List<PersonalizedFeedItem>();
                var questionIdsForAnswerFeeds = new List<string>();
                foreach (var feed in logs)
                {
                    var feedContent = new PersonalizedFeedItem();
                    feedContent.TargetAction = GetActionName(feed.Activity);
                    feedContent.LogId = feed.ActivityLogId;

                    Answer answer = null;
                    Question question = null;
                    Job job = null;
                    var isFeedAnAnswer = (feed.Activity == 2 || feed.Activity == 4 || feed.Activity == 5); // Else its a Question

                    if (isFeedAnAnswer)
                    {
                        answer = _answers.FirstOrDefault(a => a.AnswerId == feed.ActedOnObjectId);

                        if (answer != null)
                        {
                            if (questionIdsForAnswerFeeds.Contains(answer.QuestionId))
                            {
                                continue;
                            }

                            questionIdsForAnswerFeeds.Add(answer.QuestionId);
                            question = questions.FirstOrDefault(b => b.QuestionId == answer.QuestionId);
                            // if the question has been deleted then no need process this
                            if (question == null)
                                continue;

                            feedContent.QuestionId = question.QuestionId;
                            feedContent.AnswerId = answer.AnswerId;

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

                        if (job == null)
                            continue;

                        feedContent.ItemHeader = job.JobTitle;
                        feedContent.ItemSubHeader = (string.IsNullOrEmpty(job.JobCompany) ? "" : job.JobCompany + " | ")
                                                + (string.IsNullOrEmpty(job.JobCity) ? "" : job.JobCity) +
                                             (string.IsNullOrEmpty(job.JobCountry) ? "" : ", " + job.JobCountry);
                        feedContent.ItemDetail = job.JobDescription;
                        feedContent.PostedDateInUTC = job.PostedOnUtc;
                        feedContent.TargetActionUrl = "/career/jobdetails?jobid=" + job.JobId;
                    }

                    if (question != null)
                    {
                        feedContent.ItemHeader = question.Title;
                        var questionUrl = "/discussion/" + question.UrlSlug ?? question.QuestionId;
                        feedContent.TargetActionUrl = isFeedAnAnswer ? "/discussion/" + question.QuestionId + "/" + feed.ActedOnObjectId : questionUrl;

                        feedContent.ViewCount = question.ViewCount;

                        var answersCountResult = _questionController.GetAnswersCount(question.QuestionId);
                        var answersCount = answersCountResult as OkNegotiatedContentResult<int>;
                        feedContent.AnswersCount = answersCount.Content;

                        feedContent.ItemDetail = question.Description;
                        feedContent.PostedDateInUTC = feed.HappenedAtUTC;
                        feedContent.QuestionId = question.QuestionId;

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
                        feedContent.ItemDetail = answer.AnswerText;
                        feedContent.PostedDateInUTC = answer.PostedOnUtc;
                    }

                    feedContents.Add(feedContent);
                }

                return Ok<IList<PersonalizedFeedItem>>(feedContents);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message + System.Environment.NewLine + e.StackTrace);
            }
        }

        // Method to retrieve feed content for following activity types
        // 6: Register as new user, 7: Follow an user
        private async Task<IHttpActionResult> GetFeedContent(int type, string userId, string currentUserBECGraduateYear, string objectId = "", string objectUserId = "")
        {
            var feedContent = new PersonalizedFeedItem();
            CustomUserInfo userDetail = new CustomUserInfo();

            try
            {
                if (type == 6 || type == 7)
                {
                    if (string.IsNullOrEmpty(userId))
                    {
                        return NotFound();
                    }

                    var helper = new Helper.Helper();
                    Task<CustomUserInfo> actionResult = null;
                    if (type == 7)
                    {
                        // check if we have the userinfo in in-memory cache
                        userDetail = (CustomUserInfo)CacheManager.GetCachedData(userId);
                        if (userDetail == null)
                        {
                            actionResult = helper.FindUserById(objectUserId);
                            userDetail = await actionResult;

                            if (userDetail == null)
                                return NotFound();

                            // set the profile in in-memory
                            CacheManager.SetCacheData(userId, userDetail);
                        }

                        feedContent.ItemHeader = userDetail.FirstName + " " + userDetail.LastName;
                        feedContent.TargetAction = userDetail.FirstName + " " + userDetail.LastName;
                        feedContent.TargetActionUrl = "/Account/Profile?userId=" + userDetail.Id;
                        return Ok(feedContent);
                    }
                    else if (type == 6)
                    {
                        if (string.IsNullOrEmpty(currentUserBECGraduateYear))
                            return NotFound();

                        // for new user registration we want to make sure new user and current user is from same batch. 
                        // Current plan is to show the feed content only if new user and current user from same batch

                        // check in-memory first
                        userDetail = (CustomUserInfo)CacheManager.GetCachedData("completeuserprofile-" + userId);
                        if (userDetail == null)
                        {
                            actionResult = helper.FindUserById(userId, true);
                            userDetail = await actionResult;

                            if (userDetail == null)
                                return NotFound();

                            // set the profile in in-memory
                            CacheManager.SetCacheData("completeuserprofile-" + userId, userDetail);
                        }

                        // get user BEC education year
                        string becGraduateYear = string.Empty;
                        if (userDetail.EducationalHistories != null)
                        {
                            var becEducation = userDetail.EducationalHistories.FindLast(m => m.IsBECEducation == true);
                            if (becEducation == null)
                                return NotFound();
                            else
                                becGraduateYear = becEducation.GraduateYear.ToString();
                        }
                        else
                            return NotFound();                        

                        // match the graduate year
                        if (becGraduateYear.Trim() == currentUserBECGraduateYear.Trim())
                        {
                            feedContent.ItemHeader = userDetail.FirstName + " " + userDetail.LastName;
                            feedContent.TargetAction = userDetail.FirstName + " " + userDetail.LastName;
                            feedContent.TargetActionUrl = "/Account/Profile?userId=" + userDetail.Id;
                            return Ok(feedContent);
                        }
                        else
                            return NotFound();
                    }
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
                return " joined ShibpurHub";
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
                    foreach (var question in questions.ToList())
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
                        astatus.MarkedAsAnswer = answer.MarkedAsAnswer;

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

        // API to retrieve user specific information that we use while forming individual activity feed content
        public async Task<IHttpActionResult> GetFollowedUserDetails([FromUri] IList<string> userIds)
        {
            if (userIds == null || userIds.Count == 0)
            {
                return Ok();
            }

            var helper = new Helper.Helper();
            var feedUserDetails = new List<FeedUserDetail>();
            foreach (var userid in userIds)
            {
                CustomUserInfo userDetailInList = (CustomUserInfo)CacheManager.GetCachedData("completeuserprofile-" + userid);
                if (userDetailInList == null)
                {
                    Task<CustomUserInfo> result = helper.FindUserById(userid, true);
                    userDetailInList = await result;

                    // set the profile in in-memory
                    CacheManager.SetCacheData("completeuserprofile-" + userid, userDetailInList);
                }

                var user = new FeedUserDetail
                {
                    UserId = userid,
                    FullName = userDetailInList.FirstName + " " + userDetailInList.LastName,
                    ImageUrl = userDetailInList.ProfileImageURL,
                    Reputation = userDetailInList.ReputationCount
                };

                user.CareerDetail = userDetailInList.Designation + " " +
                    (string.IsNullOrEmpty(userDetailInList.EducationInfo) ? string.Empty : (
                    string.IsNullOrEmpty(userDetailInList.Designation) ? userDetailInList.EducationInfo :
                        "(" + userDetailInList.EducationInfo + ")")
                    );

                user.QuestionCount = _questionController.GetUserQuestionCount(userid);
                user.AnswerCount = _answerController.GetUserAnswerCount(userid);

                feedUserDetails.Add(user);
            }

            return Ok(feedUserDetails);
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