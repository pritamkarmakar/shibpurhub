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
        public string Title { get; set; }
        
        [Required]
        [DataMember]
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
    }
}