using Newtonsoft.Json.Linq;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models.WebAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    public class SearchController : ApiController
    {
        private ElasticSearchHelper _elasticSearchHealer;

        public SearchController()
        {
            _elasticSearchHealer = new ElasticSearchHelper();
        }

        /// <summary>
        /// Method to search across all documents of all types.
        /// </summary>
        /// <param name="searchTerm"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IHttpActionResult> SearchAllTypes(string searchTerm)
        {
            int from = 0;
            long totalHits = 0;
            int resultRetrieved = 0;
            List<object> result = new List<object>();
            var client = _elasticSearchHealer.ElasticClient();

            // convert the comma(,) separated categories into space separeted single string. We will use this string as search text in elastic search
            StringBuilder sb = new StringBuilder();

            // validate categories is not empty
            if (!string.IsNullOrEmpty(searchTerm))
            {
                foreach (string str in searchTerm.Split(','))
                {
                    sb.Append(str.Trim() + " ");
                }
            }

            while (resultRetrieved <= totalHits)
            {
                var response = client.Search<object>(s => s.AllIndices().AllTypes().Query(query => query
       .QueryString(qs => qs.Query("*" + sb.ToString().TrimEnd() + "*"))).From(from));
                // get the total count of hits
                if (totalHits == 0)
                    totalHits = response.Total;
                // increase the result retrieve + 10 as elastic search by default return this many documents
                resultRetrieved += 10;
                // increase the from to go to next page
                from += 10;
                // add the response in final result object
                foreach (object doc in response.Documents)
                {
                    result.Add(doc);
                }
            }

            // retrieve the unique user info from the result
            HashSet<string> hash = new HashSet<string>();
            List<UserProfile> userProfileList = new List<UserProfile>();

            foreach (object obj in result)
            {
                var userData = JObject.Parse(obj.ToString());
                if (!hash.Contains((string)userData["id"]))
                {
                    // retrieve the userInfo
                    ProfileController profileController = new ProfileController();
                    IHttpActionResult actionResult = await profileController.GetProfileByUserId((string)userData["id"]);
                    var userProfile = actionResult as OkNegotiatedContentResult<UserProfile>;

                    if (userProfile != null)
                    {
                        hash.Add((string)userData["id"]);
                        userProfileList.Add(userProfile.Content);
                    }
                }
            }


            return Ok(userProfileList);
        }

        /// <summary>
        /// Method to search user by email or name. We are using this API in 'Ask To Answer' search box
        /// </summary>
        /// <param name="searchTerm"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IHttpActionResult> SearchUsersByNameEmail(string userDetails)
        {
            int from = 0;
            long totalHits = 0;
            int resultRetrieved = 0;
            List<CustomUserInfo> result = new List<CustomUserInfo>();
            var client = _elasticSearchHealer.ElasticClient();

            // convert the comma(,) separated categories into space separeted single string. We will use this string as search text in elastic search
            StringBuilder sb = new StringBuilder();

            // validate categories is not empty
            if (!string.IsNullOrEmpty(userDetails))
            {
                foreach (string str in userDetails.Split(','))
                {
                    sb.Append(str.Trim() + " ");
                }
            }

            while (resultRetrieved <= totalHits)
            {
                var response = client.Search<object>(s => s.AllIndices().Type(typeof(CustomUserInfo)).Query(query => query
       .QueryString(qs => qs.Query("*" + sb.ToString().TrimEnd() + "*"))).From(from));
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

            return Ok(result);
        }

        // GET api/search
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/search/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/search
        public void Post([FromBody]string value)
        {
        }

        // PUT api/search/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/search/5
        public void Delete(int id)
        {
        }
    }
}
