using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    [Serializable]
    //[DataContract(IsReference = true)]
    //[JsonObject(IsReference = false)]
    public class EducationalHistories
    {
        // Primary key
        //[BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        [Required]
        [DataMember]
        public string UniversityName { get; set; }

        //[Required]
        //[DataMember]
        //public string Department { get; set; }

        [Required]
        [DataMember]
        public int GraduateYear { get; set; }

        [Required]
        [DataMember]
        public string UserId { get; set; }

        // Foreign key
        //[ForeignKey("Departments")]
        [Required]
        [DataMember]
        public string Department { get; set; }

        //public virtual AspNetUsers AspNetUsers { get; set; }
        //public virtual Department Departments { get; set; }
    }
}