using System.Collections.ObjectModel;
using System.Configuration;
using ShibpurConnectWebApp.Helper;

namespace ShibpurConnectWebApp
{
	using System;
	using AspNet.Identity.MongoDB;
	using MongoDB.Driver;

	public class ApplicationIdentityContext : IdentityContext, IDisposable
	{
		public ApplicationIdentityContext(MongoCollection users, MongoCollection roles) : base(users, roles)
		{
		}

		public static ApplicationIdentityContext Create()
		{
			// todo add settings where appropriate to switch server & database in your own application
            var client = new MongoClient(ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString);
            var database = client.GetServer().GetDatabase("shibpurconnect");
            var users = database.GetCollection<IdentityUser>("users");
            var roles = database.GetCollection<IdentityRole>("roles");
           
			return new ApplicationIdentityContext(users, roles);
		}

		public void Dispose()
		{
		}
	}
}