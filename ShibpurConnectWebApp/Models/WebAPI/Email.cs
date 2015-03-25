using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    [Serializable]
    public class Email
    {
        [DataMember]
        public string EmailAddress { get; set; }

        [DataMember]
        public string Subject { get; set; }

        [DataMember]
        public string Body { get; set; }
    }
}