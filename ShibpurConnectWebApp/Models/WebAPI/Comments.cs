using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    /// <summary>
    /// Model for the comment table
    /// </summary>
    [Serializable]
    [DataContract(IsReference = true)]
    [BsonIgnoreExtraElements]
    public class Comment
    {
        // Primary key
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonProperty(PropertyName = "commentId")]
        public string CommentId { get; set; }
        
        [DataMember]
        public string Description { get; set; }
        
        [DataMember]
        public DateTime PostedOnUtc { get; set; }
        
        // Foreign key
        [DataMember]
        public string AnswerId { get; set; }

        [DataMember]
        public string UserId { get; set; }

        [DataMember]
        public string DisplayName { get; set; }
    }
}