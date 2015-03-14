using System;
using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    public class Answer
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonProperty(PropertyName = "answerId")]
        public string AnswerId { get; set; }

        [DataMember]
        public string UserId { get; set; }

        [DataMember]
        public string QuestionId { get; set; }

        [DataMember]
        public string AnswerText { get; set; }

        [DataMember]
        public bool MarkedAsAnswer { get; set; }

        [DataMember]
        public int UpVoteCount { get; set; }

        [DataMember]
        public int DownVoteCount { get; set; }

        [DataMember]
        public DateTime PostedOnUtc { get; set; }
    }
}