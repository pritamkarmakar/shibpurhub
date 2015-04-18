using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models.WebAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    public class StatusController : ApiController
    {
        private MongoHelper<Categories> _mongoHelper;

        public StatusController()
        {
            _mongoHelper = new MongoHelper<Categories>();
        }

        /// <summary>
        /// API to indicate whether we can connect to database or not
        /// </summary>
        /// <returns></returns>
        public IHttpActionResult GetDatabaseStatus()
        {
            try
            {
                var result = _mongoHelper.Collection.FindAll().ToList();
                return Ok("{\"message\": \"database is working\"}");
            }
            catch (MongoDB.Driver.MongoConnectionException ex)
            {
                return BadRequest(ex.Message);
            }
        }
        
        // GET api/status
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/status/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/status
        public void Post([FromBody]string value)
        {
        }

        // PUT api/status/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/status/5
        public void Delete(int id)
        {
        }
    }
}
