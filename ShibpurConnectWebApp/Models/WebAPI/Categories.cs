using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    public class Categories
    {
        // Primary key
        [BsonId]
        public ObjectId CategoryId { get; set; }

        [Required]
        public string CategoryName { get; set; }

        [Required]
        public bool HasPublished { get; set; }

        // One category can be used in multiple question. There is a many to many relation between Questions and Category tables
        //public virtual IEnumerable<Questions> Questions { get; set; } 
    }
}