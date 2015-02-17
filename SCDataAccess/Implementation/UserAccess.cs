using SCDataAccess.Interface;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCDataAccess.Implementation
{
    public class UserAccess : IUserAccess
    {
        IContainer container;
        UserAccess(IContainer container)
        {
            this.container = container;
        }
        //SCCatalogEntities entity = new SCCatalogEntities();
        public UserProfile GetUserDetails(int UserId)
        {
            return container.UserProfiles.Where(U => U.UserId == UserId).FirstOrDefault();
        }

        public void SaveUser(int userId, string fName, string LName, string address, string emailAddress)
        {
            var errorCode = new ObjectParameter("errorCode", typeof(int));
            container.spUpsertUser(userId, emailAddress, null, fName, LName, address, emailAddress, errorCode);
        }
    }
}
