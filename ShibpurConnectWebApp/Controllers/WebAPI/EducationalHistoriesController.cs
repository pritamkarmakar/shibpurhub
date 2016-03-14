using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;
using System.Web.Http.Results;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models.WebAPI;
using WebApi.OutputCache.V2;
using System.Collections.Generic;
using Nest;


namespace ShibpurConnectWebApp.Controllers.WebAPI
{

    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class EducationalHistoriesController : ApiController
    {
        private readonly MongoHelper<EducationalHistories> _mongoHelper;
        private ElasticSearchHelper _elasticSearchHelper;

        public EducationalHistoriesController()
        {
            _mongoHelper = new MongoHelper<EducationalHistories>();
            _elasticSearchHelper = new ElasticSearchHelper();
        }

        // GET: api/Educational/GetEducationalHistories?userId=
        /// <summary>
        /// Api to get all educational histories of a user
        /// </summary>
        /// <param name="userId">userId</param>
        /// <returns></returns>
        [ResponseType(typeof(EducationalHistories))]
        [CacheOutput(ServerTimeSpan = 864000, ExcludeQueryStringFromCacheKey = true, NoCache = true)]
        public async Task<IHttpActionResult> GetEducationalHistories(string userId)
        {
            // get the userID and verify userEmail is valid or not
            Helper.Helper helper = new Helper.Helper();
            var actionResult = helper.FindUserById(userId);
            var userInfo = await actionResult;

            if (userInfo == null)
                return NotFound();

            var educationalHistory = _mongoHelper.Collection.AsQueryable().Where(m => m.UserId == userInfo.Id).ToList();

            return Ok(educationalHistory);
        }

        /// <summary>
        /// Save a new educational history for a user
        /// </summary>
        /// <param name="educationalHistory"></param>
        /// <returns></returns>
        [Authorize]
        [ResponseType(typeof(EducationalHistories))]
        [InvalidateCacheOutput("SearchUsers", typeof(SearchController))]
        public async Task<IHttpActionResult> PostEducationalHistory(EducationalHistoriesDTO educationalHistory)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (educationalHistory == null)
                return BadRequest("Request body is null. Please send a valid EducationalHistory object");

            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
            var email = principal.Identity.Name;

            Helper.Helper helper = new Helper.Helper();
            var userResult = helper.FindUserByEmail(email);
            var userInfo = await userResult;
            if (userInfo == null)
            {
                return BadRequest("No UserId is found");
            }

            // read the corresponding department id
            DepartmentsController DC = new DepartmentsController();
            IHttpActionResult actionResult = DC.GetDepartment(educationalHistory.Department.ToString());
            var contentResult = actionResult as OkNegotiatedContentResult<Departments>;

            // if corresponding dept not present then return bad request
            if (contentResult == null)
                return BadRequest(String.Format("Supplied dept: " + "  {0} is not valid", educationalHistory.Department));

            // check if this is a BEC education
            bool isBecEducation = helper.CheckUniversityName(educationalHistory.UniversityName);

            // Create the EducationalHistories object to save in the database
            EducationalHistories educationalHistories = new EducationalHistories()
            {
                Department = educationalHistory.Department,
                GraduateYear = educationalHistory.GraduateYear,
                Id = ObjectId.GenerateNewId().ToString(),
                UniversityName = educationalHistory.UniversityName,
                UserId = userInfo.Id,
                IsBECEducation = isBecEducation
            };

            // save the entry in the database
            var result = _mongoHelper.Collection.Save(educationalHistories);

            // if mongo failed to save the data then send error
            if (!result.Ok)
                return InternalServerError();

            // invalidate the cache for the action those will get impacted due to this new answer post
            var cache = Configuration.CacheOutputConfiguration().GetCacheOutputProvider(Request);

            // remove the in-memory cache for this user
            CacheManager.RemoveCacheData("completeuserprofile-" + userInfo.Id);

            // invalidate the getemploymenthistories api call for this user
            cache.RemoveStartsWith("educationalhistories-geteducationalhistories-userId=" + userInfo.Id);
            cache.RemoveStartsWith("profile-getprofilebyuserid-userId=" + userInfo.Id);

            // remove in-memory cache
            CacheManager.RemoveCacheData("completeuserprofile-" + userInfo.Id);

            // add the new entry in elastic search
            var client = _elasticSearchHelper.ElasticClient();
            client.Index(new EducationalHistories()
            {
                Id = educationalHistories.Id,
                UserId = educationalHistories.UserId,
                Department = educationalHistories.Department,
                GraduateYear = educationalHistories.GraduateYear,
                UniversityName = educationalHistories.UniversityName,
                IsBECEducation = educationalHistories.IsBECEducation
            });

            return CreatedAtRoute("DefaultApi", new { id = educationalHistories.Id }, educationalHistories);
        }

        /// <summary>
        /// API to update an educational history record of an user
        /// </summary>
        /// <param name="educationalHistories">EducationalHistories object</param>
        /// <returns></returns>
        [Authorize]
        public async Task<IHttpActionResult> EditEducationalHistory(EducationalHistories educationalHistories)
        {
            if(educationalHistories == null)
                return BadRequest("null educationalHistories supplied");
            if(educationalHistories.Id == null)
                return BadRequest("educationalHistories id can't be null");
            if(educationalHistories.Department == "--Select Department--")
                return BadRequest("Invalid department name");

            EducationalHistories educationHistory = _mongoHelper.Collection.AsQueryable().Where(m => m.Id == educationalHistories.Id).ToList()[0];
            if (educationHistory == null)
            {
                return BadRequest("Invalid educational history id");
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
            if (educationHistory.UserId != userInfo.Id)
            {
                return BadRequest("You are not allowed to edit this record");
            }

            // check if this is a BEC education
            bool isBecEducation = helper.CheckUniversityName(educationalHistories.UniversityName);

            // update the educational history record with whatever user send
            educationHistory.Department = educationalHistories.Department;
            educationHistory.GraduateYear = educationalHistories.GraduateYear;
            educationHistory.UniversityName = educationalHistories.UniversityName;
            educationHistory.IsBECEducation = isBecEducation;

            // save the record in database
            try
            {
                var result = _mongoHelper.Collection.Save(educationHistory);

                // if mongo failed to save the data then send error
                if (!result.Ok)
                    return InternalServerError();

                // update same educational history details in elastic search
                var client = _elasticSearchHelper.ElasticClient();
                dynamic updateUser = new System.Dynamic.ExpandoObject();

                var response = client.Update<EducationalHistories, object>(u => u
                    .Index("my_index")
                    .Id(educationHistory.Id)
                    .Type("educationalhistories")
                    .Doc(new
                    {
                       Department = educationalHistories.Department,
                       GraduateYear = educationalHistories.GraduateYear,
                       UniversityName = educationalHistories.UniversityName,
                       IsBECEducation = isBecEducation
                    }));

                // remove the in-memory cache for this user
                CacheManager.RemoveCacheData("completeuserprofile-" + userInfo.Id);

                // invalidate the cache for the action those will get impacted due to this new answer post
                var cache = Configuration.CacheOutputConfiguration().GetCacheOutputProvider(Request);

                // invalidate the getemploymenthistories api call for this user
                cache.RemoveStartsWith("educationalhistories-geteducationalhistories-userId=" + userInfo.Id);
                cache.RemoveStartsWith("profile-getprofilebyuserid-userId=" + userInfo.Id);

                return CreatedAtRoute("DefaultApi", new { id = educationHistory.Id }, educationHistory);
            }
            catch (MongoConnectionException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// API to update an educational history record of an user
        /// </summary>
        /// <param name="educationalHistories">EducationalHistories object</param>
        /// <returns></returns>
        [Authorize]
        public async Task<IHttpActionResult> DeleteEducationalHistory(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("null educationId supplied");

            EducationalHistories educationHistory = _mongoHelper.Collection.AsQueryable().Where(m => m.Id == id).ToList()[0];
            if (educationHistory == null)
            {
                return BadRequest("Invalid educational history id");
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
            if (educationHistory.UserId != userInfo.Id)
            {
                return BadRequest("You are not allowed to edit this record");
            }

            // delete the record in database
            try
            {
                // delete from elastic search
                var client = _elasticSearchHelper.ElasticClient();
                var response = client.Delete("my_index", "educationalhistories", id);

                // delete from mongodb
                var result = _mongoHelper.Collection.Remove(Query.EQ("_id", new BsonObjectId(new ObjectId(id))), RemoveFlags.Single);

                // if mongo failed to save the data then send error
                if (!result.Ok)
                    return InternalServerError();

                // invalidate the cache for the action those will get impacted due to this new answer post
                var cache = Configuration.CacheOutputConfiguration().GetCacheOutputProvider(Request);

                // invalidate the getemploymenthistories api call for this user
                cache.RemoveStartsWith("educationalhistories-geteducationalhistories-userId=" + userInfo.Id);
                cache.RemoveStartsWith("profile-getprofilebyuserid-userId=" + userInfo.Id);

                return CreatedAtRoute("DefaultApi", new { id = educationHistory.Id }, educationHistory);
            }
            catch (MongoConnectionException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// This is not a public API. We will use it do background operation with Hangfire. 
        /// This will delete all the educatioanl histories for a particular user
        /// </summary>
        /// <param name="userId">userId for whom we want to delete all records</param>
        /// <returns></returns>
        [Authorize]
        internal async void DeleteAllEducationalHistories(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("null userId supplied");

            try
            {
                // get the list of documents that will be deleted, we need the ids for doing the clean work in elastic search
                var documents = _mongoHelper.Collection.Find(Query.EQ("UserId", userId));

                // delete corresponding records from elastic search
                var client = _elasticSearchHelper.ElasticClient();
                dynamic updateUser = new System.Dynamic.ExpandoObject();

                foreach (var document in documents)
                {
                    var response = client.Delete("my_index", "educationalhistories", document.Id);
                }

                // delete all the records from the database
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