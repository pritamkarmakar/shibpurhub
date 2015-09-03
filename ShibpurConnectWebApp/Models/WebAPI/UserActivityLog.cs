using System;
using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    [BsonIgnoreExtraElements]
    public class UserActivityLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonProperty(PropertyName = "activityLogId")]
        public string ActivityLogId { get; set; }

        [DataMember]
        public string UserId { get; set; }

        // 1: Ask question, 2: Answer, 3: Upvote, 4: Comment, 5: Mark as Answer, 
        // 6: Register as new user, 7: Follow an user, 8: Follow a question, 9: Update profile image
        [DataMember]
        public int Activity { get; set; }

        //e.g. QuestionId, AnswerId, CommentId etc.
        [DataMember]
        public string ActedOnObjectId { get; set; }

        //In case of 3: Upvote, 5: Mark as Answer > UserID of Answer
        [DataMember]
        public string ActedOnUserId { get; set; }

        //[DataMember]
        //public int PointsEarned { get; set; }

        [DataMember]
        public DateTime HappenedAtUTC { get; set; }

        //e.g. Pritam asked a qustion: Who is the...
        public string DisplayText { get; set; }
    }
}