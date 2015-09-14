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
    /// Model to use to post a comment
    /// </summary>
    [Serializable]
    public class CommentDTO
    {
        [MaxLength(600, ErrorMessage = "Maximum 600 characters allowed in comment")]
        public string CommentText { get; set; }

        public string AnswerId { get; set; }
    }


    /// <summary>
    /// Model to save comment data in database
    /// </summary>
    [Serializable]
    [BsonIgnoreExtraElements]
    public class Comment: CommentDTO
    {
        // Primary key
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonProperty(PropertyName = "commentId")]
        public string CommentId { get; set; }
        
        public DateTime PostedOnUtc { get; set; }

        public string UserId { get; set; }
        
        [DataMember]
        public DateTime? LastEditedOnUtc { get; set; }
    }

    /// <summary>
    /// Model with comment user name
    /// </summary>
    [Serializable]
    public class CommentViewModel : Comment
    {
        public string DisplayName { get; set; }
        
        public bool IsCommentedByMe { get; set; }
    }
}