namespace ShibpurConnectWebApp
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public partial class AspNetUsers
    {
        public AspNetUsers()
        {
            AspNetUserClaims = new HashSet<AspNetUserClaims>();
            AspNetUserLogins = new HashSet<AspNetUserLogins>();
            AspNetRoles = new HashSet<AspNetRoles>();
        }

        public string Id { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        // to indicate whether its a student, alumni or authority profile
        public int ProfileType { get; set; }
        
        [StringLength(256)]
        public string Email { get; set; }

        public bool EmailConfirmed { get; set; }

        public string PasswordHash { get; set; }

        public string SecurityStamp { get; set; }

        public string PhoneNumber { get; set; }

        public bool PhoneNumberConfirmed { get; set; }

        public bool TwoFactorEnabled { get; set; }

        public DateTime? LockoutEndDateUtc { get; set; }

        public bool LockoutEnabled { get; set; }

        public int AccessFailedCount { get; set; }

        [Required]
        [StringLength(256)]
        public string UserName { get; set; }

        public virtual ICollection<AspNetUserClaims> AspNetUserClaims { get; set; }

        public virtual ICollection<AspNetUserLogins> AspNetUserLogins { get; set; }

        public virtual ICollection<AspNetRoles> AspNetRoles { get; set; }
    }
}
