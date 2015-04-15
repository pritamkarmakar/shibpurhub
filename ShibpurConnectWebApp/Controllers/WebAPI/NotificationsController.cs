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

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    public class NotificationsController : ApiController
    {
        private MongoHelper<Notifications> _mongoHelper;

        public NotificationsController()
        {
            _mongoHelper = new MongoHelper<Notifications>();
        }

        // GET: api/Notification
        public IList<Notifications> GetNotifications(string userId)
        {
            var result = (from e in _mongoHelper.Collection.AsQueryable<Notifications>()
                          where e.UserId == userId
                          orderby e.PostedOnUtc descending
                          select e).ToList();

            return result;
        }

        public IList<Notifications> GetNewNotifications(string userId)
        {
            var result = (from e in _mongoHelper.Collection.AsQueryable<Notifications>()
                          where e.UserId == userId && e.NewNotification == true
                          select e).ToList();

            return result;
        }

        // GET: api/Notification/5
        public string Get(int id)
        {
            return "value";
        }

        // POST: api/Notification
        /// <summary>
        /// Submit a new notification
        /// </summary>
        /// <param name="notification">Notification object</param>
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

        public IHttpActionResult MarkAllNewNotificationsAsOld(string userId)
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

            return Ok();
        }

        /// <summary>
        /// Mark a notification as visited. We use this method in the Notification pane when user click on a particular notification to mark that as visited
        /// </summary>
        /// <param name="notificationId">Notification Id</param>
        /// <returns></returns>
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

            return Ok();
        }

        // PUT: api/Notification/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/Notification/5
        public void Delete(int id)
        {
        }
    }
}
