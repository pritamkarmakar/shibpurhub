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
        private string databaseName = ConfigurationManager.AppSettings["databasename"];

        public MongoHelper()
        {
            var client = new MongoClient(ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString);
            var db = client.GetServer().GetDatabase(databaseName);
            Collection = db.GetCollection<T>(typeof(T).Name.ToLower());
        }
    }

    public class MongoHelper
    {
        public MongoCollection Collection { get; private set; }
        private string databaseName = ConfigurationManager.AppSettings["databasename"];

        public MongoHelper(string collectionName)
        {
            var client = new MongoClient(ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString);
            var db = client.GetServer().GetDatabase(databaseName);
            Collection = db.GetCollection(collectionName);
        }
    }
}