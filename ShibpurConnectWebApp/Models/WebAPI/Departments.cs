using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    public class Departments
    {
        // Primary key
        [Key]
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string Department { get; set; }
    }
}