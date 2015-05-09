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
using System.Security.Claims;
using System.Threading.Tasks;

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
        [ResponseType(typeof(Answer))]
        public IHttpActionResult PostAnswer(Answer answer)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (answer == null)
                return BadRequest("Request body is null. Please send a valid Answer object");
            try
            {
                // validate given questionid, userid are valid
                var questionMongoHelper = new MongoHelper<Question>();
                var question = questionMongoHelper.Collection.AsQueryable().Where(m => m.QuestionId == answer.QuestionId).ToList().FirstOrDefault();
                if (question == null)
                    return BadRequest("Supplied questionid is invalid");

                // add the datetime stamp for this question
                answer.PostedOnUtc = DateTime.UtcNow;

                // save the answer to the database
                var result = _mongoHelper.Collection.Save(answer);

                // if mongo failed to save the data then send error
                if (!result.Ok)
                    return InternalServerError();

                // change the "HasAnswered' column of the question. If it is false then change to true
                if (question.HasAnswered == false)
                {
                    question.HasAnswered = true;
                    questionMongoHelper.Collection.Save(question);
                }
            }
            catch (MongoDB.Driver.MongoConnectionException ex)
            {
                return BadRequest(ex.Message);
            }

            return CreatedAtRoute("DefaultApi", new { id = answer.QuestionId }, answer);
        }

        public async Task<int> UpdateUpVoteCount(Answer answer)
        {
            try
            {
                ObjectId.Parse(answer.AnswerId);
            }
            catch (Exception)
            {
                return 0;
            }

            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
            var claim = principal.FindFirst("sub");

            Helper.Helper helper = new Helper.Helper();
            var userResult = helper.FindUserByEmail(claim.Value);
            var userInfo = await userResult;

            if (userInfo == null)
            {
                return 0;
            }

            var answerInDB = _mongoHelper.Collection.AsQueryable().Where(m => m.AnswerId == answer.AnswerId).FirstOrDefault();
            if (answerInDB != null)
            {
                var upCount = answerInDB.UpVoteCount + 1;
                answerInDB.UpVoteCount = upCount;
                if (answerInDB.UpvotedByUserIds == null)
                {
                    answerInDB.UpvotedByUserIds = new List<string>();
                }
                answerInDB.UpvotedByUserIds.Add(userInfo.Id);
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
            if (answers == null || answers.Count == 0)
            {
                return false;
            }

            foreach (var answer in answers)
            {
                var answerInDB = _mongoHelper.Collection.AsQueryable().Where(m => m.AnswerId == answer.AnswerId).FirstOrDefault();
                if (answerInDB != null)
                {
                    answerInDB.MarkedAsAnswer = answer.MarkedAsAnswer;
                }
                _mongoHelper.Collection.Save(answerInDB);
            }

            return true;
        }
    }
}

