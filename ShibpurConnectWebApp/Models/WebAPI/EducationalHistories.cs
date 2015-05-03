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
    [BsonIgnoreExtraElements]
    public class EducationalHistories
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        [Required]
        [DataMember]
        public string UniversityName { get; set; }       

        [Required]
        [DataMember]
        [Range(typeof(int), "1950", "2025",
        ErrorMessage =  "Value for 'Gradutiaon year' must be between {1} and {2}")]
        public int GraduateYear { get; set; }

        [Required]
        [DataMember]
        public string UserId { get; set; }

        [Required]
        [DataMember]
        public string Department { get; set; }       
    }
}