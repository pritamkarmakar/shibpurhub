using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using MongoDB.Driver.Builders;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models.WebAPI;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using WebApi.OutputCache.V2;
using Nest;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    public class EmploymentHistoriesController : ApiController
    {
        private readonly MongoHelper<EmploymentHistories> _mongoHelper;
        private ElasticSearchHelper _elasticSearchHelper;

        public EmploymentHistoriesController()
        {
            _mongoHelper = new MongoHelper<EmploymentHistories>();
            _elasticSearchHelper = new ElasticSearchHelper();
        }

        // GET: api/Employment/getemploymenthistory?useremail=
        // this api will return all employment histories of a user
        [ResponseType(typeof(EmploymentHistories))]
        [CacheOutput(ServerTimeSpan = 864000, ExcludeQueryStringFromCacheKey = true, NoCache = true)]
        public async Task<IHttpActionResult> GetEmploymentHistories(string userId)
        {
            // get the userID and verify userEmail is valid or not
            Helper.Helper helper = new Helper.Helper();
            var actionResult = helper.FindUserById(userId);
            var userInfo = await actionResult;

            if (userInfo == null)
                return NotFound();

            var employmentHistory = _mongoHelper.Collection.AsQueryable().Where(m => m.UserId == userInfo.Id).ToList();            

            return Ok(employmentHistory);
        }

        [Authorize]
        [ResponseType(typeof(EmploymentHistories))]
        [InvalidateCacheOutput("SearchUsers", typeof(SearchController))]
        public async Task<IHttpActionResult> PostEmploymentHistory(EmploymentHistories employmentHistory)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (employmentHistory == null)
            {
                return BadRequest("Request body is null. Please send a valid EmploymentHistory object");
            }

            // validate employment from date is older than to date
            if(employmentHistory.From > employmentHistory.To)
                return BadRequest("'Employment start date' can't be older than 'Employment end date'");

            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
            var email = principal.Identity.Name;

            Helper.Helper helper = new Helper.Helper();
            var userResult = helper.FindUserByEmail(email);
            var userInfo = await userResult;
            if (userInfo == null)
            {
                return BadRequest("No UserId is found");
            }

            // add the guid id for this record
            employmentHistory.Id = ObjectId.GenerateNewId().ToString();

            // save the entry in database
            var result = _mongoHelper.Collection.Save(employmentHistory);

            // if mongo failed to save the data then send error
            if (!result.Ok)
                return InternalServerError();

            // remove the in-memory cache for this user
            CacheManager.RemoveCacheData("completeuserprofile-" + userInfo.Id);

            // invalidate the cache for the action those will get impacted due to this new answer post
            var cache = Configuration.CacheOutputConfiguration().GetCacheOutputProvider(Request);

            // invalidate the getemploymenthistories api call for this user
            cache.RemoveStartsWith("employmenthistories-getemploymenthistories-userId=" + userInfo.Id);
            cache.RemoveStartsWith("profile-getprofilebyuserid-userId=" + userInfo.Id);

            // remove in-memory cache
            CacheManager.RemoveCacheData("completeuserprofile-" + userInfo.Id);

            // add the new entry in elastic search
            var client = _elasticSearchHelper.ElasticClient();
            client.Index(new EmploymentHistories()
                {
                    Id = employmentHistory.Id,
                    CompanyName = employmentHistory.CompanyName,
                    Location = employmentHistory.Location,
                    Title = employmentHistory.Title,
                    UserId = employmentHistory.UserId
                });

            return CreatedAtRoute("DefaultApi", new { id = employmentHistory.Id }, employmentHistory);
        }

        // DELETE: api/EmploymentHistories/5
        [Authorize]
        [ResponseType(typeof(EmploymentHistories))]
        public async Task<IHttpActionResult> DeleteEmploymentHistory(string id)
        {
            EmploymentHistories employmentHistory = _mongoHelper.Collection.AsQueryable().Where(m => m.Id == id).ToList()[0];
            if (employmentHistory == null)
            {
                return NotFound();
            }

            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
            var email = principal.Identity.Name;

            Helper.Helper helper = new Helper.Helper();
            var userResult = helper.FindUserByEmail(email);
            var userInfo = await userResult;
            if (userInfo == null)
            {
                return BadRequest("No UserId is found");
            }

            // make sure the user requesting this is the owner of the database record. We will not allow one user to update record for any other user
            if (employmentHistory.UserId != userInfo.Id)
            {
                return BadRequest("You are not allowed to edit this record");
            }

            // delete from elastic search
            var client = _elasticSearchHelper.ElasticClient();
            var response = client.Delete("my_index", "employmenthistories", id);

            var result = _mongoHelper.Collection.Remove(Query.EQ("_id", new BsonObjectId(new ObjectId(id))), RemoveFlags.Single);
            // invalidate the cache for the action those will get impacted due to this new answer post
            var cache = Configuration.CacheOutputConfiguration().GetCacheOutputProvider(Request);


            // invalidate the getemploymenthistories api call for this user
            cache.RemoveStartsWith("employmenthistories-getemploymenthistories-userId=" + userInfo.Id);
            cache.RemoveStartsWith("profile-getprofilebyuserid-userId=" + userInfo.Id);

            return Ok(employmentHistory);
        }

        /// <summary>
        /// API to update employment history record of an user
        /// </summary>
        /// <param name="employmentHistories">EmploymentHistories object</param>
        /// <returns></returns>
        [Authorize]
        public async Task<IHttpActionResult> EditEmploymentHistory(EmploymentHistories employmentHistories)
        {
            EmploymentHistories employmentHistory = _mongoHelper.Collection.AsQueryable().Where(m => m.Id == employmentHistories.Id).ToList()[0];
            if (employmentHistory == null)
            {
                return BadRequest("Invalid employment history id");
            }

            // validate employment from date is older than to date
            if (employmentHistory.From > employmentHistory.To)
                return BadRequest("'Employment start date' can't be older than 'Employment end date'");

            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
            var email = principal.Identity.Name;

            Helper.Helper helper = new Helper.Helper();
            var userResult = helper.FindUserByEmail(email);
            var userInfo = await userResult;
            if (userInfo == null)
            {
                return BadRequest("No UserId is found");
            }

            // make sure the user requesting this is the owner of the database record. We will not allow one user to update record for any other user
            if (employmentHistory.UserId != userInfo.Id)
            {
                return BadRequest("You are not allowed to edit this record");
            }

            // update the employment history record with whatever user send
            employmentHistory.CompanyName = employmentHistories.CompanyName;
            employmentHistory.Location = employmentHistories.Location;
            employmentHistory.Title = employmentHistories.Title;
            employmentHistory.From = employmentHistories.From;
            employmentHistory.To = employmentHistories.To;

            // save the record in database
            try
            {
                var result= _mongoHelper.Collection.Save(employmentHistory);

                // if mongo failed to save the data then send error
                if (!result.Ok)
                    return InternalServerError();

                // remove the in-memory cache for this user
                CacheManager.RemoveCacheData("completeuserprofile-" + userInfo.Id);

                // invalidate the cache for the action those will get impacted due to this new answer post
                var cache = Configuration.CacheOutputConfiguration().GetCacheOutputProvider(Request);

                // invalidate the getemploymenthistories api call for this user
                cache.RemoveStartsWith("employmenthistories-getemploymenthistories-userId=" + userInfo.Id);
                cache.RemoveStartsWith("profile-getprofilebyuserid-userId=" + userInfo.Id);

                return CreatedAtRoute("DefaultApi", new { id = employmentHistories.Id }, employmentHistory);
            }
            catch (MongoConnectionException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        internal async void DeleteAllEmploymentHistories(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("null userId supplied");            

            // delete all the records in database
            try
            {
                // get the list of documents that will be deleted, we need the ids for doing the clean work in elastic search
                var documents = _mongoHelper.Collection.Find(Query.EQ("UserId", userId));

                // delete corresponding records from elastic search
                var client = _elasticSearchHelper.ElasticClient();
                dynamic updateUser = new System.Dynamic.ExpandoObject();

                foreach (var document in documents)
                {
                    var response = client.Delete("my_index", "employmenthistories", document.Id);
                }

                // delete from mongodb
                var result = _mongoHelper.Collection.Remove(Query.EQ("UserId", userId));

                // if mongo failed to save the data then send error
                if (!result.Ok)
                    throw new MongoException("failed to delete the educational histories");
            }
            catch (MongoConnectionException ex)
            {
                throw new MongoException("failed to delete the educational histories");
            }
        }
    }
}