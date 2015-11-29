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
    /// Model to use to apply a job
    /// </summary>
    [Serializable]
    public class JobApplicationDTO
    {
        [DataMember]
        public string CoverLetter { get; set; }

        [DataMember]
        public string JobId { get; set; } 
    }

    /// <summary>
    /// Model to use to save the record to database
    /// </summary>
    [Serializable]
    public class JobApplication : JobApplicationDTO
    {

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonProperty(PropertyName = "applicationId")]
        public string ApplicationId { get; set; }

        [DataMember]
        public string UserId { get; set; }

        [DataMember]
        public DateTime PostedOnUtc { get; set; } 
    }

    /// <summary>
    /// Model to use in the API as return object
    /// </summary>
    [Serializable]
    public class JobApplicationViewModel : JobApplication
    {
        public string DisplayName { get; set; }

        public string CareerDetail { get; set; }

        [DataMember]
        public string UserProfileImage { get; set; }

        /// <summary>
        /// List of comment associated with this job
        /// </summary>
        [DataMember]
        public List<JobApplicationCommentViewModel> ApplicationComments { get; set; }
    }
}