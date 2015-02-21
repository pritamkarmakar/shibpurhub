using System.ComponentModel.DataAnnotations;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    public class Categories
    {
        // Primary key
        [Key]
        public string CategoryId { get; set; }

        [Required]
        public string Name { get; set; }

        // One category can be used in multiple question. There is a many to many relation between Questions and Category tables
        //public virtual IEnumerable<Questions> Questions { get; set; } 
    }
}