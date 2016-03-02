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
    public class EducationalHistoriesDTO
    {
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
        public string Department { get; set; }       
    }

    /// <summary>
    /// We will use this model to save data into database
    /// </summary>
    public class EducationalHistories : EducationalHistoriesDTO
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [DataMember]
        public string UserId { get; set; }

        [DataMember]
        public bool IsBECEducation { get; set; }
    }
}