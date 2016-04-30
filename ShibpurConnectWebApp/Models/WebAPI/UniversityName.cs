using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    [BsonIgnoreExtraElements]
    public class UniversityName
    {
        // Primary key
        [BsonId]
        public ObjectId Id { get; set; }

        [DataMember]
        public string UName { get; set; }
    }
}