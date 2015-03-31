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
            log.PointsEarned = GetRepCountByActivity(log.Activity);
            _mongoHelper.Collection.Save(log);
        }

        public int GetUserReputation(string userId)
        {
            if(string.IsNullOrEmpty(userId))
            {
                return 0;
            }

            var count = 0;
            var activities = _mongoHelper.Collection.AsQueryable().Where(m => m.UserId == userId).ToList();
            foreach(var activity in activities)
            {
                count += activity.PointsEarned;
            }
            var mongoAnswerHelper = new MongoHelper<Answer>();
            var answersByUser = mongoAnswerHelper.Collection.AsQueryable().Where(a => a.UserId == userId).ToList();
            count += answersByUser.Where(a => a.MarkedAsAnswer).Count() * 50;
            count += answersByUser.Sum(a => a.UpVoteCount) * 20;
            return count;
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

                default:
                    return 0;
            }
        }
    }
}