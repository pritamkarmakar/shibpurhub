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
            var joblist = _mongoHelper.Collection.FindAll().OrderByDescending(a => a.PostedOnUtc).Skip(page * pageSize).Take(pageSize).ToList();

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
                jobDTO.LastEditedOnUtc = job.LastEditedOnUtc;
                jobDTO.PostedOnUtc = job.PostedOnUtc;
                jobDTO.SkillSets = job.SkillSets;
                jobDTO.SpamCount = job.SpamCount;
                jobDTO.ViewCount = job.ViewCount;
                jobDTO.DisplayName = userDetails[job.UserId].FirstName + " " + userDetails[job.UserId].LastName;
                jobDTO.UserProfileImage = userDetails[job.UserId].ProfileImageURL;
                jobDTO.CareerDetail = userDetails[job.UserId].Designation + " " +
                                      (string.IsNullOrEmpty(userDetails[job.UserId].EducationInfo)
                                          ? string.Empty
                                          : (string.IsNullOrEmpty(userDetails[job.UserId].Designation)
                                              ? userDetails[job.UserId].EducationInfo
                                              : "(" + userDetails[job.UserId].EducationInfo + ")")
                                          );

                result.Add(jobDTO);
            }

            return Ok(result);
        }

        /// <summary>
        /// API to get details about a specific jobId
        /// </summary>
        /// <param name="jobId">jobId to retrieve the details</param>
        /// <returns></returns>
        [CacheOutput(ServerTimeSpan = 864000, ExcludeQueryStringFromCacheKey = true, NoCache = true)]
        public async Task<IHttpActionResult> GetJob(string jobId)
        {
            try
            {
                var jobDetails = _mongoHelper.Collection.AsQueryable().FirstOrDefault(m => m.JobId == jobId);
                if (jobDetails == null)
                {
                    return NotFound();
                }

                Helper.Helper helper = new Helper.Helper();
                Task<CustomUserInfo> actionResult = helper.FindUserById(jobDetails.UserId, true);
                var userDetails = await actionResult;

                var jvm = new JobViewModel()
                {
                    UserId = jobDetails.UserId,
                    Followers = jobDetails.Followers,
                    JobId = jobDetails.JobId,
                    JobDescription = jobDetails.JobDescription,
                    JobTitle = jobDetails.JobTitle,
                    SkillSets = jobDetails.SkillSets,
                    ViewCount = jobDetails.ViewCount,
                    LastEditedOnUtc = jobDetails.LastEditedOnUtc,
                    HasClosed = jobDetails.HasClosed,
                    PostedOnUtc = jobDetails.PostedOnUtc,
                    SpamCount = jobDetails.SpamCount,
                    DisplayName = userDetails.FirstName + " " + userDetails.LastName,
                    UserProfileImage = userDetails.ProfileImageURL,
                    CareerDetail = userDetails.Designation + " " +
                       (string.IsNullOrEmpty(userDetails.EducationInfo) ? string.Empty :
                       (string.IsNullOrEmpty(userDetails.Designation) ? userDetails.EducationInfo :
                           "(" + userDetails.EducationInfo + ")")
                       ),
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

            // invalidate the cache for the action those will get impacted due to this new answer post
            //var cache = Configuration.CacheOutputConfiguration().GetCacheOutputProvider(Request);

            // invalidate the GetAnswersCount api for this question
            //cache.RemoveStartsWith("career-getjobs-userId=" + userInfo.Id);

            ////Invalidate personalized feed cache
            //var userIdToInvalidate = userInfo.Followers == null ? new List<string>() : userInfo.Followers;
            //userIdToInvalidate.Add(userInfo.Id);
            //BackgroundJob.Enqueue(() => WebApiCacheHelper.InvalidatePersonalizedFeedCache(userIdToInvalidate));

            // if mongo failed to save the data then send error
            if (!result.Ok)
                return InternalServerError();

            return CreatedAtRoute("DefaultApi", new { id = jobToPost.JobId }, jobToPost);
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
