using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;
using System.Web.Http.Results;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models.WebAPI;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class EducationalHistoriesController : ApiController
    {
        private readonly MongoHelper<EducationalHistories> _mongoHelper;

        public EducationalHistoriesController()
        {
            _mongoHelper = new MongoHelper<EducationalHistories>();
        }

        // GET: api/EducationalHistories
        public IList GetEducationalHistory()
        {
            return _mongoHelper.Collection.FindAll().ToList();
        }

        // GET: api/EducationalHistories/userid
        [ResponseType(typeof(EducationalHistories))]
        public IHttpActionResult GetEducationalHistory(string userId)
        {
            var educationalHistory = _mongoHelper.Collection.AsQueryable().Where(m => m.UserId == userId).ToList();
            if (educationalHistory.Count == 0)
            {
                return null;
            }

            return Ok(educationalHistory);
        }

        //// PUT: api/EducationalHistories/5
        //[ResponseType(typeof(void))]
        //public IHttpActionResult PutEducationalHistory(string id, EducationalHistory educationalHistory)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    if (id != educationalHistory.Id)
        //    {
        //        return BadRequest();
        //    }

        //    db.Entry(educationalHistory).State = EntityState.Modified;

        //    try
        //    {
        //        db.SaveChanges();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!EducationalHistoryExists(id))
        //        {
        //            return NotFound();
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }

        //    return StatusCode(HttpStatusCode.NoContent);
        //}

        // POST: api/EducationalHistories
        [ResponseType(typeof(EducationalHistories))]
        public IHttpActionResult PostEducationalHistory(EducationalHistories educationalHistory)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (educationalHistory == null)
                return BadRequest("Request body is null. Please send a valid EducationalHistory object");

            // add the object id for this history
            educationalHistory.Id = ObjectId.GenerateNewId();

            // read the corresponding department id
            DepartmentsController DC = new DepartmentsController();
            IHttpActionResult actionResult = DC.GetDepartment(educationalHistory.Department.ToString());
            var contentResult = actionResult as OkNegotiatedContentResult<Departments>;

            // if corresponding dept not present then return bad request
            if (contentResult == null)
                return BadRequest(String.Format("Supplied dept: " + "  {0} is not valid", educationalHistory.Department));

            // save the department id into the educationalHistory object
            //educationalHistory.Department = contentResult.Content.Id;

            // save the entry in the database
            _mongoHelper.Collection.Save(educationalHistory);

            return CreatedAtRoute("DefaultApi", new { id = educationalHistory.Id }, educationalHistory);
        }

        // DELETE: api/EducationalHistories/5
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

        //protected override void Dispose(bool disposing)
        //{
        //    if (disposing)
        //    {
        //        db.Dispose();
        //    }
        //    base.Dispose(disposing);
        //}

        //private bool EducationalHistoryExists(string id)
        //{
        //    return db.EducationalHistories.Count(e => e.Id == id) > 0;
        //}
    }
}