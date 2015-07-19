using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Results;
using MongoDB.Driver.Linq;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models.WebAPI;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    public class CategoryTaggingController : ApiController
    {
        private MongoHelper<CategoryTagging> _mongoHelper;

        public CategoryTaggingController()
        {
            _mongoHelper = new MongoHelper<CategoryTagging>();
        }

        /// <summary>
        /// Get the total question tagged with the specific category
        /// </summary>
        /// <param name="categoryId">category id</param>
        /// <returns></returns>
        public IHttpActionResult GetQuestionCount(string categoryId)
        {

            try
            {
                var result = _mongoHelper.Collection.AsQueryable().Where(m => m.CategoryId == categoryId).ToList().Count;
               
                return Ok(result);
            }
            catch (MongoDB.Driver.MongoConnectionException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: api/CategoryTagging
        /// <summary>
        /// Save a new category tagging
        /// </summary>
        /// <param name="categoryTagging"></param>
        public void PostCategoryTagging(CategoryTagging categoryTagging)
        {
            _mongoHelper.Collection.Save(categoryTagging);
        }
    }
}
