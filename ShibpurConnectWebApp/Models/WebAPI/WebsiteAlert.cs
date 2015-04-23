using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    [Serializable]
    public class WebsiteAlert
    {
        // Primary key
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string AlertId { get; set; }

        [DataMember]
        public DateTime AlertTime { get; set; }

        [DataMember]
        public string Content { get; set; }

        [DataMember]
        public string Source { get; set; }

        [DataMember]
        public string EmailSentTo { get; set; }
    }

    [Serializable]
    public class Alert
    {
        public List<WebsiteAlert> Lists { get; set; }
    }
}