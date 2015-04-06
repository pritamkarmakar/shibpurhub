using MongoDB.Driver;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models.WebAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Nest;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    public class UsersController : ApiController
    {
        private MongoHelper _mongoHelper;
        private ElasticSearchHelper _elasticSearchHealer;

        public UsersController()
        {
            _elasticSearchHealer = new ElasticSearchHelper();
        }

        // GET api/users
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        /// <summary>
        /// Get all system users
        /// </summary>
        /// <returns></returns>
        public object GetAllUsers()
        {
            int from = 0;
            long totalHits = 0;
            int resultRetrieved = 0;
            List<CustomUserInfo> result = new List<CustomUserInfo>();
            var client = _elasticSearchHealer.ElasticClient();

            while (resultRetrieved <= totalHits)
            {
                var response = client.Search<object>(s => s.AllIndices().Type(typeof(CustomUserInfo)).From(from));
                // get the total count of hits
                if (totalHits == 0)
                    totalHits = response.Total;
                // increase the result retrieve + 10 as elastic search by default return this many documents
                resultRetrieved += 10;
                // increase the from to go to next page
                from += 10;
                // add the response in final result object
                foreach (CustomUserInfo doc in response.Documents)
                {
                    result.Add(doc);
                }                
            }            

            return result;
        }

        /// <summary>
        /// Search users by name, email, company name, location etc
        /// </summary>
        /// <param name="searchTerm">search term (can be location/company name/ user name/email etc)</param>
        /// <returns></returns>
        public object GetUsersByTerm(string searchTerm)
        {
            int from = 0;
            int total = 0;
            List<object> result = new List<object>();
            var client = _elasticSearchHealer.ElasticClient();

            var response = client.Search<object>(s => s.AllIndices().AllTypes().Query(query => query
        .QueryString(qs => qs.Query("*" +searchTerm+ "*"))).From(from));

            return result;
        }



        // POST api/users
        public void Post([FromBody]string value)
        {
        }

        // PUT api/users/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/users/5
        public void Delete(int id)
        {
        }
    }
}
