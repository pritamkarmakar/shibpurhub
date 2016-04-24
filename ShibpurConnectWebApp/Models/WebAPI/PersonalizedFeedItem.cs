using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    [BsonIgnoreExtraElements]
    [Serializable]
    public class PersonalizedFeedItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonProperty(PropertyName = "personalizedFeedItemId")]
        public string PersonalizedFeedItemId { get; set; }
        
        [DataMember]
        public string LogId { get; set; }

        [DataMember]
        public int ActivityType { get; set; }
        
        [DataMember]
        public string UserId { get; set; }
        
        [DataMember]
        public string ActedOnUserId { get; set; }
        
        [DataMember]
        public string ActedOnObjectId { get; set; }

        [DataMember]
        public string QuestionId { get; set; }

        [DataMember]
        public string AnswerId { get; set; }

        [DataMember]
        public string UserName { get; set; }

        [DataMember]
        public string UserProfileUrl { get; set; }

        [DataMember]
        public string UserProfileImageUrl { get; set; }

        [DataMember]
        public string ActionText { get; set; }

        [DataMember]
        public string TargetAction { get; set; }

        [DataMember]
        public string TargetActionUrl { get; set; }

        [DataMember]
        public string UserDesignation { get; set; }

        [DataMember]
        public string ItemHeader { get; set; }
        
        [DataMember]
        public string ItemSubHeader { get; set; }

        [DataMember]
        public string ItemDetail { get; set; }

        [DataMember]
        public string ItemImageUrl { get; set; }

        [DataMember]
        public Dictionary<string, string> ItemValues { get; set; }
        
        [DataMember]
        public IList<PersonalizedFeedItem> ChildItems { get; set; }

        public int ViewCount { get; set; }

        public DateTime? PostedDateInUTC { get; set; }

        public int AnswersCount { get; set; }
        
        public int FollowedByCount { get; set; }
        
        public bool IsFollowedByme { get; set; }
        
        public int UpvoteCount { get; set; }
        
        public bool IsUpvotedByme { get; set; }
        
        public void CopyFromUserActivityLog(UserActivityLog log)
        {
            if (log == null)
            {
                return;
            }
            
            this.LogId = log.ActivityLogId;
            this.UserId = log.UserId;
            this.ActedOnUserId = log.ActedOnUserId;
            this.ActedOnObjectId = log.ActedOnObjectId;
            this.PostedDateInUTC = log.HappenedAtUTC;
            this.ActivityType = log.Activity;
            this.ActionText = GetActionText(log.Activity);
        }
        
        private string GetActionText(int type)
        {
            if (type == 1)
            {
                return " asked a ";
            }

            if (type == 2)
            {
                return " answered a ";
            }

            if (type == 3)
            {
                return " upvoted a ";
            }

            if (type == 4)
            {
                return " commented on a ";
            }

            if (type == 5)
            {
                return " marked an answer for ";
            }

            if (type == 6)
            {
                return " joined ShibpurHub";
            }

            if (type == 7 || type == 8)
            {
                return " started following ";
            }

            if (type == 9)
            {
                return " updated profile image ";
            }

            if (type == 10)
            {
                return " posted a new ";
            }

            return string.Empty;
        }
    }
}