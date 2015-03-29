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
    }

    /// <summary>
    /// Notification types that we support
    /// </summary>
    public enum NotificationTypes
    {
        Following,
        Tagged,
        ReceivedAnswer,
        ReceivedComment,
        AskToAnswer
    }
}