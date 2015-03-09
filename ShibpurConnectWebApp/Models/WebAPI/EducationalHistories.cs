using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    [Serializable]
    [DataContract(IsReference = true)]
    public class EducationalHistories
    {
        // Primary key
        [BsonId]
        public ObjectId Id { get; set; }

        [Required]
        [DataMember]
        public string UniversityName { get; set; }

        //[Required]
        //[DataMember]
        //public string Department { get; set; }

        [DataMember]
        public int GraduateYear { get; set; }

        // Foreign key
        //[BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; }

        // Foreign key
        [ForeignKey("Departments")]
        [DataMember]
        public string Department { get; set; }

        //public virtual AspNetUsers AspNetUsers { get; set; }
        //public virtual Department Departments { get; set; }
    }
}