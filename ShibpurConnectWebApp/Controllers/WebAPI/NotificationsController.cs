using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models.WebAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;

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
        public IEnumerable<string> GetNotifications()
        {
            return new string[] { "value1", "value2" };
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

            // save the notification to the database collection
            var result = _mongoHelper.Collection.Save(notification);

            // if mongo failed to save the data then send error
            if (!result.Ok)
                return InternalServerError();

            return CreatedAtRoute("DefaultApi", new { id = notification.NotificationId }, notification);
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
