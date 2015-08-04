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

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    public class NotificationsController : ApiController
    {
        private MongoHelper<Notifications> _mongoHelper;

        public NotificationsController()
        {
            _mongoHelper = new MongoHelper<Notifications>();
        }

        /// <summary>
        /// Get all the notifications for a particular user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [Authorize]
        [CacheOutput(ServerTimeSpan = 864000, ExcludeQueryStringFromCacheKey = true)]
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
        [CacheOutput(ServerTimeSpan = 864000, ExcludeQueryStringFromCacheKey = true)]
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
    }
}
