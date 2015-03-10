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
        private MongoHelper<QuestionsDTO> _mongoHelper;

        public QuestionsController()
        {
            _mongoHelper = new MongoHelper<QuestionsDTO>();
        }

        // GET: api/Questions
        /// <summary>
        /// Will return all available questions
        /// </summary>
        /// <returns></returns>
        public IList<QuestionsDTO> GetQuestions()
        {
            var result = _mongoHelper.Collection.FindAll().ToList();

            return result;
        }

        // GET: api/Questions/5
        // Will return a specific question with comments
        [ResponseType(typeof(QuestionsDTO))]
        public IHttpActionResult GetQuestion(string questionId)
        {
            // validate questionId is valid hex string

            try
            {
                ObjectId.Parse(questionId);
            }
            catch (Exception)
            {
                return BadRequest(String.Format("Supplied questionId: {0} is not a valid object id", questionId));
            }
            

            var questions = _mongoHelper.Collection.AsQueryable().Where(m => m.QuestionId == questionId);
            if (questions.Count() == 0)
            {
                return NotFound();
            }

            return Ok(questions.ToList()[0]);
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
            if (question.Categories == null || question.Categories.Length == 0)
                return BadRequest("Question must have a category association");

            // validate given categories are valid and create the category object list
            List<Categories> categoryList = new List<Categories>();
            CategoriesController categoriesController = new CategoriesController();
            foreach (var category in question.Categories)
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

            // add the datetime stamp for this question
            question.PostedOnUtc = DateTime.UtcNow;

            // save the question to the database
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