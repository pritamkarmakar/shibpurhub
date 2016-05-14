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
        public ActionResult DiscussionDetail(string id, string title)
        {
            var name = User.Identity.GetUserName();
            var admins = ConfigurationManager.AppSettings["adminsEmail"];
            if (!string.IsNullOrEmpty(name))
            {
                ViewBag.IsAdmin = admins.Contains(name);
            }
            TempData["SelectedPage"] = "Threads";
            ViewData["questionId"] = id;
            ViewData["questionTitle"] = title;
            return View();
        }

        /*Not sure why we created this controller. Commenting it for now as I need same method signature in above function*/
        //[ActionName("DiscussionDetailWithAnswerID")]
        //public ActionResult DiscussionDetail(string id, string answerId)
        //{
        //    TempData["SelectedPage"] = "Threads";

        //    ViewData["questionId"] = id;
        //    if (!string.IsNullOrEmpty(answerId))
        //    {
        //        ViewData["answerId"] = answerId;
        //    }

        //    return View("DiscussionDetail");
        //}
    }
}