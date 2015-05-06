using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Http.Results;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models;
using ShibpurConnectWebApp.Models.WebAPI;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    public class AnswersController : ApiController
    {
        private MongoHelper<Answer> _mongoHelper;

        public AnswersController()
        {
            _mongoHelper = new MongoHelper<Answer>();
        }

        // GET: api/Questions
        /// <summary>
        /// Will return all available Answers
        /// </summary>
        /// <returns></returns>
        public IList<Answer> GetAnswers()
        {
            var result = _mongoHelper.Collection.FindAll().OrderBy(a => a.PostedOnUtc).ToList();
            return result;
        }

        // GET: api/Questions/5
        // Will return all the answers for a specific question
        [ResponseType(typeof(Answer))]
        public IHttpActionResult GetAnswers(string questionId)
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


            var questions = _mongoHelper.Collection.AsQueryable().Where(m => m.QuestionId == questionId).OrderBy(a => a.MarkedAsAnswer).ThenBy(b => b.PostedOnUtc);
            if (questions.Count() == 0)
            {
                return NotFound();
            }

            return Ok(questions.ToList());
        }

        // POST: api/Questions
        [ResponseType(typeof (Answer))]
        public IHttpActionResult PostAnswer(Answer answer)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (answer == null)
                return BadRequest("Request body is null. Please send a valid Questions object");

            // validate given questionid, userid are valid
            QuestionsController questionsController = new QuestionsController();

            var actionResult = questionsController.GetQuestion(answer.QuestionId);
            var contentResult = actionResult as OkNegotiatedContentResult<QuestionViewModel>;
            if (contentResult.Content == null)
                return BadRequest("Supplied questionid is invalid");

            // add the datetime stamp for this question
            answer.PostedOnUtc = DateTime.UtcNow;

            // save the question to the database
            var result = _mongoHelper.Collection.Save(answer);

            // if mongo failed to save the data then send error
            if (!result.Ok)
                return InternalServerError();

            return CreatedAtRoute("DefaultApi", new {id = answer.QuestionId}, answer);
        }

        public int UpdateUpVoteCount(Answer answer)
        {
            try
            {
                ObjectId.Parse(answer.AnswerId);
            }
            catch (Exception)
            {
                return 0;
            }

            var answerInDB = _mongoHelper.Collection.AsQueryable().Where(m => m.AnswerId == answer.AnswerId).FirstOrDefault();
            if (answerInDB != null)
            {
                    var upCount = answerInDB.UpVoteCount + 1;
                    answerInDB.UpVoteCount = upCount;
                    if(answerInDB.UpvotedBy == null)
                    {
                        answerInDB.UpvotedBy = new List<string> { answer.UserEmail };
                    }
                    answerInDB.UpvotedBy.Add(answer.UserEmail);
                    _mongoHelper.Collection.Save(answerInDB);
                    return upCount;                
            }

            return 0;
        }

        public int UpdateDownVoteCount(Answer answer)
        {
            try
            {
                ObjectId.Parse(answer.AnswerId);
            }
            catch (Exception)
            {
                return 0;
            }

            var answerInDB = _mongoHelper.Collection.AsQueryable().Where(m => m.AnswerId == answer.AnswerId).FirstOrDefault();
            if (answerInDB != null)
            {                
                    var downCount = answerInDB.DownVoteCount + 1;
                    answerInDB.DownVoteCount = downCount;
                    _mongoHelper.Collection.Save(answerInDB);
                    return downCount;
            }

            return 0;
        }

        public bool UpdateMarkAsAnswer(List<Answer> answers)
        {
            if(answers == null || answers.Count == 0)
            {
                return false;
            }

            foreach(var answer in answers)
            {
                var answerInDB = _mongoHelper.Collection.AsQueryable().Where(m => m.AnswerId == answer.AnswerId).FirstOrDefault();
                if(answerInDB != null)
                {
                    answerInDB.MarkedAsAnswer = answer.MarkedAsAnswer;
                }
                _mongoHelper.Collection.Save(answerInDB);
            }

            return true;
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

