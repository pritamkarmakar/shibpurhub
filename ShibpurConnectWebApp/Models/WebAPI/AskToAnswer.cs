using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    /// <summary>
    /// AskToAnswer model for the API
    /// </summary>
    [Serializable]
    [BsonIgnoreExtraElements]
    public class AskToAnswerDTO
    {
        [DataMember]
        [Required]
        public string AskedTo { get; set; }

        [DataMember]
        [Required]
        public string AskedBy { get; set; }

        [DataMember]
        [Required]
        public string QuestionId { get; set; }

    }

    /// <summary>
    /// Model we will use to save to database
    /// </summary>
    public class AskToAnswer : AskToAnswerDTO
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [DataMember]
        public DateTime AskedOnUtc { get; set; }

        [DataMember]
        [Required]
        public bool HasAnswered { get; set; }

    }
}