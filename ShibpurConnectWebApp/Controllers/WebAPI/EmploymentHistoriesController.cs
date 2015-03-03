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
    public class EmploymentHistoriesController : ApiController
    {
        private ShibpurConnectDB db = new ShibpurConnectDB();

        // GET: api/EmploymentHistories
        public IQueryable<EmploymentHistory> GetEmploymentHistories()
        {
            return db.EmploymentHistories;
        }

        // GET: api/EmploymentHistories/5
        [ResponseType(typeof(EmploymentHistory))]
        public IHttpActionResult GetEmploymentHistory(string id)
        {
            EmploymentHistory employmentHistory = db.EmploymentHistories.Find(id);
            if (employmentHistory == null)
            {
                return NotFound();
            }

            return Ok(employmentHistory);
        }

        // PUT: api/EmploymentHistories/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutEmploymentHistory(string id, EmploymentHistory employmentHistory)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != employmentHistory.Id)
            {
                return BadRequest();
            }

            db.Entry(employmentHistory).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmploymentHistoryExists(id))
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

        // POST: api/EmploymentHistories
        [ResponseType(typeof(EmploymentHistory))]
        public IHttpActionResult PostEmploymentHistory(EmploymentHistory employmentHistory)
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
            employmentHistory.Id = Guid.NewGuid().ToString();

            db.EmploymentHistories.Add(employmentHistory);

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateException)
            {
                if (EmploymentHistoryExists(employmentHistory.Id))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtRoute("DefaultApi", new { id = employmentHistory.Id }, employmentHistory);
        }

        // DELETE: api/EmploymentHistories/5
        [ResponseType(typeof(EmploymentHistory))]
        public IHttpActionResult DeleteEmploymentHistory(string id)
        {
            EmploymentHistory employmentHistory = db.EmploymentHistories.Find(id);
            if (employmentHistory == null)
            {
                return NotFound();
            }

            db.EmploymentHistories.Remove(employmentHistory);
            db.SaveChanges();

            return Ok(employmentHistory);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool EmploymentHistoryExists(string id)
        {
            return db.EmploymentHistories.Count(e => e.Id == id) > 0;
        }
    }
}