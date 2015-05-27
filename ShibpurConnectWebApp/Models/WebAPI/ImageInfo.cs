using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    /// <summary>
    /// Model for user profile image
    /// </summary>
    [DataContract(IsReference = true)]
    [JsonObject(IsReference = false)]
    [BsonIgnoreExtraElements]
    public class ImageInfo
    {
        public string ImageBase64 { get; set; }
    }
}