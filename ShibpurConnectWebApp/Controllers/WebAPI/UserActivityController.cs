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
using System.Threading.Tasks;

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
            if(log == null)
            {
                return;
            }

            log.HappenedAtUTC = DateTime.UtcNow;
            _mongoHelper.Collection.Save(log);

            var pointsEarned = GetRepCountByActivity(log.Activity);
            var helper = new Helper.Helper();
            helper.UpdateReputationCount(log.UserId, pointsEarned, true);
            if(log.Activity == 3 || log.Activity == 5)
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
            switch(activity)
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
    }
}