using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using AspNet.Identity.MongoDB;
using Microsoft.AspNet.Identity;
using ShibpurConnectWebApp.Models;
using ShibpurConnectWebApp.Models.WebAPI;
using ShibpurConnectWebApp.Providers;
using System;
using ShibpurConnectWebApp.Helper;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    [RoutePrefix("api/Account")]
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class AccountController : ApiController
    {
        private AuthRepository _repo = null;       

        public AccountController()
        {
            _repo = new AuthRepository();
        }

        // POST api/Register
        /// <summary>
        /// POST: Register a new user
        /// </summary>
        /// <param name="userModel"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [Route("Register")]
        public async Task<IHttpActionResult> Register(RegisterViewModel userModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IdentityResult result = await _repo.RegisterUser(userModel);         
            IHttpActionResult errorResult = GetErrorResult(result);

            if (errorResult != null)
            {
                return errorResult;
            }          

            return Ok();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _repo.Dispose();
            }

            base.Dispose(disposing);
        }

        private IHttpActionResult GetErrorResult(IdentityResult result)
        {
            if (result == null)
            {
                return InternalServerError();
            }

            if (!result.Succeeded)
            {
                if (result.Errors != null)
                {
                    foreach (string error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }

                if (ModelState.IsValid)
                {
                    // No ModelState errors are available to send, so just return an empty BadRequest.
                    return BadRequest();
                }

                return BadRequest(ModelState);
            }

            return null;
        }

        // GET api/Finduser
        /// <summary>
        /// Find user, you have to provide your email and password. This API will help you to get userid, that require for making any POST call
        /// </summary>
        /// <param name="userEmail">user email</param>
        /// <param name="password">password</param>
        /// <returns></returns>
        [AllowAnonymous]
        [Route("FindUser")]
        [HttpGet]
        public async Task<CustomUserInfo> FindUser(string userEmail, string password)
        {
            using (AuthRepository _repo = new AuthRepository())
            {
                ApplicationUser user = await _repo.FindUser(userEmail.ToLower(), password);

                if (user == null)
                {
                    return null;
                }
                else
                {
                    return new CustomUserInfo
                    {
                        Email = user.Email,
                        UserId = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Location = user.Location,
                        ReputationCount = user.ReputationCount
                    };
                }
            }
        }
    }
}
