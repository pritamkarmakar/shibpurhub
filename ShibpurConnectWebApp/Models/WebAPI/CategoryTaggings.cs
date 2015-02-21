using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    public class CategoryTaggings
    {
        // Primary key
        [Key]
        public string Id { get; set; }

        // Foreign keys
        public string QuestionId { get; set; }
        public string CategoryId { get; set; }

        public virtual Questions Question { get; set; }
        // One question can be part of many Categories
        public virtual IEnumerable<Categories> Categories { get; set; }
    }
}