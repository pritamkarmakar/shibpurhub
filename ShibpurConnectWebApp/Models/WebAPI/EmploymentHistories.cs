using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Serialization;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    [Serializable]
    [DataContract(IsReference = true)]
    public class EmploymentHistories
    {
        // Primary key
        [BsonId]
        public ObjectId Id { get; set; }

        [Required]
        public string CompanyName { get; set; }

        [Required]
        public DateTime? From { get; set; }
        
        public DateTime? To { get; set; }

        // Foreign key
        [ForeignKey("AspNetUsers")]
        [DataMember]
        public ObjectId UserId { get; set; }

        //public virtual AspNetUsers AspNetUsers { get; set; }
    }
}