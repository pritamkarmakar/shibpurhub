using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    [Serializable]
    [DataContract(IsReference = true)]
    [JsonObject(IsReference = false)]
    [BsonIgnoreExtraElements]
    public class EmploymentHistories
    {
        // Primary key
        [BsonId]
        public ObjectId Id { get; set; }

        [Required (ErrorMessage = "Company Name field is mandatory")]
        [DataMember]        
        public string CompanyName { get; set; }

        [Required(ErrorMessage = "Job Title field is mandatory")]
        [DataMember]
        public string Title { get; set; }

        [Required(ErrorMessage = "Location field is mandatory")]
        [DataMember]
        public string Location { get; set; }

        [Required(ErrorMessage = "Employment start date field is mandatory")]
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