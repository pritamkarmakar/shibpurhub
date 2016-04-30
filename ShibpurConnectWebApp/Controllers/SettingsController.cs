using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Http.Results;
using System.Web.Mvc;
using ShibpurConnectWebApp.Controllers.WebAPI;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models.WebAPI;

namespace ShibpurConnectWebApp.Controllers
{
    public class SettingsController : Controller
    {

        public SettingsController()
        {
            // get the department list and send it to the view
            DepartmentsController DP = new DepartmentsController();
            var actionResult = DP.GetDepartments();
            var departmentList = actionResult as OkNegotiatedContentResult<List<Departments>>;

            WebAPI.TagsController categoriesController = new WebAPI.TagsController();
            var actionResult3 = categoriesController.GetTags();
            var categoryList = actionResult3 as OkNegotiatedContentResult<List<Categories>>;

            // if there is no categories in the db then add the default categories
            if (categoryList != null && categoryList.Content.Count == 0)
            {
                var _mongoHelper = new MongoHelper<Categories>();
                foreach (var category in ConfigurationManager.AppSettings["categories"].Split(','))
                {
                    Categories obj = new Categories();
                    obj.CategoryName = category;

                    _mongoHelper.Collection.Save(obj);
                }
            }

            ViewBag.Departments = departmentList.Content;

            // add all BEC names into viewbag that we will use to populate 'Add Education' form dropdown
            var _mongounHelper = new MongoHelper<UniversityName>();
            var unlist = _mongounHelper.Collection.FindAllAs<UniversityName>().ToList();
            ViewBag.UniversityNames = unlist;
        }          
    

        // GET: Settings
        public ActionResult Index()
        {
            return View();
        }
    }
}