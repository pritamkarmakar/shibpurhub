using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models.WebAPI;
using System.Web.Http.Results;
using WebApi.OutputCache.V2;
using System.Threading.Tasks;
using System.Security.Claims;

using System.Net.Http;
using ShibpurConnectWebApp.Providers;
using Microsoft.AspNet.Identity;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class TagsController : ApiController
    {
        private MongoHelper<Categories> _mongoHelper;

        public TagsController()
        {
            _mongoHelper = new MongoHelper<Categories>();
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

        /// <summary>
        /// Get all existing categories
        /// </summary>
        /// <returns></returns>
        public IHttpActionResult GetTags()
        {
            try
            {
                return Ok(_mongoHelper.Collection.FindAll().ToList());
            }
            catch (MongoDB.Driver.MongoConnectionException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get details about a specific category
        /// </summary>
        /// <param name="categoryName">category name</param>
        /// <returns></returns>
        [ResponseType(typeof(Categories))]
       [CacheOutput(ServerTimeSpan = 864000, ExcludeQueryStringFromCacheKey = true, NoCache = true)]
        public async Task<IHttpActionResult> GetTag(string categoryName)
        {
            Categories category = null;

            try
            {
                category = _mongoHelper.Collection.AsQueryable().Where(m => m.CategoryName.ToLower() == categoryName.Trim().ToLower()).ToList().Count == 0 ? null : _mongoHelper.Collection.AsQueryable().Where(m => m.CategoryName.ToLower() == categoryName.Trim().ToLower()).ToList()[0];
            }
            catch(Exception ex)
            {
                return InternalServerError(ex);                
            }

            if (category == null)
            {
                return BadRequest("category not found" + categoryName);
            }

            return Ok(category);
        }
       
        /// <summary>
        /// Get tags with question count
        /// we use this api in http://shibpur.azurewebsites.net/Tags page
        /// </summary>
        /// <param name="count">no of categories that we want</param>
        /// <returns></returns>
        [CacheOutput(ServerTimeSpan = 864000, ExcludeQueryStringFromCacheKey = true, NoCache = true)]
        public IHttpActionResult GetPopularTags(int count)
        {
            List<CategoryCloud> categoryCloud = new List<CategoryCloud>();
            
            foreach(Categories catg in _mongoHelper.Collection.FindAll())
            {
                // retrieve the total questions tagged with this category
                CategoryTaggingController ctc = new CategoryTaggingController();
                IHttpActionResult actionResult = ctc.GetQuestionCount(catg.CategoryId.ToString());
                var result = actionResult as OkNegotiatedContentResult<int>;

                categoryCloud.Add(new CategoryCloud
                    {
                        CategoryId = catg.CategoryId,
                        CategoryName = catg.CategoryName,
                        HasPublished = catg.HasPublished,
                        QuestionCount = result.Content
                    });
            }

            return Ok(categoryCloud.OrderByDescending(m => m.QuestionCount).ToList().Take(count));
        }

        /// <summary>
        /// Get all the tags by popularity (no of questions tagged to each tag)
        /// We are using this api in the Tag>Index page
        /// </summary>
        /// <returns></returns>
        [CacheOutput(ServerTimeSpan = 864000, ExcludeQueryStringFromCacheKey = true, NoCache = true)]
        public IHttpActionResult GetPopularTags()
        {
            List<CategoryCloud> categoryCloud = new List<CategoryCloud>();

            foreach (Categories catg in _mongoHelper.Collection.FindAll())
            {
                // retrieve the total questions tagged with this category
                CategoryTaggingController ctc = new CategoryTaggingController();
                IHttpActionResult actionResult = ctc.GetQuestionCount(catg.CategoryId.ToString());
                var result = actionResult as OkNegotiatedContentResult<int>;

                categoryCloud.Add(new CategoryCloud
                {
                    CategoryId = catg.CategoryId,
                    CategoryName = catg.CategoryName,
                    HasPublished = catg.HasPublished,
                    QuestionCount = result.Content
                });
            }

            return Ok(categoryCloud.OrderByDescending(m => m.QuestionCount).ToList());
        }

        /// <summary>
        /// API to follow a new tag
        /// </summary>
        /// <param name="tagName">new tag to follow</param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        [InvalidateCacheOutput("SearchUsers", typeof(SearchController))]        
        public async Task<IHttpActionResult> FollowNewTag(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
            {
                ModelState.AddModelError("", "categoryName can't be null or empty string");
                return BadRequest(ModelState);
            }

            // check if this is a valid tag that available in database collection
            var actionResult = await GetTag(tagName);
            var contentResult = actionResult as OkNegotiatedContentResult<Categories>;
            if (contentResult == null)
            {
                ModelState.AddModelError("", "No such category found");
                return BadRequest(ModelState);
            }

            // get user identity from the supplied bearer token
            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
            var email = principal.Identity.Name;

            Helper.Helper helper = new Helper.Helper();
            var userResult = helper.FindUserByEmail(email);
            var userInfo = await userResult;
            if (userInfo == null)
            {
                ModelState.AddModelError("", "No user is found");
                return BadRequest(ModelState);
            }
            
            //process this new tag only if it is not available in user profile, otherwise return
            if(userInfo.Tags != null && userInfo.Tags.Contains(tagName))
            {
                return Ok("{'status': 'tag already in user profile'}");
            }

            // if we are here that means this tag is not in user profile so we have to add it
            AuthRepository _repo = new AuthRepository();
            IdentityResult result = await _repo.FollowNewTag(userInfo.Id, tagName);
            IHttpActionResult errorResult = GetErrorResult(result);
            if (errorResult != null)
            {
                return errorResult;
            }

            // invalidate the cache for the action those will get impacted due to this new answer post
            var cache = Configuration.CacheOutputConfiguration().GetCacheOutputProvider(Request);
            // invalidate the findusertags api 
            cache.RemoveStartsWith("tags-findusertags-userId=" + userInfo.Id);

            return Ok("{'status': 'success'}");
        }

        /// <summary>
        /// API to unfollow a tag
        /// </summary>
        /// <param name="tagName">tag to unfollow</param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        [InvalidateCacheOutput("SearchUsers", typeof(SearchController))]      
        public async Task<IHttpActionResult> UnfollowTag(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
            {
                ModelState.AddModelError("", "categoryName can't be null or empty string");
                return BadRequest(ModelState);
            }

            // check if this is a valid tag that available in database collection
            var actionResult = await GetTag(tagName);
            var contentResult = actionResult as OkNegotiatedContentResult<Categories>;
            if (contentResult == null)
            {
                ModelState.AddModelError("", "No such category found");
                return BadRequest(ModelState);
            }

            // get user identity from the supplied bearer token
            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
            var email = principal.Identity.Name;

            Helper.Helper helper = new Helper.Helper();
            var userResult = helper.FindUserByEmail(email);
            var userInfo = await userResult;
            if (userInfo == null)
            {
                ModelState.AddModelError("", "No user is found");
                return BadRequest(ModelState);
            }

            //process this new tag only if it is not available in user profile, otherwise return
            if (userInfo.Tags.Contains(tagName))
            {       
                AuthRepository _repo = new AuthRepository();
                IdentityResult result = await _repo.UnfollowTag(userInfo.Id, tagName);
                IHttpActionResult errorResult = GetErrorResult(result);
                if (errorResult != null)
                {
                    return errorResult;
                }

                // invalidate the cache for the action those will get impacted due to this new answer post
                var cache = Configuration.CacheOutputConfiguration().GetCacheOutputProvider(Request);
                // invalidate the findusertags api 
                cache.RemoveStartsWith("tags-findusertags-userId=" + userInfo.Id);
            }

            return Ok("{'status': 'success'}");
        }


        /// <summary>
        /// API to return all tags that one user is following
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [CacheOutput(ServerTimeSpan = 864000, ExcludeQueryStringFromCacheKey = true, NoCache = true)]
        public async Task<IHttpActionResult> FindUserTags(string userId)
        {
            Helper.Helper helper = new Helper.Helper();
            var userResult = helper.FindUserById(userId);
            var userInfo = await userResult;
            if (userInfo == null)
            {
                ModelState.AddModelError("",
                    "No user is found with the provided bearer token. Please relogin or try again. Or API user please send valid bearer token");
                return BadRequest(ModelState);
            }

            else
                return Ok(userInfo.Tags);
        }
        
        /// <summary>
        /// Delete a category
        /// </summary>
        /// <param name="categoryName">category name</param>
        /// <returns></returns>
        [ResponseType(typeof(Categories))]
        public IHttpActionResult DeleteCategories(string categoryName)
        {
            Categories categories = _mongoHelper.Collection.AsQueryable().Where(m => m.CategoryName == categoryName).ToList().Count == 0 ? null : _mongoHelper.Collection.AsQueryable().Where(m => m.CategoryName == categoryName).ToList()[0];
            if (categories == null)
            {
                return NotFound();
            }

            _mongoHelper.Collection.Remove(Query.EQ("Categoryname", categoryName));
            
            return Ok(categories);
        }
       
    }
}