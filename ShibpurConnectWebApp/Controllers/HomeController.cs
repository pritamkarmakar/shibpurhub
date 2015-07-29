using System.Web.Mvc;

namespace ShibpurConnectWebApp.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            if(User.Identity.IsAuthenticated)
            {
               return RedirectToAction("Index", "Feed");
            }

            return View();
        }

        public ActionResult Maintenance()
        {
            return View();
        }

        public ActionResult LearnMore()
        {
            return View();
        }
    }
}