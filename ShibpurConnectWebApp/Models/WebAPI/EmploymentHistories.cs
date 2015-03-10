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
        [DataMember]
        public string CompanyName { get; set; }

        [Required]
        [DataMember]
        public DateTime? From { get; set; }

        [DataMember]
        public DateTime? To { get; set; }

        // Foreign key
        //[ForeignKey("AspNetUsers")]
        [Required]
        [DataMember]
        public string UserId { get; set; }

        //public virtual AspNetUsers AspNetUsers { get; set; }
    }
}