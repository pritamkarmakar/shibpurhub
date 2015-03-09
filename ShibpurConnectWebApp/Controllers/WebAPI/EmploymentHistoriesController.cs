﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

        public EmploymentHistoriesController()
        {
            _mongoHelper = new MongoHelper<EmploymentHistories>();
        }

        // GET: api/EmploymentHistories
        public IList<EmploymentHistories> GetEmploymentHistories()
        {
            return _mongoHelper.Collection.FindAll().ToList();
        }

        // GET: api/EmploymentHistories/5
        //[ResponseType(typeof(EmploymentHistory))]
        //public IHttpActionResult GetEmploymentHistory(string id)
        //{
        //    EmploymentHistory employmentHistory = db.EmploymentHistories.Find(id);
        //    if (employmentHistory == null)
        //    {
        //        return NotFound();
        //    }

        //    return Ok(employmentHistory);
        //}

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
            //employmentHistory.Id = Guid.NewGuid().ToString();

            _mongoHelper.Collection.Save(employmentHistory);
            // try
            //{
            
            //}
            //catch (DbUpdateException)
            //{
            //    if (EmploymentHistoryExists(employmentHistory.Id))
            //    {
            //        return Conflict();
            //    }
            //    else
            //    {
            //        throw;
            //    }
            //}

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