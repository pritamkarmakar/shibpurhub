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
            var result = _mongoHelper.Collection.FindAll().OrderByDescending(a => a.PostedOnUtc).ToList();

            return result;
        }

        public IList<QuestionsDTO> GetQuestionsByCategory(string category)
        {
            var result = new List<QuestionsDTO>();
            var allQuestions = _mongoHelper.Collection.FindAll().ToList();
            foreach (var question in allQuestions)
            {
                var searchedCategory = question.Categories.Where(a => a.ToLower().Trim() == category.ToLower().Trim()).FirstOrDefault();
                if(!string.IsNullOrEmpty(searchedCategory))
                {
                    result.Add(question);
                }
            }

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

        public int GetAnswersCount(string questionId)
        {
            try
            {
                ObjectId.Parse(questionId);
            }
            catch (Exception)
            {
                return 0;
            }

            var answerMongoHelper = new MongoHelper<Answer>();
            var count = answerMongoHelper.Collection.AsQueryable().Where(m => m.QuestionId == questionId).ToList().Count;
            return count;
        }

        public int GetViewCount(string questionId)
        {
            try
            {
                ObjectId.Parse(questionId);
            }
            catch (Exception)
            {
                return 0;
            }

            var question = _mongoHelper.Collection.AsQueryable().Where(m => m.QuestionId == questionId).FirstOrDefault();
            return question == null ? 0 : question.ViewCount;
        }

        [ResponseType(typeof(int))]
        [ActionName("IncrementViewCount")]
        public int IncrementViewCount(QuestionsDTO question)
        {
            try
            {
                ObjectId.Parse(question.QuestionId);
            }
            catch (Exception)
            {
                return 0;
            }

            var questionInDB = _mongoHelper.Collection.AsQueryable().Where(m => m.QuestionId == question.QuestionId).FirstOrDefault();
            if (questionInDB != null)
            {
                var count = questionInDB.ViewCount + 1;
                questionInDB.ViewCount = count;
                _mongoHelper.Collection.Save(questionInDB);
                return count;
            }

            return 0;
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