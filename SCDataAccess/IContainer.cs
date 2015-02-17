using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCDataAccess
{
    public interface IContainer
    {
        DbSet<UserProfile> UserProfiles { get; set; }
        DbSet<Ticket> Tickets { get; set; }
        DbSet<TicketCategoryMapping> TicketCategoryMappings { get; set; }
        DbSet<TicketCategoryMaster> TicketCategoryMasters { get; set; }
        DbSet<TicketCommentMapping> TicketCommentMappings { get; set; }
        DbSet<TicketStatu> TicketStatus { get; set; }
        int spUpsertUser(Nullable<int> userID, string id, string password, string fname, string lname, string address, string emailAddress, ObjectParameter errorCode);
    }
}
