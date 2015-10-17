using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    [Serializable]
    public class SkillSets
    {
        // Primary key
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string SkillSetId { get; set; }

        [Required]
        public string SkillSetName { get; set; }
    }
}