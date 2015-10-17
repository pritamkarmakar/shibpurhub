using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;
using MongoDB.Driver.Linq;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models.WebAPI;
using WebApi.OutputCache.V2;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class SkillSetsController : ApiController
    {
        private MongoHelper<SkillSets> _mongoHelper;

        public SkillSetsController()
        {
            _mongoHelper = new MongoHelper<SkillSets>();
        }

        /// <summary>
        /// Get all existing skillsets
        /// </summary>
        /// <returns></returns>
        public IHttpActionResult GetSkillSets()
        {
            try
            {
                return Ok(_mongoHelper.Collection.FindAll().ToList());
            }
            catch (MongoDB.Driver.MongoConnectionException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get details about a specific skillset
        /// </summary>
        /// <param name="categoryName">category name</param>
        /// <returns></returns>
        [ResponseType(typeof(Categories))]
        [CacheOutput(ServerTimeSpan = 864000, ExcludeQueryStringFromCacheKey = true, NoCache = true)]
        public async Task<IHttpActionResult> GetSkillSet(string categoryName)
        {
            SkillSets skillSet = null;

            try
            {
                skillSet = _mongoHelper.Collection.AsQueryable().Where(m => m.SkillSetName.ToLower() == categoryName.Trim().ToLower()).ToList().Count == 0 ? null : _mongoHelper.Collection.AsQueryable().Where(m => m.SkillSetName.ToLower() == categoryName.Trim().ToLower()).ToList()[0];
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }

            if (skillSet == null)
            {
                return BadRequest("skillset not found" + categoryName);
            }

            return Ok(skillSet);
        }
    }
}
