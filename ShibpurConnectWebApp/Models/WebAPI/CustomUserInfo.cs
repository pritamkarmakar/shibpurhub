using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System.Runtime.Serialization;
namespace ShibpurConnectWebApp.Models.WebAPI
{
    /// <summary>
    /// We will use this model in the api/FindUser API. Main intention is to send the userid to the API user, so that we can use the userid for other APIs
    /// </summary>
    [DataContract(IsReference = true)]
    [JsonObject(IsReference = false)]
    [BsonIgnoreExtraElements]
    public class CustomUserInfo
    {
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Location { get; set; }
        public int ReputationCount { get; set; }
    }
}