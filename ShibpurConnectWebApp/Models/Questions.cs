using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using ShibpurConnectWebApp.Migrations;

namespace ShibpurConnectWebApp.Models
{
    [Serializable]
    [DataContract(IsReference = true)]
    public class Questions
    {
        // Primary key
        [Key]
        [DataMember]
        public string QuestionId { get; set; }
        
        [Required]
        [DataMember]
        public string Title { get; set; }
        
        [Required]
        [DataMember]
        public string Description { get; set; }
        
        [DataMember]
        public bool HasAnswered { get; set; }

        [DataMember]
        public DateTime? PostedOnUtc { get; set; }

        // Foreign key
        [ForeignKey("AspNetUsers")]
        [DataMember]
        public string UserId { get; set; }

        public virtual AspNetUsers AspNetUsers { get; set; }
        
        // One question can have multiple comments
        [DataMember]
        public virtual IEnumerable<Comments> Comments { get; set; }

        // Categories associated with the question
        [DataMember]
        public virtual IEnumerable<Categories> Categories { get; set; } 
    }
}