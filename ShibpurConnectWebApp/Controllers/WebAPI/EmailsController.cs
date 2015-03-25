using Microsoft.AspNet.Identity;
using SendGrid;
using ShibpurConnectWebApp.Models.WebAPI;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    public class EmailsController : ApiController
    {
        private ApplicationUserManager _userManager;
        // GET: api/Emails
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/Emails/5
        public string Get(int id)
        {
            return "value";
        }

        // POST: api/Emails
        public void PostEmails(Email email)
        {
            //await UserManager.SendEmailAsync("", "Hello from ShibpurConnect", "Thanks for joining ShibpurConnect. You have to confirm your account to use ShibpurConnect. To confirm your account please click <a href=\"" + callbackUrl + "\">here</a> <br/> <br/><br/>Regards, <br/>2kChakka");
        }

        // PUT: api/Emails/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/Emails/5
        public void Delete(int id)
        {
        }

        public Task SendAsync(Email message)
        {
            // Plug in your email service here to send an email.
            //return Task.FromResult(0);
            return configSendGridasync(message);
        }

        private async Task configSendGridasync(Email message)
        {
            //var myMessage = new MailMessage("pritam83@gmail.com", "metricsqa@gmail.com");
            var myMessage = new SendGridMessage();
            myMessage.AddTo(message.EmailAddress);
            myMessage.From = new System.Net.Mail.MailAddress(
                                "pritam83@gmail.com", "ShibpurConnect");
            myMessage.Subject = message.Subject;
            myMessage.Text = message.Body;
            myMessage.Html = message.Body;

            var credentials = new NetworkCredential(
                       ConfigurationManager.AppSettings["mailAccount"],
                       ConfigurationManager.AppSettings["mailPassword"]
                       );

            // Create a Web transport for sending email.
            var transportWeb = new Web(credentials);

            // Send the email.
            if (transportWeb != null)
            {
                await transportWeb.DeliverAsync(myMessage);
            }
            else
            {
                Trace.TraceError("Failed to create Web transport.");
                await Task.FromResult(0);
            }
        }
    }
}
