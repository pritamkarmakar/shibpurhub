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
using System.Web.Http.Results;
using WebApi.OutputCache.V2;

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

        /// <summary>
        /// Get all existing categories
        /// </summary>
        /// <returns></returns>
        public IHttpActionResult GetCategories()
        {
            try
            {
                return Ok(_mongoHelper.Collection.FindAll().ToList());
            }
            catch (MongoDB.Driver.MongoConnectionException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get details about a specific category
        /// </summary>
        /// <param name="categoryName">category name</param>
        /// <returns></returns>
        [ResponseType(typeof(Categories))]
        [CacheOutput(ClientTimeSpan = 86400, ServerTimeSpan = 86400)]
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
       
        /// <summary>
        /// Get top 20 list of categories with question count
        /// </summary>
        /// <returns></returns>
        [CacheOutput(ClientTimeSpan = 86400, ServerTimeSpan = 86400)]
        public IHttpActionResult GetTop20CategoriesWithQuestionCount()
        {
            List<CategoryCloud> categoryCloud = new List<CategoryCloud>();
            
            foreach(Categories catg in _mongoHelper.Collection.FindAll())
            {
                // retrieve the total questions tagged with this category
                CategoryTaggingController ctc = new CategoryTaggingController();
                IHttpActionResult actionResult = ctc.GetQuestionCount(catg.CategoryId.ToString());
                var count = actionResult as OkNegotiatedContentResult<int>;

                categoryCloud.Add(new CategoryCloud
                    {
                        CategoryId = catg.CategoryId,
                        CategoryName = catg.CategoryName,
                        HasPublished = catg.HasPublished,
                        QuestionCount = count.Content
                    });
            }

            return Ok(categoryCloud.OrderByDescending(m => m.QuestionCount).ToList().Take(20));
        }

        /// <summary>
        /// Add a new category
        /// </summary>
        /// <param name="category">Categories object</param>
        /// <returns></returns>
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

        /// <summary>
        /// Delete a category
        /// </summary>
        /// <param name="categoryName">category name</param>
        /// <returns></returns>
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