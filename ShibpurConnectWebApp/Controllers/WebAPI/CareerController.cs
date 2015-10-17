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
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models.WebAPI;
using WebApi.OutputCache.V2;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class CareerController : ApiController
    {
        private MongoHelper<Job> _mongoHelper;
        private int maxSkillSetLength = Convert.ToInt16(ConfigurationManager.AppSettings["maxskillsetlength"]);

        public CareerController()
        {
            _mongoHelper = new MongoHelper<Job>();
        }

        /// <summary>
        /// API to post a new job
        /// </summary>
        /// <param name="jobDto"></param>
        /// <returns></returns>
        [Authorize]
        [ResponseType(typeof(Job))]
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

            //// invalidate the cache for the action those will get impacted due to this new answer post
            //var cache = Configuration.CacheOutputConfiguration().GetCacheOutputProvider(Request);

            //// invalidate the GetAnswersCount api for this question
            //cache.RemoveStartsWith("questions-getquestionsbyuser-userId=" + userInfo.Id);

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
    }
}
