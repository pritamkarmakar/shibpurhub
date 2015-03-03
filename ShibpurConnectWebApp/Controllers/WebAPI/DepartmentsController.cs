using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using ShibpurConnectWebApp;
using ShibpurConnectWebApp.Models.WebAPI;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    public class DepartmentsController : ApiController
    {
        private ShibpurConnectDB db = new ShibpurConnectDB();

        // GET: api/Departments
        public IQueryable<Departments> GetDepartments()
        {
            return db.Departmentses;
        }

        // GET: api/Departments/electrical
        [ResponseType(typeof(Departments))]
        public IHttpActionResult GetDepartment(string name)
        {
            Departments departments = db.Departmentses.First(m => m.Department.ToLower() == name.ToLower());
            if (departments == null)
            {
                return NotFound();
            }

            return Ok(departments);
        }

        // PUT: api/Departments/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutDepartments(string id, Departments departments)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != departments.Id)
            {
                return BadRequest();
            }

            db.Entry(departments).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DepartmentsExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/Departments
        [ResponseType(typeof(Departments))]
        public IHttpActionResult PostDepartments(Departments departments)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if(departments == null)
                return BadRequest("Request body is null. Please send a valid Departments object");

            db.Departmentses.Add(departments);

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateException)
            {
                if (DepartmentsExists(departments.Id))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtRoute("DefaultApi", new { id = departments.Id }, departments);
        }

        // DELETE: api/Departments/5
        [ResponseType(typeof(Departments))]
        public IHttpActionResult DeleteDepartments(string id)
        {
            Departments departments = db.Departmentses.Find(id);
            if (departments == null)
            {
                return NotFound();
            }

            db.Departmentses.Remove(departments);
            db.SaveChanges();

            return Ok(departments);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool DepartmentsExists(string id)
        {
            return db.Departmentses.Count(e => e.Id == id) > 0;
        }
    }
}