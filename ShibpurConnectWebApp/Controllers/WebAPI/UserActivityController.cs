using MongoDB.Bson;
using MongoDB.Driver.Linq;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models.WebAPI;
using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    public class UserActivityController : ApiController
    {
        private MongoHelper<UserActivityLog> _mongoHelper;

        public UserActivityController()
        {
            _mongoHelper = new MongoHelper<UserActivityLog>();
        }

        [ResponseType(typeof(UserActivityLog))]
        public IHttpActionResult GetUserActivities(string userId)
        {
            try
            {
                ObjectId.Parse(userId);
            }
            catch (Exception)
            {
                return BadRequest(String.Format("Supplied userId: {0} is not a valid object id", userId));
            }

            var activities = _mongoHelper.Collection.AsQueryable().Where(m => m.UserId == userId).OrderByDescending(a => a.HappenedAtUTC);

            // TO-DO: Work on DisplayText
            if (activities.Count() == 0)
            {
                return NotFound();
            }

            return Ok(activities.ToList());
        }

        public void PostAnActivity(UserActivityLog log)
        {
            if (log == null)
            {
                return;
            }

            log.HappenedAtUTC = DateTime.UtcNow;
            _mongoHelper.Collection.Save(log);

            var pointsEarned = GetRepCountByActivity(log.Activity);
            var helper = new Helper.Helper();
            helper.UpdateReputationCount(log.UserId, pointsEarned, true);
            if (log.Activity == 3 || log.Activity == 5)
            {
                var points = log.Activity == 3 ? 20 : 50;
                helper.UpdateReputationCount(log.ActedOnUserId, points, true);
            }
        }

        public async Task<int> GetUserReputation(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return 0;
            }
            var helper = new Helper.Helper();
            Task<CustomUserInfo> actionResult = helper.FindUserById(userId);
            var userInfo = await actionResult;

            return userInfo.ReputationCount;
        }

        private int GetRepCountByActivity(int activity)
        {
            switch (activity)
            {
                case 1: // Ask question
                    return 15;

                case 2: // Answer
                    return 30;

                case 3: // Upvote
                    return 2;

                case 4: //comment
                    return 8;

                case 5: //mark as answer
                    return 10;

                case 6: // Joined ShibpurConnect
                    return 100;

                default:
                    return 0;
            }
        }

        /// <summary>
        /// API to get user last login log (when we have shown the toast notification to update user profile and when user logged in into the system)
        /// </summary>
        /// <param name="loginlog"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<IHttpActionResult> GetUserLoginLog()
        {
            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
            var email = principal.Identity.Name;

            Helper.Helper helper = new Helper.Helper();
            var userResult = helper.FindUserByEmail(email);
            var userInfo = await userResult;
            if (userInfo == null)
            {
                return BadRequest("No UserId is found");
            }

            MongoHelper<LoginLog> _mongoHelper2 = new MongoHelper<LoginLog>();          
            // check if user has any previous login log then return that one or else create a new log and save to database and then return the same
            var loginLog = _mongoHelper2.Collection.AsQueryable().Where(m => m.UserId == userInfo.Id).ToList();
            if (loginLog.Count == 0)
            {
                LoginLog newloginLog = new LoginLog()
                {
                    LogId = ObjectId.GenerateNewId().ToString(),
                    UserId = userInfo.Id,
                    EduToastNotificationShownOn = DateTime.UtcNow,
                    EmpToastNotificationShownOn = DateTime.UtcNow,
                    LastSeen = DateTime.UtcNow
                };
                _mongoHelper2.Collection.Save(newloginLog);

                return Ok(newloginLog);
            }

            return Ok(loginLog[0]);
        }

        /// <summary>
        /// API to update user login details, mainly to track when we have shown the toast notification to update user profile and when user logged in into the system
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        [ResponseType(typeof(LoginLog))]
        public async Task<IHttpActionResult> UpdateUserLoginLog(string type)
        {
            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
            var email = principal.Identity.Name;

            Helper.Helper helper = new Helper.Helper();
            var userResult = helper.FindUserByEmail(email);
            var userInfo = await userResult;
            if (userInfo == null)
            {
                return BadRequest("No UserId is found");
            }

            MongoHelper<LoginLog> _mongoHelper2 = new MongoHelper<LoginLog>();
            // check if user has any previous login log then return that one or else create a new log and save to database and then return the same
            var loginLog = _mongoHelper2.Collection.AsQueryable().Where(m => m.UserId == userInfo.Id).ToList();
            if (loginLog.Count == 0)
            {
                LoginLog newloginLog = new LoginLog()
                {
                    LogId = ObjectId.GenerateNewId().ToString(),
                    UserId = userInfo.Id,
                    EduToastNotificationShownOn = DateTime.UtcNow,
                    EmpToastNotificationShownOn = DateTime.UtcNow,
                    LastSeen = DateTime.UtcNow
                };
                _mongoHelper2.Collection.Save(newloginLog);

                return Ok(newloginLog);
            }
            else
            {
                if(type == "edu")
                {
                    loginLog[0].EduToastNotificationShownOn = DateTime.UtcNow;
                    _mongoHelper2.Collection.Save(loginLog[0]);
                }
                else if (type == "emp")
                {
                    loginLog[0].EmpToastNotificationShownOn = DateTime.UtcNow;
                    _mongoHelper2.Collection.Save(loginLog[0]);
                }
            }


            return Ok(loginLog[0]);
        }
    }
}