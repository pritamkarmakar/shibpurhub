using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ShibpurConnectWebApp.Models;

namespace ShibpurConnectWebApp.Controllers
{
    public class FeedController : Controller
    {
        // GET: Feed
        public ActionResult Index()
        {
            var model = new FeedViewModel
            {
                Threads = new List<DiscussionThread> 
                { 
                    new DiscussionThread 
                    {
                        AskedBy = "Pritam", 
                        Question="Who is working on authentication?", 
                        DetailText = "Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book.",
                        Categories=new List<string>{"General"}, 
                        DatePosted=new DateTime(2015,2,15)
                    },
                    new DiscussionThread 
                    {
                        AskedBy = "Sukanta", 
                        Question="Where should we keep the database?", 
                        DetailText = "Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book.",
                        Categories=new List<string>{"General"},
                        DatePosted=new DateTime(2015,2,15)
                    },
                    new DiscussionThread 
                    {
                        AskedBy = "Subrata", 
                        Question="What are the technologies being used?", 
                        DetailText = "Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book.",
                        Categories=new List<string>{"General"}, 
                        DatePosted=new DateTime(2015,2,10)
                    },
                    new DiscussionThread 
                    {
                        AskedBy = "Arindam", 
                        Question="What is the budget of this website?", 
                        DetailText = "Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book.",
                        Categories=new List<string>{"General"}, 
                        DatePosted=new DateTime(2015,2,12)
                    },
                    new DiscussionThread 
                    {
                        AskedBy = "Monotosh", 
                        Question="When this website is going to be released?", 
                        DetailText = "Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book.",
                        Categories=new List<string>{"General"}, 
                        DatePosted=new DateTime(2015,2,9)
                    },
                }
            };
            return View(model);
        }

        // GET: Feed/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Feed/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Feed/Create
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

        // GET: Feed/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Feed/Edit/5
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

        // GET: Feed/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Feed/Delete/5
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
