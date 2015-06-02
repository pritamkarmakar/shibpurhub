using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServiceStack.Text;

namespace ShibpurConnectWebApp.MongodbOutputCache
{
    public class CachedItem
    {
        public CachedItem(string key, object value, DateTime expireAt)
        {
            Key = key;
            Value = JsonSerializer.SerializeToString(value);
            ValueType = value.GetType().AssemblyQualifiedName;

            ExpireAt = expireAt;
        }

        public string Key { get; set; }

        public string Value { get; set; }
        public string ValueType { get; private set; }

        public DateTime ExpireAt { get; set; }
    }
}