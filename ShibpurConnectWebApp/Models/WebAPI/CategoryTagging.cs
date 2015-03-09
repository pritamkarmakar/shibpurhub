using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    public class CategoryTagging
    {
        // Primary key
        [BsonId]
        public ObjectId Id { get; set; }

        // Foreign keys
        public ObjectId QuestionId { get; set; }
        public ObjectId CategoryId { get; set; }

        public virtual Questions Question { get; set; }
        // One question can be part of many Categories
        public virtual IEnumerable<Categories> Categories { get; set; }
    }
}