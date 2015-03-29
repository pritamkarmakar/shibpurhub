using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

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
        public ObjectId CommentId { get; set; }
        
        [Required]
        [DataMember]
        public string Description { get; set; }
        
        [DataMember]
        public DateTime? PostedOnUtc { get; set; }
        
        // Foreign key
        [ForeignKey("Question")]
        [DataMember]
        public ObjectId QuestionId { get; set; }

        [ForeignKey("AspNetUsers")]
        [DataMember]
        public ObjectId UserId { get; set; }

        //// One comment can be part of only one Question
        //public virtual Question Question { get; set; }

        //public virtual AspNetUsers AspNetUsers { get; set; }
    }
}