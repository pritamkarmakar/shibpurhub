using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;
using System.Web.Http.Results;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models.WebAPI;
using WebApi.OutputCache.V2;

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

        // GET: api/EducationalHistories
        /// <summary>
        /// Get education histories for all users
        /// </summary>
        /// <returns></returns>
        public IList GetEducationalHistory()
        {
            return _mongoHelper.Collection.FindAll().ToList();
        }

        // GET: api/Educational/GetEducationalHistories?useremail=
        /// <summary>
        /// Api to get all educational histories of a user
        /// </summary>
        /// <param name="userEmail">user email</param>
        /// <returns></returns>
        [ResponseType(typeof(EducationalHistories))]
        public async Task<IHttpActionResult> GetEducationalHistories(string userEmail)
        {
            // get the userID and verify userEmail is valid or not
            Helper.Helper helper = new Helper.Helper();
            var actionResult = helper.FindUserByEmail(userEmail);
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
        [ResponseType(typeof(EducationalHistories))]
        [InvalidateCacheOutput("SearchUsers", typeof(SearchController))]
        public IHttpActionResult PostEducationalHistory(EducationalHistories educationalHistory)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (educationalHistory == null)
                return BadRequest("Request body is null. Please send a valid EducationalHistory object");

            // read the corresponding department id
            DepartmentsController DC = new DepartmentsController();
            IHttpActionResult actionResult = DC.GetDepartment(educationalHistory.Department.ToString());
            var contentResult = actionResult as OkNegotiatedContentResult<Departments>;

            // if corresponding dept not present then return bad request
            if (contentResult == null)
                return BadRequest(String.Format("Supplied dept: " + "  {0} is not valid", educationalHistory.Department));

            // save the department id into the educationalHistory object
            //educationalHistory.Department = contentResult.Content.Id;
            
            // generate the unique id for this new record
            educationalHistory.Id = ObjectId.GenerateNewId();

            // save the entry in the database
            var result = _mongoHelper.Collection.Save(educationalHistory);

            // if mongo failed to save the data then send error
            if (!result.Ok)
                return InternalServerError();

            // add the new entry in elastic search
            var client = _elasticSearchHelper.ElasticClient();
            client.Index(new EducationalHistories()
            {
               Id = educationalHistory.Id,
               UserId = educationalHistory.UserId,
               Department = educationalHistory.Department,
               GraduateYear = educationalHistory.GraduateYear,
               UniversityName = educationalHistory.UniversityName               
            });

            return CreatedAtRoute("DefaultApi", new { id = educationalHistory.Id }, educationalHistory);
        }

        // DELETE: api/EducationalHistories/5
        /// <summary>
        /// Delete a education history record by its id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ResponseType(typeof(EducationalHistories))]
        public IHttpActionResult DeleteEducationalHistory(string id)
        {
            EducationalHistories educationalHistory = _mongoHelper.Collection.FindOneById(id);
            if (educationalHistory == null)
            {
                return NotFound();
            }

            _mongoHelper.Collection.Remove(Query.EQ("Id", id));

            return Ok(educationalHistory);
        }
    }
}