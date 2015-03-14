using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using ShibpurConnectWebApp.Models.WebAPI;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    public class ProfileController : ApiController
    {
        // GET: api/profile/getprofile?useremail=<email>
        // Get the user profile for a particular user
        public async Task<IHttpActionResult> GetProfile(string userEmail)
        {
            // validate userEmail is valid and get the userid
            Helper.Helper helper = new Helper.Helper();
            Task<CustomUserInfo> actionResult = helper.FindUserByEmail(userEmail);
            var userInfo = await actionResult;

            if (userInfo == null)
                return NotFound();

            // get the user education details
            EducationalHistoriesController educationalHistoriesController = new EducationalHistoriesController();
            IHttpActionResult actionResult2 = await educationalHistoriesController.GetEducationalHistories(userInfo.Email);
            var education = actionResult2 as OkNegotiatedContentResult<List<EducationalHistories>>;

            // get the user employment details
            EmploymentHistoriesController employmentHistoriesController = new EmploymentHistoriesController();
            IHttpActionResult actionResult3 = await employmentHistoriesController.GetEmploymentHistories(userInfo.Email);
            var employment = actionResult3 as OkNegotiatedContentResult<List<EmploymentHistories>>;

            // now form the UserProfile object
            UserProfile userProfile = new UserProfile();
            if (education != null) userProfile.EducationalHistories = education.Content;
            if (employment != null) userProfile.EmploymentHistories = employment.Content;
            userProfile.UserInfo = userInfo;

            return Ok(userProfile);
        }

        // GET: api/Profile/5
        public string Get(int id)
        {
            return "value";
        }

        // POST: api/Profile
        public void Post([FromBody]string value)
        {
        }

        // PUT: api/Profile/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/Profile/5
        public void Delete(int id)
        {
        }
    }
}
