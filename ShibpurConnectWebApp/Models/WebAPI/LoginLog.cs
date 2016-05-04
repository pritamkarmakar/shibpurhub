using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using System.Web.Http;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    [Serializable]
    [DataContract(IsReference = true)]
    [JsonObject(IsReference = false)]
    [BsonIgnoreExtraElements]
    public class LoginLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string LogId { get; set; }
        public string UserId { get; set; }
        public DateTime EduToastNotificationShownOn { get; set; }
        public DateTime EmpToastNotificationShownOn { get; set; }
        public DateTime LastSeen { get; set; }
    }
}