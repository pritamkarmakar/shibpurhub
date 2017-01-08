using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Http;
using WebApi.OutputCache.Core.Cache;
using WebApi.OutputCache.V2;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    public static class WebApiCacheHelper
    {
        public static void InvalidateCache<T, U>(Expression<Func<T, U>> expression)
        {
            var config = GlobalConfiguration.Configuration;

            // Gets the cache key.
            var outputConfig = config.CacheOutputConfiguration();
            var cacheKey = outputConfig.MakeBaseCachekey(expression);

            // Remove from cache.
            var cache = (config.Properties[typeof(IApiOutputCache)] as Func<IApiOutputCache>)();
            cache.RemoveStartsWith(cacheKey);
        }

        /// <summary>
        /// Invalidate a particular cache using the provided key
        /// </summary>
        /// <param name="key"></param>
        public static void InvalidateCacheByKey(string key)
        {
            var config = GlobalConfiguration.Configuration;
            var cache = (config.Properties[typeof(IApiOutputCache)] as Func<IApiOutputCache>)();
            var exisitingKeys = cache.AllKeys.Where(a => a.Contains(key)).ToList();
            foreach (var exisitingKey in exisitingKeys)
            {
                cache.Remove(exisitingKey);
            }
        }

        /// <summary>
        /// Invalidate multiple cache using the list keys
        /// </summary>
        /// <param name="keys"></param>
        public static void InvalidateCacheByKeys(IList<string> keys)
        {
            var config = GlobalConfiguration.Configuration;
            var cache = (config.Properties[typeof(IApiOutputCache)] as Func<IApiOutputCache>)();

            var allKeys = cache.AllKeys.ToList();
            foreach (var exisitingKey in keys)
            {
                var exactKeys = allKeys.Where(a => a.Contains(exisitingKey)).ToList();
                foreach(var key in exactKeys)
                {
                    cache.Remove(key);
                }
            }
        }

        public static void InvalidatePersonalizedFeedCache(IList<string> userIds)
        {
            var keys = new List<string>();
            foreach(var id in userIds)
            {
                var key = "feed-getpersonalizedfeeds-loggedInUserId=" + id;
                keys.Add(key);
            }

            InvalidateCacheByKeys(keys);
        }
    }
}