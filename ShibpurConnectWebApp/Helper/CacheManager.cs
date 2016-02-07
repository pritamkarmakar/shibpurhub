using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.Caching;
using System.Web;

namespace ShibpurConnectWebApp.Helper
{
    public static class CacheManager
    {
        const string CacheKey = "CacheKey";
        static readonly object cacheLock = new object();
        static readonly object cacheLock2 = new object();
        static readonly object cacheLock3 = new object();

        // read the cache expiry time
        static int cacheExpiry = Convert.ToInt16(ConfigurationManager.AppSettings["MemoryCacheExpiry"]);

        public static object GetCachedData(string cacheKey)
        {

            //Returns null if the string does not exist, prevents a race condition where the cache invalidates between the contains check and the retreival.
            var cachedString = MemoryCache.Default.Get(cacheKey, null);

            if (cachedString != null)
            {
                return cachedString;
            }

            lock (cacheLock)
            {
                //Check to see if anyone wrote to the cache while we where waiting our turn to write the new value.
                cachedString = MemoryCache.Default.Get(cacheKey, null);

                if (cachedString != null)
                {
                    return cachedString;
                }

                return null;
            }
        }

        /// <summary>
        /// Method to save cache data, if it is not present
        /// </summary>
        public static void SetCacheData(string key, object value)
        {
            // check if the key already exist then no need to do this operation
            var cachedString = MemoryCache.Default.Get(key, null);

            if (cachedString == null)
            {
                lock (cacheLock2)
                {
                    //The value still did not exist so we now write it in to the cache.
                    CacheItemPolicy cip = new CacheItemPolicy()
                    {
                        AbsoluteExpiration = new DateTimeOffset(DateTime.Now.AddMinutes(cacheExpiry))
                    };
                    MemoryCache.Default.Set(key, value, cip);
                }
            }
        }

        /// <summary>
        /// Method to overwrite cache data even it is already exist
        /// </summary>
        private static void OverwriteCacheData(string key, string value)
        {
            lock (cacheLock3)
            {
                //The value still did not exist so we now write it in to the cache.
                CacheItemPolicy cip = new CacheItemPolicy()
                {
                    AbsoluteExpiration = new DateTimeOffset(DateTime.Now.AddMinutes(cacheExpiry))
                };
                MemoryCache.Default.Set(key, value, cip);
            }
        }
    }
}