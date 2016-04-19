using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    /// <summary>
    /// Model to hold user by graduation batch. Will use it in Users Page
    /// </summary>
    [DataContract(IsReference = true)]
    [JsonObject(IsReference = false)]
    [BsonIgnoreExtraElements]
    public class UserByBatch
    {
        public int GraduateYear { get; set; }
        public List<CustomUserInfo> UserList { get; set; }
    }

    public class UserByBatchRootObject
    {
        public List<UserByBatch> UserByBatch { get; set; }
    }
}