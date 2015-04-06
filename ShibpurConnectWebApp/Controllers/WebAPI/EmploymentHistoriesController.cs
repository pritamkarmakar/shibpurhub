using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using MongoDB.Driver.Builders;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models.WebAPI;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

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

        // GET: api/EmploymentHistories
        public IList<EmploymentHistories> GetEmploymentHistories()
        {
            return _mongoHelper.Collection.FindAll().ToList();
        }

        // GET: api/Employment/getemploymenthistory?useremail=
        // this api will return all employment histories of a user
        [ResponseType(typeof(EmploymentHistories))]
        public async Task<IHttpActionResult> GetEmploymentHistories(string userEmail)
        {
            // get the userID and verify userEmail is valid or not
            Helper.Helper helper = new Helper.Helper();
            var actionResult = helper.FindUserByEmail(userEmail);
            var userInfo = await actionResult;

            if (userInfo == null)
                return NotFound();

            var employmentHistory = _mongoHelper.Collection.AsQueryable().Where(m => m.UserId == userInfo.UserId).ToList();
            if (employmentHistory.Count == 0)
            {
                return null;
            }

            return Ok(employmentHistory);
        }

        // PUT: api/EmploymentHistories/5
        //[ResponseType(typeof(void))]
        //public IHttpActionResult PutEmploymentHistory(string id, EmploymentHistory employmentHistory)
        //{
        //    //if (!ModelState.IsValid)
        //    //{
        //    //    return BadRequest(ModelState);
        //    //}

        //    //if (id != employmentHistory.Id)
        //    //{
        //    //    return BadRequest();
        //    //}

        //    //db.Entry(employmentHistory).State = EntityState.Modified;

        //    //try
        //    //{
        //    //    db.SaveChanges();
        //    //}
        //    //catch (DbUpdateConcurrencyException)
        //    //{
        //    //    if (!EmploymentHistoryExists(id))
        //    //    {
        //    //        return NotFound();
        //    //    }
        //    //    else
        //    //    {
        //    //        throw;
        //    //    }
        //    //}

        //    //return StatusCode(HttpStatusCode.NoContent);
        //}

        // POST: api/EmploymentHistories
        [ResponseType(typeof(EmploymentHistories))]
        public IHttpActionResult PostEmploymentHistory(EmploymentHistories employmentHistory)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (employmentHistory == null)
            {
                return BadRequest("Request body is null. Please send a valid EmploymentHistory object");
            }

            // add the guid id for this record
            employmentHistory.Id = ObjectId.GenerateNewId();

            // save the entry in database
            var result = _mongoHelper.Collection.Save(employmentHistory);

            // if mongo failed to save the data then send error
            if (!result.Ok)
                return InternalServerError();
            
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
        [ResponseType(typeof(EmploymentHistories))]
        public IHttpActionResult DeleteEmploymentHistory(string id)
        {
            EmploymentHistories employmentHistory = _mongoHelper.Collection.FindOneById(id);
            if (employmentHistory == null)
            {
                return NotFound();
            }

            _mongoHelper.Collection.Remove(Query.EQ("Id", id));

            return Ok(employmentHistory);
        }

        //protected override void Dispose(bool disposing)
        //{
        //    if (disposing)
        //    {
        //        db.Dispose();
        //    }
        //    base.Dispose(disposing);
        //}

        //private bool EmploymentHistoryExists(string id)
        //{
        //    return db.EmploymentHistories.Count(e => e.Id == id) > 0;
        //}
    }
}