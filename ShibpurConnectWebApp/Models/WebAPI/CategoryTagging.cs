using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    [BsonIgnoreExtraElements]
    public class CategoryTagging
    {
        // Primary key
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // Foreign keys
        public string QuestionId { get; set; }
        public string CategoryId { get; set; }

        //public virtual Questions Question { get; set; }
        // One question can be part of many Categories
        //public virtual IEnumerable<Categories> Categories { get; set; }
    }
}