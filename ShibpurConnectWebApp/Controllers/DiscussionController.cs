using Microsoft.AspNet.Identity;
using System.Configuration;
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
            var name = User.Identity.GetUserName();
            var admins = ConfigurationManager.AppSettings["adminsEmail"];
            if (!string.IsNullOrEmpty(name))
            {
                ViewBag.IsAdmin = admins.Contains(name);
            }
            TempData["SelectedPage"] = "Threads";
            ViewData["questionId"] = id;
            ViewData["questionTitle"] = RouteData.Values["title"];
            return View();
        }

        /// <summary>
        /// We use this action for the feed content appear in the activity page.
        /// For example when user will click on someone's answer feed then this action will be called
        /// </summary>
        /// <param name="id"></param>
        /// <param name="answerId"></param>
        /// <returns></returns>
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