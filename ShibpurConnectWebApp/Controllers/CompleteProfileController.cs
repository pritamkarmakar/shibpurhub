using ShibpurConnectWebApp.Controllers.WebAPI;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models.WebAPI;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Http.Results;
using System.Web.Mvc;

namespace ShibpurConnectWebApp.Controllers
{
    public class CompleteProfileController : Controller
    {
        public CompleteProfileController()
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
        }

        // GET: Registration
        public ActionResult Index()
        {
            return View();
        }

        // GET: Registration/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Registration/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Registration/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Registration/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Registration/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Registration/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Registration/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
