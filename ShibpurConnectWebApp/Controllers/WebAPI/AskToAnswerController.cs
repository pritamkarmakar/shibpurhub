using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models.WebAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using MongoDB.Driver.Linq;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    public class AskToAnswerController : ApiController
    {
        private MongoHelper<AskToAnswer> _mongoHelper;

        public AskToAnswerController()
        {
            _mongoHelper = new MongoHelper<AskToAnswer>();
        }

        // GET api/asktoanswer
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        /// <summary>
        ///  API to search AskToAnswer collection for a specific question and specific user
        /// </summary>
        /// <param name="questionId">question id</param>
        /// <param name="userId">User to whom we have asked to answer the question</param>
        /// <returns>AskToAnswer object; if not found then null</returns>
        public AskToAnswer GetAskToAnswer(string questionId, string userId)
        {
            var result = (from e in _mongoHelper.Collection.AsQueryable<AskToAnswer>()
                          where e.AskedTo == userId && e.QuestionId == questionId                       
                          select e).ToList();

            if (result.Count == 0)
                return null;
            else
                return result[0];
        }

        // GET api/asktoanswer/5
        public string Get(int id)
        {
            return "value";
        }

        /// <summary>
        /// API to save the Ask to answer request 
        /// </summary>
        /// <param name="askToAnswer"></param>
        /// <returns></returns>
        [ResponseType(typeof(AskToAnswer))]
        public IHttpActionResult PostAskToAnswer(AskToAnswer askToAnswer)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (askToAnswer == null)
                return BadRequest("Request body is null. Please send a valid AskToAnswer object");

            // add the datetime stamp for this askToAnswer if only this is a new entry
            if (string.IsNullOrEmpty(askToAnswer.Id))
                askToAnswer.AskedOnUtc = DateTime.UtcNow;        
            
            // save the notification to the database collection
            var result = _mongoHelper.Collection.Save(askToAnswer);

            // if mongo failed to save the data then send error
            if (!result.Ok)
                return InternalServerError();

            return CreatedAtRoute("DefaultApi", new { id = askToAnswer.Id }, askToAnswer);
        }

        // PUT api/asktoanswer/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/asktoanswer/5
        public void Delete(int id)
        {
        }
    }
}
