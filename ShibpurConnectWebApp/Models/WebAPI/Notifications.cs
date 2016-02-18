using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    /// <summary>
    /// Notification model
    /// </summary>
    [Serializable]
    [BsonIgnoreExtraElements]
    public class Notifications
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string NotificationId { get; set; }

        /// <summary>
        /// userid for which this notification is
        /// </summary>
        [DataMember]
        public string UserId { get; set; }

        [DataMember]
        public DateTime PostedOnUtc { get; set; }

        [DataMember]
        public bool HasVisited { get; set; }

        [DataMember]
        public bool NewNotification { get; set; }

        [DataMember]
        [Required]
        public string NotificationContent { get; set; }

        [DataMember]
        [Required]
        public NotificationTypes NotificationType { get; set; }

        /// <summary>
        /// userid of the user who caused this notification
        /// </summary>
        [DataMember]
        [Required]
        public string NotificationByUser { get; set; }
    }

    /// <summary>
    /// Notification types that we support
    /// ***Don't add a new notification type in between this list as it will break the UI (because each enum correspond to an integer).
    /// If we need to add a new type then add it at the end ***
    /// </summary>
    public enum NotificationTypes
    {
        Following,
        Tagged,
        ReceivedAnswer,
        ReceivedComment,
        ReceivedJobApplication,
        ReceivedCommentInJobApplication,
        AskToAnswer,
        ReceivedCommentInQuestion
    }
}