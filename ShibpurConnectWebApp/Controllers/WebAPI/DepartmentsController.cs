using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Description;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models.WebAPI;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    public class DepartmentsController : ApiController
    {
        private readonly MongoHelper<Departments> _mongoHelper;

        public DepartmentsController()
        {
            _mongoHelper = new MongoHelper<Departments>();
        }


        // GET: api/Departments
        public IHttpActionResult GetDepartments()
        {
            try
            {
                return Ok(_mongoHelper.Collection.FindAll().ToList());
            }
            catch(MongoDB.Driver.MongoConnectionException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET: api/Departments/electrical
        [ResponseType(typeof(Departments))]
        public IHttpActionResult GetDepartment(string name)
        {
            var departments = _mongoHelper.Collection.AsQueryable().Where(m => m.DepartmentName.ToLower() == name.ToLower());
            if (departments.Count() == 0)
            {
                return NotFound();
            }

            return Ok(departments.First());
        }     

        // POST: api/Departments
        [ResponseType(typeof(Departments))]
        public IHttpActionResult PostDepartments(Departments departments)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (departments == null)
                return BadRequest("Request body is null. Please send a valid Departments object");

            // add the id
            departments.Id = ObjectId.GenerateNewId();

            var result = _mongoHelper.Collection.Save(departments);

            // if mongo failed to save the data then send error
            if (!result.Ok)
                return InternalServerError();

           return CreatedAtRoute("DefaultApi", new { id = departments.Id }, departments);
        }       
    }
}