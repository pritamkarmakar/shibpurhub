using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    [DataContract(IsReference = true)]
    [JsonObject(IsReference = false)]
    [BsonIgnoreExtraElements]
    public class ImageInfo
    {
        public string UserId { get; set; }

        public string ImageBase64 { get; set; }
    }
}