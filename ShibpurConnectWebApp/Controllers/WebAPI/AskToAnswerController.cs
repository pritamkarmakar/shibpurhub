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
using WebApi.OutputCache.V2;

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
        ///  API to check if we have asked a user to answer a question
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

        /// <summary>
        /// Get the response rate for a user. We will calculate this based on how many request for answer he/she has received 
        /// and how many he/she responded
        /// </summary>
        /// <param name="userId">user id</param>
        /// <returns></returns>
        [CacheOutput(ServerTimeSpan = 86400)]
        public string GetResponseRate(string userId)
        {
            // total response received
            int responseReceived = 0;
            var result = (from e in _mongoHelper.Collection.AsQueryable<AskToAnswer>()
                          where e.AskedTo == userId 
                          select e).ToList();

            if (result.Count > 0)
                responseReceived = result.Count;
            else
                return "NA";
              

            // response given
            var responseGiven = (from e in _mongoHelper.Collection.AsQueryable<AskToAnswer>()
                          where e.AskedTo == userId && e.HasAnswered == true
                          select e).ToList();

            if (responseGiven.Count == 0)
                return "0%";
            else
            {
                // calculate response rate
                double rRate = ((double)responseGiven.Count / (double)responseReceived) * 100;

                return Convert.ToInt32(rRate).ToString() + "%";
            }
           
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
    }
}
