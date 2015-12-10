using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;
using System.Web.Http.Results;
using Hangfire;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models.WebAPI;
using WebApi.OutputCache.V2;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class CareerController : ApiController
    {
        private MongoHelper<Job> _mongoHelper;
        private string jobPageSize = ConfigurationManager.AppSettings["jobPageSize"];
        private int maxSkillSetLength = Convert.ToInt16(ConfigurationManager.AppSettings["maxskillsetlength"]);
        private static string hostName = string.Empty;

        public CareerController()
        {
            _mongoHelper = new MongoHelper<Job>();
        }

        /// <summary>
        /// API to get available jobs
        /// </summary>
        /// <param name="page">page number to get the jobs</param>
        /// <returns></returns>
        [CacheOutput(ServerTimeSpan = 864000, ExcludeQueryStringFromCacheKey = true, NoCache = true)]
        public async Task<IHttpActionResult> GetJobs(int page = 0)
        {
            int pageSize = Convert.ToInt16(jobPageSize);

            try
            {
                var joblist =
                    _mongoHelper.Collection.FindAll()
                        .OrderByDescending(a => a.PostedOnUtc)
                        .Skip(page * pageSize)
                        .Take(pageSize)
                        .ToList();

                // retrieve all the user information who posted these jobs
                Helper.Helper helper = new Helper.Helper();
                var userIds = joblist.Select(a => a.UserId).Distinct();
                var userDetails = new Dictionary<string, CustomUserInfo>();

                foreach (var userId in userIds)
                {
                    Task<CustomUserInfo> actionResult = helper.FindUserById(userId, true);
                    var userDetail = await actionResult;
                    userDetails.Add(userId, userDetail);
                }

                var result = new List<JobViewModel>();
                foreach (var job in joblist)
                {
                    var jobDTO = new JobViewModel();
                    jobDTO.Followers = job.Followers;
                    jobDTO.UserId = job.UserId;
                    jobDTO.HasClosed = job.HasClosed;
                    jobDTO.JobDescription = job.JobDescription;
                    jobDTO.JobId = job.JobId;
                    jobDTO.JobTitle = job.JobTitle;
                    jobDTO.JobCompany = job.JobCompany;
                    jobDTO.JobCity = job.JobCity;
                    jobDTO.JobCountry = job.JobCountry;
                    jobDTO.LastEditedOnUtc = job.LastEditedOnUtc;
                    jobDTO.PostedOnUtc = job.PostedOnUtc;
                    jobDTO.SkillSets = job.SkillSets;
                    jobDTO.SpamCount = job.SpamCount;
                    jobDTO.ViewCount = job.ViewCount;
                    jobDTO.DisplayName = userDetails[job.UserId].FirstName + " " + userDetails[job.UserId].LastName;
                    jobDTO.UserProfileImage = userDetails[job.UserId].ProfileImageURL;
                    jobDTO.CareerDetail = userDetails[job.UserId].Designation;
                    result.Add(jobDTO);
                }

                return Ok(result);
            }

            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// API to get details about a specific jobId
        /// Note: we are not caching this API as we don't want to restrict the 'JobApplications' collection. 
        /// This will be different for each user
        /// </summary>
        /// <param name="jobId">jobId to retrieve the details</param>
        /// <returns></returns>
        public async Task<IHttpActionResult> GetJob(string jobId)
        {
            try
            {
                // dictionary to keep a userinfo
                Dictionary<string, CustomUserInfo> userInfoDictionary = new Dictionary<string, CustomUserInfo>();

                var jobDetails = _mongoHelper.Collection.AsQueryable().FirstOrDefault(m => m.JobId == jobId);
                if (jobDetails == null)
                {
                    return NotFound();
                }

                Helper.Helper helper = new Helper.Helper();
                Task<CustomUserInfo> actionResult = helper.FindUserById(jobDetails.UserId, true);
                var userDetails = await actionResult;

                // add this user in the dictionary
                userInfoDictionary.Add(userDetails.Id, userDetails);

                // retrieve the job applications associated with this job, if current user is the job poster then retrieve all  
                // otherwise send only the user application if there is any
                List<JobApplication> applications = new List<JobApplication>();
                List<JobApplicationViewModel> javm = new List<JobApplicationViewModel>();
                ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
                if (principal != null)
                {
                    var email = principal.Identity.Name;
                    if (email != null)
                    {
                        var userResult = helper.FindUserByEmail(email, true);
                        var userInfo = await userResult;

                        // add this user in the userinfo dictionary, if it is not present
                        if (!userInfoDictionary.ContainsKey(userInfo.Id))
                            userInfoDictionary.Add(userInfo.Id, userInfo);

                        var mongoHelper = new MongoHelper<JobApplication>();
                        if (userInfo != null)
                        {
                            // retrieve all the applcations as this is the user who posted the job
                            if (userInfo.Id == userDetails.Id)
                            {
                                applications =
                                    mongoHelper.Collection.AsQueryable()
                                        .Where(a => a.JobId == jobId)
                                        .OrderBy(a => a.PostedOnUtc)
                                        .ToList();
                            }
                            else
                            {
                                // retrieve applications that this user posted
                                applications =
                                    mongoHelper.Collection.AsQueryable()
                                        .Where(a => a.JobId == jobId && a.UserId == userInfo.Id)
                                        .OrderBy(a => a.PostedOnUtc)
                                        .ToList();
                            }

                            // add displayName property to the job applications
                            foreach (var application in applications)
                            {
                                // retrieve the user info for this application, if that is not present in the dictionary
                                CustomUserInfo userInfo2;
                                if (!userInfoDictionary.ContainsKey(application.UserId))
                                {
                                    var userResult2 = helper.FindUserById(application.UserId, true);
                                    userInfo2 = await userResult2;
                                    
                                    userInfoDictionary.Add(userInfo2.Id, userInfo2);
                                }
                                else
                                {
                                    userInfo2 = userInfoDictionary[application.UserId];
                                }

                                // retrieve the associated comments with this application
                                var mongoHelper2 = new MongoHelper<JobApplicationComment>();
                                var comments = mongoHelper2.Collection.AsQueryable()
                                        .Where(a => a.ApplicationId == application.ApplicationId)
                                        .OrderBy(a => a.PostedOnUtc)
                                        .ToList();

                                // create the JobApplicationCommentViewModel
                                List<JobApplicationCommentViewModel> jacvm = new List<JobApplicationCommentViewModel>();
                                foreach (var comment in comments)
                                {
                                    // retrieve the userinfo
                                    CustomUserInfo userInfo3;
                                    if (!userInfoDictionary.ContainsKey(comment.UserId))
                                    {
                                        var userResult3 = helper.FindUserById(comment.UserId);
                                        userInfo3 = await userResult3;

                                        userInfoDictionary.Add(userInfo3.Id, userInfo3);
                                    }
                                    else
                                    {
                                        userInfo3 = userInfoDictionary[comment.UserId];
                                    }

                                    jacvm.Add(new JobApplicationCommentViewModel()
                                    {
                                        UserId = userInfo3.Id,
                                        PostedOnUtc = comment.PostedOnUtc,
                                        ApplicationId = comment.ApplicationId,
                                        DisplayName = userInfo3.FirstName + " " + userInfo3.LastName,
                                        UserProfileImage = userInfo3.ProfileImageURL,
                                        CommentId = comment.CommentId,
                                        CommentText = comment.CommentText
                                    });
                                }

                                javm.Add(new JobApplicationViewModel()
                                {
                                    JobId = application.JobId,
                                    UserId = application.UserId,
                                    CareerDetail = userInfo2.Designation,
                                    PostedOnUtc = application.PostedOnUtc,
                                    DisplayName = userInfo2.FirstName + " " + userInfo2.LastName,
                                    UserProfileImage = userInfo2.ProfileImageURL,
                                    CoverLetter = application.CoverLetter,
                                    ApplicationId = application.ApplicationId,
                                    ApplicationComments = jacvm
                                });
                            }
                        }
                    }
                }

                var jvm = new JobViewModel()
                        {
                            UserId = jobDetails.UserId,
                            Followers = jobDetails.Followers,
                            JobId = jobDetails.JobId,
                            JobDescription = jobDetails.JobDescription,
                            JobTitle = jobDetails.JobTitle,
                            JobApplications = javm,
                            JobCompany = jobDetails.JobCompany,
                            JobCity = jobDetails.JobCity,
                            JobCountry = jobDetails.JobCountry,
                            SkillSets = jobDetails.SkillSets,
                            ViewCount = jobDetails.ViewCount,
                            LastEditedOnUtc = jobDetails.LastEditedOnUtc,
                            HasClosed = jobDetails.HasClosed,
                            PostedOnUtc = jobDetails.PostedOnUtc,
                            SpamCount = jobDetails.SpamCount,
                            DisplayName = userDetails.FirstName + " " + userDetails.LastName,
                            UserProfileImage = userDetails.ProfileImageURL,
                            CareerDetail = userDetails.Designation
                        };


                return Ok(jvm);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// API to post a new job
        /// </summary>
        /// <param name="jobDto"></param>
        /// <returns></returns>
        [Authorize]
        [ResponseType(typeof(Job))]
        [InvalidateCacheOutput("GetJobs")]
        public async Task<IHttpActionResult> PostJob(JobDTO jobDto)
        {
            // validate title
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (jobDto == null)
                return BadRequest("Request body is null. Please send a valid QuestionDTO object");

            // validate if incase any skillset is just space, contains invalid characters
            foreach (string cat in jobDto.SkillSets)
            {
                if (string.IsNullOrWhiteSpace(cat))
                {
                    ModelState.AddModelError("", cat + " skillset is invalid. It contains empty string or contains only whitespace");
                    return BadRequest(ModelState);
                }
                if (cat.Length > maxSkillSetLength)
                {
                    ModelState.AddModelError("", cat + " skillset is invalid, it is too long. Max " + maxSkillSetLength + " characters allowed per tag");
                    return BadRequest(ModelState);
                }
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

            // new question object that we will save in the database
            Job jobToPost = new Job()
            {
                JobTitle = jobDto.JobTitle,
                JobCompany = jobDto.JobCompany,
                JobCity = jobDto.JobCity,
                JobCountry = jobDto.JobCountry,
                UserId = userInfo.Id,
                JobDescription = jobDto.JobDescription,
                SkillSets = jobDto.SkillSets.Select(c => c.Trim()).ToArray(),
                JobId = ObjectId.GenerateNewId().ToString(),
                PostedOnUtc = DateTime.UtcNow
            };


            // create the new categories if those don't exist            
            List<SkillSets> categoryList = new List<SkillSets>();
            SkillSetsController skillSetsController = new SkillSetsController();
            foreach (string skillset in jobDto.SkillSets)
            {
                var actionResult = await skillSetsController.GetSkillSet(skillset.Trim());
                var contentResult = actionResult as OkNegotiatedContentResult<SkillSets>;
                if (contentResult == null)
                {
                    SkillSets sset = new SkillSets()
                    {
                        SkillSetId = ObjectId.GenerateNewId().ToString(),
                        SkillSetName = skillset.Trim().ToLower()
                    };
                    var actionResult2 = await helper.PostSkillSet(sset);
                }
            }

            //save the question to the database
            var result = _mongoHelper.Collection.Save(jobToPost);

            var userActivityLog = new UserActivityLog
            {
                Activity = 10,
                UserId = userInfo.Id,
                ActedOnObjectId = jobToPost.JobId,
                ActedOnUserId = string.Empty
            };

            UpdateUserActivityLog(userActivityLog);

            // if mongo failed to save the data then send error
            if (!result.Ok)
                return InternalServerError();

            return CreatedAtRoute("DefaultApi", new { id = jobToPost.JobId }, jobToPost);
        }

        /// <summary>
        /// Close a job
        /// </summary>
        /// <param name="jobId">jobid to close</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [InvalidateCacheOutput("GetJobs")]
        public async Task<IHttpActionResult> CloseJob(string jobId)
        {
            // retrieve the job details
            var jobDetails = _mongoHelper.Collection.AsQueryable().FirstOrDefault(m => m.JobId == jobId);
            if (jobDetails == null)
            {
                return BadRequest("Invalid job id");
            }

            // check the user who posted the job is actually requested to close
            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
            var email = principal.Identity.Name;

            Helper.Helper helper = new Helper.Helper();
            var userResult = helper.FindUserByEmail(email);
            var userInfo = await userResult;
            if (userInfo == null)
            {
                return BadRequest("No UserId is found");
            }

            if (userInfo.Id != jobDetails.UserId)
                return BadRequest("You can't close this job as you are not the job poster");

            // set job status to close
            jobDetails.HasClosed = true;
            var result = _mongoHelper.Collection.Save(jobDetails);

            // if mongo failed to save the data then send error
            if (!result.Ok)
                return InternalServerError();

            return CreatedAtRoute("DefaultApi", new { id = jobDetails.JobId }, jobDetails);
        }

        /// <summary>
        /// Get the total application count for a specific job
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        [HttpGet]
        [CacheOutput(ServerTimeSpan = 864000, ExcludeQueryStringFromCacheKey = true, NoCache = true)]
        public async Task<IHttpActionResult> GetJobApplicationCount(string jobId)
        {
            var mongoHelper = new MongoHelper<JobApplication>();
            try
            {
                var jobApplicationCount = mongoHelper.Collection.AsQueryable()
                              .Where(a => a.JobId == jobId).ToList().Count;

                return CreatedAtRoute("DefaultApi", new { id = jobId }, jobApplicationCount);
            }
            catch (MongoConnectionException ex)
            {
                return BadRequest(ex.Message);
            }
        }

            /// <summary>
        /// API to post a job application
        /// </summary>
        /// <param name="jobApplicationDto">JobApplicationDTO object</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public async Task<IHttpActionResult> ApplyForAJob(JobApplicationDTO jobApplicationDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (jobApplicationDto == null)
                return BadRequest("Request body is null. Please send a valid JobApplicationDTO object");

            // retrieve the user information from the ClaimsPrincipal
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
                // validate given jobId is valid
                var jobMongoHelper = new MongoHelper<Job>();
                var jobInfo = jobMongoHelper.Collection.AsQueryable().Where(m => m.JobId == jobApplicationDto.JobId).ToList().FirstOrDefault();
                if (jobInfo == null)
                    return BadRequest("Supplied jobId is invalid");
                // if the job is closed then we can't apply to this anymore
                if (jobInfo.HasClosed)
                    return BadRequest("This job has been closed, you can't apply anymore");

                // create the JobApplication object to save to database
                JobApplication jobApplication = new JobApplication()
                {
                    UserId = userInfo.Id,
                    JobId = jobInfo.JobId,
                    PostedOnUtc = DateTime.UtcNow,
                    ApplicationId = ObjectId.GenerateNewId().ToString(),
                    CoverLetter = jobApplicationDto.CoverLetter
                };

                var mongoHelper = new MongoHelper<JobApplication>();
                // check if this user alraedy applied for this job
                var jobApplications =
                    mongoHelper.Collection.AsQueryable()
                        .Where(m => m.JobId == jobApplicationDto.JobId && m.UserId == userInfo.Id);

                if (jobApplications.ToList().Count > 0)
                {
                    return BadRequest("You already applied for this job");
                }

                // save the JobApplication to the database
                var result = mongoHelper.Collection.Save(jobApplication);

                // if mongo failed to save the data then send error
                if (!result.Ok)
                    return InternalServerError();

                // get the hostname
                Uri myuri = new Uri(System.Web.HttpContext.Current.Request.Url.AbsoluteUri);
                string pathQuery = myuri.PathAndQuery;
                hostName = myuri.ToString().Replace(pathQuery, "");

                // send notification to the user who posted the job
                EmailsController emailsController = new EmailsController();
                NotificationsController notificationsController = new NotificationsController();
                if (jobInfo.UserId != userInfo.Id)
                {
                    await emailsController.SendEmail(new Email()
                    {
                        UserId = jobInfo.UserId,
                        Body = "<a href='" + hostName + "/Account/Profile?userId=" + userInfo.Id + "' style='text-decoration:none'>" + userInfo.FirstName + " " + userInfo.LastName + "</a>" + " applied to your job <a href='" + hostName + "/career/jobdetails?jobid=" + jobInfo.JobId + "' style='text-decoration:none'>" + jobInfo.JobTitle + "</a><i>" + jobApplication.CoverLetter + "</i>",
                        Subject = "ShibpurHub | New application to your job \"" + jobInfo.JobTitle + "\""
                    });

                    notificationsController.PostNotification(new Notifications()
                    {
                        UserId = jobInfo.UserId,
                        PostedOnUtc = DateTime.UtcNow,
                        NewNotification = true,
                        NotificationType = NotificationTypes.ReceivedJobApplication,
                        NotificationContent = "{\"appliedBy\":\"" + userInfo.Id + "\",\"displayName\":\"" + userInfo.FirstName + " " + userInfo.LastName + "\",\"jobId\":\"" + jobInfo.JobId + "\",\"profileImage\":\"" + userInfo.ProfileImageURL + "\",\"jobTitle\":\"" + jobInfo.JobTitle + "\"}"
                    });
                }

                // invalidate the cache for the action those will get impacted due to this new application
                var cache = Configuration.CacheOutputConfiguration().GetCacheOutputProvider(Request);

                // invalidate the getjob api call for the job associated with this application
                cache.RemoveStartsWith("career-getjob-jobId=" + jobApplication.JobId);
                // invalidate the getjobapplicationcount api call for the job associated with this application
                cache.RemoveStartsWith("career-getjobapplicationcount-jobId=" + jobApplication.JobId);

                // invalidate the getnotification cache for the user
                cache.RemoveStartsWith("notifications-getnotifications-userId=" + jobInfo.UserId);
                cache.RemoveStartsWith("notifications-getnewnotifications-userId=" + jobInfo.UserId);

                return CreatedAtRoute("DefaultApi", new { id = jobApplication.ApplicationId }, jobApplication);
            }
            catch (MongoConnectionException ex)
            {
                return BadRequest(ex.Message);
            }
        }
        

        /// <summary>
        /// Updates the user activity log.
        /// </summary>
        /// <param name="log">The log.</param>
        private void UpdateUserActivityLog(UserActivityLog log)
        {
            //Call WebApi to log activity
            var userActivityController = new UserActivityController();
            userActivityController.PostAnActivity(log);
        }

        /// <summary>
        /// Increment view count for a question
        /// </summary>
        /// <param name="question"></param>
        /// <returns></returns>
        [ResponseType(typeof(int))]
        [ActionName("IncrementViewCount")]
        [InvalidateCacheOutput("GetViewCount")]
        public int IncrementViewCount(string jobId)
        {
            try
            {
                ObjectId.Parse(jobId);
            }
            catch (Exception)
            {
                return 0;
            }

            var jobInDB = _mongoHelper.Collection.AsQueryable().Where(m => m.JobId == jobId).FirstOrDefault();
            if (jobInDB != null)
            {
                var count = jobInDB.ViewCount + 1;
                jobInDB.ViewCount = count;
                _mongoHelper.Collection.Save(jobInDB);
                return count;
            }

            return 0;
        }
    }
}
