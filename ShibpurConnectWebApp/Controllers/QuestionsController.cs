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
using ShibpurConnectWebApp.Models;

namespace ShibpurConnectWebApp.Controllers
{
    [Authorize]
    public class QuestionsController : ApiController
    {
        private ShibpurConnectDB db = new ShibpurConnectDB();

        // GET: api/Questions
        /// <summary>
        /// Will return all available questions
        /// </summary>
        /// <returns></returns>
        public IQueryable<Questions> GetQuestions()
        {
            return db.Questions.OrderByDescending(a => a.PostedOnUtc);
        }

        // GET: api/Questions/5
        // Will return a specific question with comments
        [ResponseType(typeof(Questions))]
        public IHttpActionResult GetQuestion(string questionId)
        {
            Questions questions = db.Questions.Find(questionId);
            if (questions == null)
            {
                return NotFound();
            }

            // Get the associated comments for this question
            IEnumerable<Comments> comments = db.Comments.Where(m => m.QuestionId == questionId).ToList();
            questions.Comments = comments;

            return Ok(questions);
        }

        // GET: api/Questions/5
        // Will return all the questions tagged to a particular category
        [ResponseType(typeof(Questions))]
        [HttpGet]
        public IHttpActionResult QuestionsForACategory(string categoryName)
        {
            // check if supplied category is valid or not
            Categories category = db.Categories.Where(m => m.Name == categoryName).ToList().Count == 0 ? null : db.Categories.Where(m => m.Name == categoryName).ToList()[0];
            if (category == null)
            {
                return BadRequest("Invalid category name");
            }

            // Get the associated question for this category
            var result = from n in db.Questions
                where (from m in db.CategoryTaggings
                    where m.CategoryId == category.CategoryId
                    select m.QuestionId).Contains(n.QuestionId)
                select n;

            return Ok(result);
        }

        // PUT: api/Questions/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutQuestions(string id, Questions questions)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != questions.QuestionId)
            {
                return BadRequest();
            }

            db.Entry(questions).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!QuestionsExists(id))
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

        // POST: api/Questions
        [ResponseType(typeof (Questions))]
        public IHttpActionResult PostQuestions(Questions questions)
        {
            if (!ModelState.IsValid || questions == null)
            {
                return BadRequest(ModelState);
            }

            // if Question doesn't have any category tagging then its a bad request
            if (questions.Categories == null || questions.Categories.ToList().Count == 0)
                return BadRequest("Question must have a category association");

            // Create the QuestionId guid if it is null (for new question)
            if (questions.QuestionId == null)
                questions.QuestionId = Guid.NewGuid().ToString();

            // validate associated categories are add then into db context
            foreach (var category in questions.Categories)
            {
                Categories categories =  db.Categories.Where(m => m.Name == category.Name).ToList().Count == 0 ? null : db.Categories.Where(m => m.Name == category.Name).ToList()[0];

                if (categories == null)
                    return BadRequest( string.Format("Category {0} is not valid.", category.Name));
                else
                {
                    db.CategoryTaggings.Add(new CategoryTaggings()
                    {
                        Id = Guid.NewGuid().ToString(),
                        CategoryId = categories.CategoryId,
                        QuestionId = questions.QuestionId                        
                    });
                }
            }

            // add the datetime stamp for this question
            questions.PostedOnUtc = DateTime.UtcNow;

            db.Questions.Add(questions);

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateException)
            {
                if (QuestionsExists(questions.QuestionId))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtRoute("DefaultApi", new {id = questions.QuestionId}, questions);
        }

        // DELETE: api/Questions/5
        [ResponseType(typeof(Questions))]
        public IHttpActionResult DeleteQuestions(string id)
        {
            Questions questions = db.Questions.Find(id);
            if (questions == null)
            {
                return NotFound();
            }

            db.Questions.Remove(questions);
            db.SaveChanges();

            return Ok(questions);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool QuestionsExists(string id)
        {
            return db.Questions.Count(e => e.QuestionId == id) > 0;
        }
    }
}