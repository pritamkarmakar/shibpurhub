using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;
using System.Web.Http.Results;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models.WebAPI;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class QuestionsController : ApiController
    {
        private MongoHelper<Questions> _mongoHelper;

        public QuestionsController()
        {
            _mongoHelper = new MongoHelper<Questions>();
        }

        // GET: api/Questions
        /// <summary>
        /// Will return all available questions
        /// </summary>
        /// <returns></returns>
        public IList<Questions> GetQuestions()
        {
            var result = _mongoHelper.Collection.FindAll().ToList();

            return result;
        }

        // GET: api/Questions/5
        // Will return a specific question with comments
        [ResponseType(typeof(Questions))]
        public IHttpActionResult GetQuestion(string questionId)
        {
            var questions = _mongoHelper.Collection.AsQueryable().Where(m => m.QuestionId.ToString() == questionId);
            if (questions.Count() == 0)
            {
                return NotFound();
            }

            //// Get the associated comments for this question
            //IEnumerable<Comments> comments = db.Comments.Where(m => m.QuestionId == questionId).ToList();
            //// find who submitted this question
            //string userName = db.AspNetUsers.Where(m => m.Id == questions.UserId).ToList()[0].UserName;
            //questions.Comments = comments;

            return Ok(questions);
        }

        // PUT: api/Questions/5
        //[ResponseType(typeof(void))]
        //public IHttpActionResult PutQuestions(string id, Question questions)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    if (id != questions.QuestionId)
        //    {
        //        return BadRequest();
        //    }

        //    db.Entry(questions).State = EntityState.Modified;

        //    try
        //    {
        //        db.SaveChanges();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!QuestionsExists(id))
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

        // POST: api/Questions
        [ResponseType(typeof(Questions))]
        public IHttpActionResult PostQuestions(QuestionsDTO question)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (question == null)
                return BadRequest("Request body is null. Please send a valid Questions object");

            // if Question doesn't have any category tagging then its a bad request
            if (question.CategoriesName == null || question.CategoriesName.Split(',').ToList().Count == 0)
                return BadRequest("Question must have a category association");

            // validate given categories are valid and create the category object list
            List<Categories> categoryList = new List<Categories>();
            CategoriesController categoriesController = new CategoriesController();
            foreach (var category in question.CategoriesName.Split(','))
            {
                var actionResult = categoriesController.GetCategory(category);
                var contentResult = actionResult as OkNegotiatedContentResult<Categories>;
                if (contentResult.Content == null)
                    return BadRequest("Supplied category is not valid");
                else
                {
                    categoryList.Add(contentResult.Content);
                }
            }

            // Create the QuestionId guid if it is null (for new question)
            if (question.QuestionId == null)
                question.QuestionId = ObjectId.GenerateNewId();

            //// validate associated categories are add them into db context
            //foreach (var category in question.Categories)
            //{
            //    Category categories = db.Categories.Where(m => m.Name == category.Name).ToList().Count == 0 ? null : db.Categories.Where(m => m.Name == category.Name).ToList()[0];

            //    if (categories == null)
            //        return BadRequest(string.Format("Category {0} is not valid.", category.Name));
            //    else
            //    {
            //        db.CategoryTaggings.Add(new CategoryTagging()
            //        {
            //            Id = Guid.NewGuid().ToString(),
            //            CategoryId = categories.CategoryId,
            //            QuestionId = question.QuestionId
            //        });
            //    }
            //}

            // add the datetime stamp for this question
            question.PostedOnUtc = DateTime.UtcNow;

            _mongoHelper.Collection.Save(question);
            
            // update the CategoryTagging collection
            CategoryTaggingController categoryTaggingController = new CategoryTaggingController();
            foreach (var category in categoryList)
            {
                CategoryTagging ct = new CategoryTagging();
                ct.Id = ObjectId.GenerateNewId();
                ct.CategoryId = category.CategoryId;
                ct.QuestionId = question.QuestionId;

                categoryTaggingController.PostCategoryTagging(ct);
            }
            

            return CreatedAtRoute("DefaultApi", new { id = question.QuestionId }, question);
        }

        //// DELETE: api/Questions/5
        //[ResponseType(typeof(Question))]
        //public IHttpActionResult DeleteQuestions(string id)
        //{
        //    //Question questions = db.Questions.Find(id);
        //    //if (questions == null)
        //    //{
        //    //    return NotFound();
        //    //}

        //    //db.Questions.Remove(questions);
        //    //db.SaveChanges();

        //    //return Ok(questions);
        //}
    }
}