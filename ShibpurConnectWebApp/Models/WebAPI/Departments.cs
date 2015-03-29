using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    [BsonIgnoreExtraElements]
    public class Departments
    {
        // Primary key
        [BsonId]
        public ObjectId Id { get; set; }

        [DataMember]
        public string DepartmentName { get; set; }
    }
}