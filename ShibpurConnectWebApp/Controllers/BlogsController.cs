using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ShibpurConnectWebApp.Controllers
{
    public class BlogsController : Controller
    {
        // GET: Blogs
        public ActionResult Index()
        {
            return View();
        }

        // GET: Blogs/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Blogs/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Blogs/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
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

        // GET: Blogs/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Blogs/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Blogs/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Blogs/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
