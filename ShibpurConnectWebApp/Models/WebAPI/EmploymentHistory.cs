using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    [Serializable]
    [DataContract(IsReference = true)]
    public class EmploymentHistory
    {
        // Primary key
        [Key]
        [DataMember]
        public string Id { get; set; }

        [Required]
        [DataMember]
        public string CompanyName { get; set; }

        [Required]
        [DataMember]
        public DateTime? From { get; set; }
        
        [DataMember]
        public DateTime? To { get; set; }

        // Foreign key
        [ForeignKey("AspNetUsers")]
        [DataMember]
        public string UserId { get; set; }

        public virtual AspNetUsers AspNetUsers { get; set; }
    }
}