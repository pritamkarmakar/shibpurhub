using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    [Serializable]
    [DataContract(IsReference = true)]
    [BsonIgnoreExtraElements]
    public class Questions
    {
        // Primary key
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonProperty(PropertyName = "questionId")]
        public string QuestionId { get; set; }
        
        [Required]
        [DataMember]        
        [MinLength(20, ErrorMessage="Minimum length of the question title should be more than 20 characters")]
        [MaxLength(150, ErrorMessage = "Maximum 150 characters allowed in question title")]
        public string Title { get; set; }
        
        [Required]
        [DataMember]
        [MinLength(40, ErrorMessage = "Minimum length of description should be more than 40 characters")]
        [MaxLength(30000, ErrorMessage = "Maximum 30000 characters allowed in description")]
        public string Description { get; set; }
        
        [DataMember]
        public bool HasAnswered { get; set; }

        [DataMember]
        public DateTime? PostedOnUtc { get; set; }

        // Foreign key
        //[ForeignKey("AspNetUsers")]
        [Required]
        [DataMember]
        public string UserEmail { get; set; }

        [DataMember]
        public int ViewCount { get; set; }
    }

    /// <summary>
    /// We will use this to send data from the API. Thi has a new property UserName. We will add firstname, lastname as well later
    /// </summary>
    [Serializable]
    [BsonIgnoreExtraElements]
    public class QuestionsDTO : Questions
    {
        [Required]
        [DataMember]
        public string[] Categories { get; set; }

        [DataMember]
        public string DisplayName { get; set; }

        [Required]
        [DataMember]
        public string UserProfileImage { get; set; }
    }
}