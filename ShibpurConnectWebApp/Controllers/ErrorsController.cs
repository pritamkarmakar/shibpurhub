using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ShibpurConnectWebApp.Controllers
{
    public class ErrorsController : Controller
    {
        //
        // GET: /Errors/
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult NotFound()
        {
            ActionResult result;

            object model = Request.Url.PathAndQuery;

            if (!Request.IsAjaxRequest())
                result = View(model);
            else
                result = PartialView("_NotFound", model);

            return result;
        }

        public ActionResult Maintenance()
        {
            return View();
        }

        public ActionResult General(Exception exception)
        {
            // log the error here
            return View(exception);
        }

        public ActionResult Http404()
        {
            return View("Http404");
        }

        public ActionResult Http403()
        {
            return View("Http403");
        }
	}
}