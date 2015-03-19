using Nest;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace ShibpurConnectWebApp.Helper
{
    public class ElasticSearchHelper
    {
        /// <summary>
        /// Method to create the Elastic Search client instance
        /// </summary>
        /// <returns></returns>
        public ElasticClient ElasticClient()
        {
            var node = new Uri(ConfigurationManager.ConnectionStrings["ElasticSearch"].ConnectionString);

            var settings = new ConnectionSettings(
                node,
                defaultIndex: "my_index"
            );

            return new ElasticClient(settings);
        }
    }
}