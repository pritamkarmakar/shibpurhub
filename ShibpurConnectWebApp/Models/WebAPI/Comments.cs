using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    /// <summary>
    /// Model for the comment table
    /// </summary>
    [Serializable]
    [DataContract(IsReference = true)]
    public class Comments
    {
        // Primary key
        [Key]
        [DataMember]
        public string CommentId { get; set; }
        
        [Required]
        [DataMember]
        public string Description { get; set; }
        
        [DataMember]
        public DateTime? PostedOnUtc { get; set; }
        
        // Foreign key
        [ForeignKey("Question")]
        [DataMember]
        public string QuestionId { get; set; }

        [ForeignKey("AspNetUsers")]
        [DataMember]
        public string UserId { get; set; }

        // One comment can be part of only one Question
        public virtual Questions Question { get; set; }

        public virtual AspNetUsers AspNetUsers { get; set; }
    }
}