using SCDataAccess.Interface;
using ShibpurConnect.Contract;
using ShibpurConnect.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using Microsoft.Practices.Unity;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ShibpurConnect.WebApi
{
    [RoutePrefix("v1.0/UserController")]
    public class UserController : ApiController
    {
        public IUserAccess userAccess;
        public UserController()
        {
            this.userAccess = DependencyInjectionServiceFactory.DependencyInjector.Resolve<IUserAccess>();
        }

        [Route("user/{userId:int}")]
        [HttpGet]
        public async Task<UserProfile> getUserProfile(int userId)
        {            
            var result =  await Task.Run(() => this.userAccess.GetUserDetails(userId));
            UserProfile profile = new UserProfile() { UserId = result.UserId, FirstName = result.Legal_First_Name, lastName = result.Legal_last_Name, Address = result.Address };
            return profile;
        }
    }
}