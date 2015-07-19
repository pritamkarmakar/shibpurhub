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
        private string emailTemplate = ConfigurationManager.AppSettings["emailTemplate"];

        /// <summary>
        /// API to send email to end user
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        [Authorize]
        public async Task SendEmail(Email message)
        {
            // Plug in your email service here to send an email.
            //return Task.FromResult(0);
            await configSendGridasync(message);
        }

        private async Task configSendGridasync(Email message)
        {
            var myMessage = new SendGridMessage();
            Helper.Helper helper = new Helper.Helper();
            // split the user ids, if there are multiples
            foreach (string userId in message.UserId.Split(','))
            {
                // find the email address of the user from the userid
                var userInfo = await helper.FindUserById(userId);
                //var userInfo = actionResult.;
                myMessage.AddTo(userInfo.Email);
            }           
           
            myMessage.From = new System.Net.Mail.MailAddress(
                                "info@shibpurhub.com", "ShibpurHub");
            myMessage.Subject = message.Subject;
            myMessage.Text = message.Body;
            myMessage.Html= "<p>" + message.Body + "</p>";
            myMessage.EnableTemplateEngine(emailTemplate);

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
