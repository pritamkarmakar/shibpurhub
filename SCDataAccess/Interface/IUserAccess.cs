using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCDataAccess.Interface
{
    public interface IUserAccess
    {
        UserProfile GetUserDetails(int userId);
        void SaveUser(int userId, string fName, string LName, string Address, String emailAddress);

    }
}
