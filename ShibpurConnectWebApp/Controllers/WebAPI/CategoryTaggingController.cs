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

        // GET: api/CategoryTagging
        public IHttpActionResult QuestionsForACategory(string categoryName)
        {
            // check if supplied category is valid or not
            CategoriesController categoriesController = new CategoriesController();
            IHttpActionResult actionResult = categoriesController.GetCategory(categoryName);
            var contentResult = actionResult as OkNegotiatedContentResult<Categories>;
            
            //var category = _mongoHelper.Collection.AsQueryable().Where(m => m. == categoryName).ToList().Count == 0 ? null : db.Categories.Where(m => m.Name == categoryName).ToList()[0];
            if (contentResult == null)
            {
                return BadRequest("Invalid category name");
            }

            // Get the associated questions for this category
            var questions =
                _mongoHelper.Collection.AsQueryable()
                    .Where(m => m.CategoryId == contentResult.Content.CategoryId)
                    .ToList();
            IList<Questions> questionList = new List<Questions>();
            QuestionsController questionsController = new QuestionsController();

            // form the question list
            foreach (var question in questions)
            {
                IHttpActionResult actionResult2 = questionsController.GetQuestion(question.QuestionId.ToString());
                var contentResult2 = actionResult2 as OkNegotiatedContentResult<Questions>;
                questionList.Add(contentResult2.Content);
            }

            return Ok(questionList);
        }

        // GET: api/CategoryTagging/5
        public string Get(int id)
        {
            return "value";
        }

        // POST: api/CategoryTagging
        public void PostCategoryTagging(CategoryTagging categoryTagging)
        {
            _mongoHelper.Collection.Save(categoryTagging);
        }

        // PUT: api/CategoryTagging/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/CategoryTagging/5
        public void Delete(int id)
        {
        }
    }
}
