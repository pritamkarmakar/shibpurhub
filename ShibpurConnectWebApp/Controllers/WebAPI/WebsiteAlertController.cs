using Newtonsoft.Json;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models.WebAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
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

        /// <summary>
        /// Send email notification for any outage. This method validate if there is already email sent for the same 
        /// error in last one hour then don't send again
        /// </summary>
        /// <param name="alert"></param>
        /// <returns></returns>
        public async Task<IHttpActionResult> SendEmailNotificationForOutage(WebsiteAlert alert)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // make sure there is no similar alert in last 1 hr
            try
            {
                var result = GetExistingAlerts();

                //if there is no existing record then send alert 
                if(result == null)
                {
                    alert.AlertTime = DateTime.UtcNow;
                    EmailsController emailController = new EmailsController();
                    await emailController.SendEmail(new Email
                    {
                        Body = alert.Content,
                        Subject = "ShibpurHub: ALERT !!!",
                        UserId = alert.EmailSentTo
                    });

                    return Ok("sent new email alert");
                }
                else 
                {
                    // check the latest entry and decide whether we should send a new alert or not
                    // find the latest record for this type of alert
                    var latestAlert = (from m in result
                                      where m.Source == "MongoDB.Driver"
                                      orderby m.AlertTime descending
                                      select m).ToList();

                    if (latestAlert.Count > 0)
                    {
                        if (latestAlert[0].AlertTime.Date == DateTime.UtcNow.Date && (DateTime.UtcNow - latestAlert[0].AlertTime).TotalMinutes > 60)
                        {
                            alert.AlertTime = DateTime.UtcNow;        

                            EmailsController emailController = new EmailsController();
                            await emailController.SendEmail(new Email
                            {
                                Body = alert.Content,
                                Subject = "ShibpurHub: ALERT !!!",
                                UserId = alert.EmailSentTo
                            });

                            return Ok("sent new email alert");
                        }
                        else
                        {
                            return Ok("same alert already sent within last 1 hour");
                        }
                    }
                    else
                    {
                        // there is no record for this error, send alert and save it as well
                        alert.AlertTime = DateTime.UtcNow;
    
                        EmailsController emailController = new EmailsController();
                        await emailController.SendEmail(new Email
                        {
                            Body = alert.Content,
                            Subject = "ShibpurHub: ALERT !!!",
                            UserId = alert.EmailSentTo
                        });

                        return Ok("sent new email alert");
                    }
                }              
                
            }
            catch (Exception ex)
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
        public IHttpActionResult SaveNewAlert(WebsiteAlert websiteAlert)
        {
            try
            {
                Alert alert = new Alert();
                alert.Lists = new List<WebsiteAlert>();
                alert.Lists.Add(websiteAlert);

                // get existing alerts
                var lists = GetExistingAlerts();
                if (lists != null)
                {
                    foreach (WebsiteAlert obj in lists)
                    {
                        alert.Lists.Add(obj);
                    }
                }

                string json = JsonConvert.SerializeObject(alert);

                //write string to file
                System.IO.File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "websitealerts.json", json);

                return Ok();
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
