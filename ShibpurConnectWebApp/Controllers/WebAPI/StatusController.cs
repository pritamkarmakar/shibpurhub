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
            catch (MongoDB.Driver.MongoQueryException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
