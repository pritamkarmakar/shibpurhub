using ShibpurConnectWebApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ShibpurConnectWebApp.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var model = new IndexViewModel
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

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            //ViewBag.Message = "Contact Page";

            return View();
        }
    }
}