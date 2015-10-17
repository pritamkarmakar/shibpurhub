using System.Web.Mvc;

namespace ShibpurConnectWebApp.Controllers
{
    public class CareerController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult PostJob(FormCollection collection)
        {
            return View();
        } 
    }
}