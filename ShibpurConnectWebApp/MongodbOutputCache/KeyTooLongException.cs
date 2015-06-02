using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShibpurConnectWebApp.MongodbOutputCache
{
    public class KeyTooLongException : ArgumentException
    {
        public KeyTooLongException()
            : base("The key provided was over the 1024 bytes maximum for an indexed MongoDb field")
        { }
    }
}