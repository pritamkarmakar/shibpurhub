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
    public class EducationalHistory
    {
        // Primary key
        [Key]
        [DataMember]
        public string Id { get; set; }

        [Required]
        [DataMember]
        public string UniversityName { get; set; }

        //[Required]
        //[DataMember]
        //public string Department { get; set; }

        [DataMember]
        public int GraduateYear { get; set; }

        // Foreign key
        [ForeignKey("AspNetUsers")]
        [DataMember]
        public string UserId { get; set; }

        // Foreign key
        [ForeignKey("Departments")]
        [DataMember]
        public string Department { get; set; }

        public virtual AspNetUsers AspNetUsers { get; set; }
        public virtual Departments Departments { get; set; }
    }
}