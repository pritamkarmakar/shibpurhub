using System;
using System.Collections.Generic;
using System.Web.Mvc;
using ShibpurConnectWebApp.Models;

namespace ShibpurConnectWebApp.Controllers
{
    public class FeedController : Controller
    {
        // GET: Feed
        public ActionResult Index()
        {
            TempData["SelectedPage"] = "Threads";
            return View();
        }

        // GET: Feed/Details/5
        [SlugToId]
        [ActionName("Details")]
        public ActionResult Details(string id)
        {
            TempData["SelectedPage"] = "Threads";            

            ViewData["questionId"] = id;
            return View();
        }

        [ActionName("DetailsWithAnswerID")]
        public ActionResult Details(string id, string answerId)
        {
            TempData["SelectedPage"] = "Threads";

            ViewData["questionId"] = id;
            if (!string.IsNullOrEmpty(answerId))
            {
                ViewData["answerId"] = answerId;
            }

            return View("Details");
        }

        // GET: Feed/Create
        public ActionResult StartDiscussion()
        {
            TempData["SelectedPage"] = "StartDiscussion";
            return View();
        }

        public ActionResult MyFeeds()
        {
            TempData["SelectedPage"] = "Threads";
            return View();
        }

        // POST: Feed/Create
        [HttpPost]
        public ActionResult StartDiscussion(FormCollection collection)
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

        public ActionResult Categories(string category)
        {
            TempData["SelectedPage"] = "Categories";
            return View();
        }

        public ActionResult FeedByCategory(string category)
        {
            // if user asking about Feed/FeedByCategory page without any category name then redirect to feed index page
            if(string.IsNullOrEmpty(category))
                return RedirectToAction("Index");

            ViewData["selectedtag"] = category;
            TempData["SelectedPage"] = "Threads";
            return View();
        }

        public ActionResult Users()
        {
            TempData["SelectedPage"] = "Users";
            return View();
        }
    }
}
