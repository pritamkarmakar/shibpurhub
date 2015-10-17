using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    /// <summary>
    /// Data model to post a new job opportunity
    /// </summary>
    [Serializable]
    public class JobDTO
    {
        // Primary key
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonProperty(PropertyName = "jobId")]
        public string JobId { get; set; }

        [Required]
        public string JobTitle { get; set; }

        [Required]
        public string JobDescription { get; set; }

        [Required]
        [DataMember]
        public string[] SkillSets { get; set; }
    }

    [Serializable]
    public class Job : JobDTO
    {
        [DataMember]
        public string UserId { get; set; }

        [DataMember]
        public bool HasClosed { get; set; }

        [DataMember]
        public DateTime? PostedOnUtc { get; set; }

        [DataMember]
        public int ViewCount { get; set; }

        public int SpamCount { get; set; }

        public List<string> Followers { get; set; }

        [DataMember]
        public DateTime? LastEditedOnUtc { get; set; }
    }
}
