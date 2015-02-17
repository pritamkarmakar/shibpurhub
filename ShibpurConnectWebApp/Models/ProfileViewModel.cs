using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShibpurConnectWebApp.Models
{
    public class ProfileViewModel
    {
        public int UserID { get; set; }

        [Required]
        [Display(Name="First Name")]
        public string FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [Display(Name = "Department")]
        public string DepartMent { get; set; }

        public IEnumerable<string> Departments = new List<string> 
        {   "Architecture", 
            "Civil", 
            "Computer Science",
            "Electrical", 
            "Electronics & Communication", "Mechanical"
        };
            
        [Required]
        [Display(Name="Passout Batch")]
        public int PassoutYear { get; set; }

        [Required]
        [Display(Name = "Email")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Display(Name = "Address")]
        public string Address { get; set; }

        [Display(Name = "Phone Number")]
        [DataType(DataType.PhoneNumber)]
        public string Phone { get; set; }

        
    }
}