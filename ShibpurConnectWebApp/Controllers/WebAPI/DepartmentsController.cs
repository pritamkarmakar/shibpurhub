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
        public IList<Departments> GetDepartments()
        {
            return _mongoHelper.Collection.FindAll().ToList();
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

        //// PUT: api/Departments/5
        //[ResponseType(typeof(void))]
        //public IHttpActionResult PutDepartments(string id, Departments departments)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    if (id != departments.Id)
        //    {
        //        return BadRequest();
        //    }

        //    db.Entry(departments).State = EntityState.Modified;

        //    try
        //    {
        //        db.SaveChanges();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!DepartmentsExists(id))
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

            _mongoHelper.Collection.Save(departments);

           return CreatedAtRoute("DefaultApi", new { id = departments.Id }, departments);
        }

        //// DELETE: api/Departments/5
        //[ResponseType(typeof(Departments))]
        //public IHttpActionResult DeleteDepartments(string id)
        //{
        //    Departments departments = db.Departmentses.Find(id);
        //    if (departments == null)
        //    {
        //        return NotFound();
        //    }

        //    db.Departmentses.Remove(departments);
        //    db.SaveChanges();

        //    return Ok(departments);
        //}

        //protected override void Dispose(bool disposing)
        //{
        //    if (disposing)
        //    {
        //        db.Dispose();
        //    }
        //    base.Dispose(disposing);
        //}

        //private bool DepartmentsExists(string id)
        //{
        //    return db.Departmentses.Count(e => e.Id == id) > 0;
        //}
    }
}