using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ShibpurConnectWebApp.Controllers
{
    public class LegalController : Controller
    {
        // GET: Legal
        public ActionResult TOS()
        {
            return View();
        }

        public ActionResult Privacy()
        {
            return View();
        }
    }
}