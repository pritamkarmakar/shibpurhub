using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    [BsonIgnoreExtraElements]
    public class Categories
    {
        // Primary key
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string CategoryId { get; set; }

        [Required]
        public string CategoryName { get; set; }

        [Required]
        public bool HasPublished { get; set; }       
    }

    /// <summary>
    /// Will use this model in the tag cloud
    /// </summary>
    public class CategoryCloud: Categories
    {
        public int QuestionCount { get; set; }
    }
}