using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SCDataAccess
{
    /// <summary>
    /// Cache helper class
    /// </summary>
    public static class CacheHelper
    {
        //private static ITransientErrorDetectionStrategy errorDetectionStrategy = new CacheTransientErrorDetectionStrategy();
        //private static readonly int retryCount = Convert.ToInt32(ConfigurationManager.AppSettings.Get("CacheRetryPolicyCount"), CultureInfo.InvariantCulture);
        //private static readonly int retryTimeInterval = Convert.ToInt32(ConfigurationManager.AppSettings.Get("CacheRetryTimeIntervalInMilliseconds"), CultureInfo.InvariantCulture);
        //private static RetryPolicy policy = new RetryPolicy(errorDetectionStrategy, retryCount, TimeSpan.FromMilliseconds(retryTimeInterval));

        /// <summary>
        /// Gets from cache or store.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cache">The cache.</param>
        /// <param name="getFromStore">The get from store.</param>
        /// <param name="cacheKeyFormat">The cache key format.</param>
        /// <param name="cacheKeyArgs">The cache key args.</param>
        /// <returns></returns>
        public static T GetFromCacheOrStore<T>(this ICache cache, Func<T> getFromStore, string cacheKeyFormat,
                                               params object[] cacheKeyArgs)
        {
            string cacheKey = string.Format(CultureInfo.InvariantCulture, cacheKeyFormat, cacheKeyArgs);
            bool retrievedFromCache = false;
            T value;

            value = cache.Get<T>(cacheKey, out retrievedFromCache);

            if (!retrievedFromCache)
            {
                value = getFromStore();
                cache.Put(cacheKey, value);
            }

            return value;
        }

        /// <summary>
        /// Gets from cache or store.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cache">The cache.</param>
        /// <param name="getFromStore">The get from store.</param>
        /// <param name="shouldPutValueFromStoreInCache">The should put value from store in cache.</param>
        /// <param name="cacheKeyFormat">The cache key format.</param>
        /// <param name="cacheKeyArgs">The cache key args.</param>
        /// <returns></returns>
        public static T GetFromCacheOrStore<T>(this ICache cache, Func<T> getFromStore, Func<T, bool> shouldPutValueFromStoreInCache, string cacheKeyFormat,
                                               params object[] cacheKeyArgs)
        {
            string cacheKey = string.Format(CultureInfo.InvariantCulture, cacheKeyFormat, cacheKeyArgs);
            bool retrievedFromCache = false;
            T value;

            value = cache.Get<T>(cacheKey, out retrievedFromCache);

            if (!retrievedFromCache)
            {
                value = getFromStore();

                if (shouldPutValueFromStoreInCache(value))
                {
                    cache.Put(cacheKey, value);
                }
            }

            return value;
        }

        /// <summary>
        /// Gets from cache or store.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cache">The cache.</param>
        /// <param name="getFromStore">The get from store.</param>
        /// <param name="shouldPutValueFromStoreInCache">The should put value from store in cache.</param>
        /// <param name="cacheKeyFormat">The cache key format.</param>
        /// <param name="cacheKeyArgs">The cache key args.</param>
        /// <returns></returns>
        public static T GetFromCacheOrStore<T>(this ICache cache, Func<T> getFromStore, Func<T, bool> shouldPutValueFromStoreInCache, TimeSpan expiration, string cacheKeyFormat,
                                               params object[] cacheKeyArgs)
        {
            string cacheKey = string.Format(CultureInfo.InvariantCulture, cacheKeyFormat, cacheKeyArgs);
            bool retrievedFromCache = false;
            T value;

            value = cache.Get<T>(cacheKey, out retrievedFromCache);

            if (!retrievedFromCache)
            {
                value = getFromStore();
                if (shouldPutValueFromStoreInCache(value))
                {
                    cache.Put(cacheKey, value, expiration);
                }
            }

            return value;
        }

        ///// <summary>
        ///// Gets all from cache or store.
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="cache">The cache.</param>
        ///// <param name="getFromStore">The get from store.</param>
        ///// <param name="shouldPutValueFromStoreInCache">The should put value from store in cache.</param>
        ///// <param name="cacheKeyFormat">The cache key format.</param>
        ///// <param name="cacheKeyArgs">The cache key args.</param>
        ///// <returns></returns>
        //public static IEnumerable<T> GetAllFromCacheOrStore<T>(this ICache cache, Func<IEnumerable<T>> getFromStore, Func<IEnumerable<T>, bool> shouldPutValueFromStoreInCache, TimeSpan expiration, string cacheKeyFormat,
        //                                       params object[] cacheKeyArgs)
        //{
        //    string cacheKey = string.Format(CultureInfo.InvariantCulture, cacheKeyFormat, cacheKeyArgs);
        //    bool retrievedFromCache = false;
        //    IEnumerable<T> value;

        //    value = policy.ExecuteAction<IEnumerable<T>>(() =>
        //    {
        //        return cache.GetAll<T>(cacheKey, out retrievedFromCache);
        //    });

        //    if (!retrievedFromCache)
        //    {
        //        value = getFromStore();

        //        if (shouldPutValueFromStoreInCache(value))
        //        {
        //            policy.ExecuteAction(() =>
        //            {
        //                cache.Put(cacheKey, value, expiration);
        //            });
        //        }
        //    }

        //    return value;
        //}

        ///// <summary>
        ///// Gets from cache or store.
        ///// </summary>
        ///// <typeparam name="T">The item type being retrieved.</typeparam>
        ///// <param name="cache">The cache.</param>
        ///// <param name="getFromStoreAsync">The get from store.</param>
        ///// <param name="cacheKeyFormat">The cache key format.</param>
        ///// <param name="cacheKeyArgs">The cache key args.</param>
        ///// <returns>The asynchronous task to await on that contains the item from the cache or store.</returns>
        //public static async Task<T> GetFromCacheOrStoreAsync<T>(
        //    this ICache cache,
        //    Func<Task<T>> getFromStoreAsync,
        //    TimeSpan expiration,
        //    string cacheKeyFormat,
        //    params object[] cacheKeyArgs)
        //{
        //    return await GetFromCacheOrStoreAsync<T>(
        //        cache,
        //        getFromStoreAsync,
        //        (s) => true,
        //        expiration,
        //        cacheKeyFormat,
        //        cacheKeyArgs);
        //}

        ///// <summary>
        ///// Gets from cache or store.
        ///// </summary>
        ///// <typeparam name="T">The item type being retrieved.</typeparam>
        ///// <param name="cache">The cache.</param>
        ///// <param name="getFromStoreAsync">The get from store.</param>
        ///// <param name="shouldPutValueFromStoreInCache">The should put value from store in cache.</param>
        ///// <param name="cacheKeyFormat">The cache key format.</param>
        ///// <param name="cacheKeyArgs">The cache key args.</param>
        ///// <returns>The asynchronous task to await on that contains the item from the cache or store.</returns>
        //public static async Task<T> GetFromCacheOrStoreAsync<T>(
        //    this ICache cache,
        //    Func<Task<T>> getFromStoreAsync,
        //    Func<T, bool> shouldPutValueFromStoreInCache,
        //    TimeSpan expiration,
        //    string cacheKeyFormat,
        //    params object[] cacheKeyArgs)
        //{
        //    string cacheKey = string.Format(CultureInfo.InvariantCulture, cacheKeyFormat, cacheKeyArgs);
        //    bool retrievedFromCache = false;
        //    T value;

        //    value = policy.ExecuteAction<T>(() =>
        //    {
        //        return cache.Get<T>(cacheKey, out retrievedFromCache);
        //    });

        //    if (!retrievedFromCache)
        //    {
        //        value = await getFromStoreAsync();

        //        if (shouldPutValueFromStoreInCache(value))
        //        {
        //            policy.ExecuteAction(() =>
        //            {
        //                cache.Put(cacheKey, value, expiration);
        //            });
        //        }
        //    }

        //    return value;
        //}

        ///// <summary>
        ///// Gets from cache or store.
        ///// </summary>
        ///// <typeparam name="T">The item type being retrieved.</typeparam>
        ///// <param name="cache">The cache.</param>
        ///// <param name="getFromStoreAsync">The get from store.</param>
        ///// <param name="shouldPutValueFromStoreInCache">The should put value from store in cache.</param>
        ///// <param name="cacheKeyFormat">The cache key format.</param>
        ///// <param name="cacheKeyArgs">The cache key args.</param>
        ///// <returns>The asynchronous task to await on that contains the item from the cache or store.</returns>
        //public static async Task<T> GetFromCacheOrStoreAsync<T>(
        //    this ICache cache,
        //    Func<Task<T>> getFromStoreAsync,
        //    Func<T, bool> shouldPutValueFromStoreInCache,
        //    string cacheKeyFormat,
        //    params object[] cacheKeyArgs)
        //{
        //    string cacheKey = string.Format(CultureInfo.InvariantCulture, cacheKeyFormat, cacheKeyArgs);
        //    bool retrievedFromCache = false;
        //    T value;

        //    value = policy.ExecuteAction<T>(() =>
        //    {
        //        return cache.Get<T>(cacheKey, out retrievedFromCache);
        //    });

        //    if (!retrievedFromCache)
        //    {
        //        value = await getFromStoreAsync();

        //        if (shouldPutValueFromStoreInCache(value))
        //        {
        //            policy.ExecuteAction(() =>
        //            {
        //                cache.Put(cacheKey, value);
        //            });
        //        }
        //    }

        //    return value;
        //}
    }
}
