using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ShibpurConnectWebApp.Controllers
{
    public class TagsController : Controller
    {
        // GET: Categories
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult NewTags()
        {
            return View();
        }
    }
}