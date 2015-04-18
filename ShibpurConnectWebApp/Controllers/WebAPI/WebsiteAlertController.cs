using Newtonsoft.Json;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models.WebAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;


namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    /// <summary>
    /// Controller to keep all website maintenance, connectivity issue alerts
    /// </summary>
    public class WebsiteAlertController : ApiController
    { 
        public WebsiteAlertController()
        {
            // create the local log file if it doesn't exist
            if(!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "websitealerts.json"))
            {
                File.Create(AppDomain.CurrentDomain.BaseDirectory + "websitealerts.json").Close(); 
            }
        }

        // GET api/websitealert
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/websitealert/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/websitealert
        public IHttpActionResult PostAlert(WebsiteAlert alert)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // make sure there is no similar alert in last 1 hr
            try
            {
                var result = GetExistingAlerts();

                //if there is no existing record then add this one 
                if(result == null)
                {
                    alert.AlertTime = DateTime.UtcNow;
                    SaveNewAlert(alert);

                    EmailsController emailController = new EmailsController();
                    emailController.SendEmail(new Email
                    {
                        Body = alert.Content,
                        Subject = "ShibpurConnect: ALERT !!!",
                        EmailAddress = alert.EmailSentTo
                    });

                    return Ok("sent new email alert");
                }
                else if (result[0].AlertTime.Date == DateTime.UtcNow.Date && (DateTime.UtcNow - result[0].AlertTime).TotalMinutes > 60)
                {
                    alert.AlertTime = DateTime.UtcNow;
                    SaveNewAlert(alert);                   
                   
                    EmailsController emailController = new EmailsController();
                    emailController.SendEmail(new Email
                    {
                        Body = alert.Content,
                        Subject = "ShibpurConnect: ALERT !!!",
                        EmailAddress = alert.EmailSentTo
                    });

                    return Ok("sent new email alert");
                }
                else
                    return Ok("alert already exist, email not sent");
                
            }
            catch (MongoDB.Driver.MongoConnectionException ex)
            {
                return BadRequest(ex.Message);
            }


        }

        /// <summary>
        /// Method to load local json file
        /// </summary>
        private List<WebsiteAlert> GetExistingAlerts()
        {            
            using (StreamReader r = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "websitealerts.json"))
            {
                string json = r.ReadToEnd();
                var items = JsonConvert.DeserializeObject<Alert>(json);

                if (items != null)
                    return items.Lists;
                else
                    return null;
            } 
        }

        /// <summary>
        /// Save new alert
        /// </summary>
        /// <param name="alert"></param>
        private void SaveNewAlert(WebsiteAlert websiteAlert)
        {
            //var items = JsonConvert.DeserializeObject<WebsiteAlert>(websiteAlert);

            Alert alert = new Alert();
            alert.Lists = new List<WebsiteAlert>();
            alert.Lists.Add(websiteAlert);

            // get existing alerts
            var lists = GetExistingAlerts();
            if(lists != null)
            {
                foreach(WebsiteAlert obj in lists)
                {
                    alert.Lists.Add(obj);
                }
            }

            string json = JsonConvert.SerializeObject(alert);
           
            //write string to file
            System.IO.File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "websitealerts.json", json);
        }
    }
}
