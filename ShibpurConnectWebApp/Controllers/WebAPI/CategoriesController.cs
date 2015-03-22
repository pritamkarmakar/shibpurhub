using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models.WebAPI;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class CategoriesController : ApiController
    {
        private MongoHelper<Categories> _mongoHelper;

        public CategoriesController()
        {
            _mongoHelper = new MongoHelper<Categories>();
        }

        // GET: api/Categories
        public IList<Categories> GetCategories()
        {
            return _mongoHelper.Collection.FindAll().ToList();
        }

        // GET: api/Categories/placement
        [ResponseType(typeof(Categories))]
        public IHttpActionResult GetCategory(string categoryName)
        {
            Categories category = null;

            try
            {
                category = _mongoHelper.Collection.AsQueryable().Where(m => m.CategoryName.ToLower() == categoryName.Trim().ToLower()).ToList().Count == 0 ? null : _mongoHelper.Collection.AsQueryable().Where(m => m.CategoryName.ToLower() == categoryName.Trim().ToLower()).ToList()[0];
            }
            catch(Exception ex)
            {
                return InternalServerError(ex);                
            }

            if (category == null)
            {
                return NotFound();
            }

            return Ok(category);
        }

        //// PUT: api/Categories/5
        //[ResponseType(typeof(void))]
        //public IHttpActionResult PutCategories(string id, Category categories)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    if (id != categories.CategoryId)
        //    {
        //        return BadRequest();
        //    }

        //    db.Entry(categories).State = EntityState.Modified;

        //    try
        //    {
        //        db.SaveChanges();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!CategoriesExists(id))
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

        // POST: api/Categories
        [ResponseType(typeof(Categories))]
        public IHttpActionResult PostCategories(Categories category)
        {
            if (!ModelState.IsValid || category == null)
            {
                return BadRequest(ModelState);
            }

            var result = _mongoHelper.Collection.Save(category);

            // if mongo failed to save the data then send error
            if (!result.Ok)
                return InternalServerError(new Exception("Failed to save the category in the database"));

           return CreatedAtRoute("DefaultApi", new { id = category.CategoryId }, category);
        }

        // DELETE: api/Categories/5
        [ResponseType(typeof(Categories))]
        public IHttpActionResult DeleteCategories(string categoryName)
        {
            Categories categories = _mongoHelper.Collection.AsQueryable().Where(m => m.CategoryName == categoryName).ToList().Count == 0 ? null : _mongoHelper.Collection.AsQueryable().Where(m => m.CategoryName == categoryName).ToList()[0];
            if (categories == null)
            {
                return NotFound();
            }

            _mongoHelper.Collection.Remove(Query.EQ("Categoryname", categoryName));
            
            return Ok(categories);
        }
       
    }
}