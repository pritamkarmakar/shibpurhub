using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace ShibpurConnectWebApp.Models
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