using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ShibpurConnectWebApp.Controllers
{
    public class DiscussionController : Controller
    {
        // GET: Discussion
        public ActionResult Index()
        {
            return View();
        }

        [SlugToId]
        [ActionName("DiscussionDetail")]
        public ActionResult DiscussionDetail(string id)
        {
            TempData["SelectedPage"] = "Threads";

            ViewData["questionId"] = id;
            return View();
        }

        [ActionName("DiscussionDetailWithAnswerID")]
        public ActionResult DiscussionDetail(string id, string answerId)
        {
            TempData["SelectedPage"] = "Threads";

            ViewData["questionId"] = id;
            if (!string.IsNullOrEmpty(answerId))
            {
                ViewData["answerId"] = answerId;
            }

            return View("DiscussionDetail");
        }
    }
}