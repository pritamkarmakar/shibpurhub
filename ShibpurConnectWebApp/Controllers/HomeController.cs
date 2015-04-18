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

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            return View();
        }

        public ActionResult Maintenance()
        {
            return View();
        }
    }
}