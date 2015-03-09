using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using AspNet.Identity.MongoDB;
using MongoDB.Driver;

namespace ShibpurConnectWebApp.Helper
{
    public class MongoHelper<T> where T : class
    {
        public MongoCollection<T> Collection { get; private set; }

        public MongoHelper()
        {
            var client = new MongoClient(ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString);
            var db = client.GetServer().GetDatabase("shibpurconnect");
            Collection = db.GetCollection<T>(typeof(T).Name.ToLower());
        }
    }
}