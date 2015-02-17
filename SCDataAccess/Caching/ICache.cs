using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCDataAccess
{
    /// <summary>
    /// Cache interface
    /// </summary>
    public interface ICache
    {
        /// <summary>
        /// Puts the specified key/value pair in the cache
        /// with no expiration.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        void Put<T>(string key, T value);

        /// <summary>
        /// Removes a specific entry from the cache
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns></returns>
        bool Remove(string key);

        /// <summary>
        /// Puts the specified key/value pair in the cache with the given expiration period.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="expiration">The expiration.</param>
        void Put<T>(string key, T value, TimeSpan expiration);

        /// <summary>
        /// Gets value for the specified key from the cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        T Get<T>(string key);

        /// <summary>
        /// Gets value for the specified key from the cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <param name="retrievedFromCache">if set to <c>true</c> if retrieved from cache.
        /// This is primarily to denote if primitive values (e.g. false) are retrieved
        /// from the cache or not</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Get")]
        T Get<T>(string key, out bool retrievedFromCache);

        /// <summary>
        /// Gets CacheItem for the specified key from the cache.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>(object)CacheItem or (object)DataCacheItem</returns>
        object GetCacheItem(string key);

        /// <summary>
        /// Gets all values for the specified key from the cache.
        /// </summary>
        /// <typeparam name="T">The T Type</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>Collection of T items</returns>
        IEnumerable<T> GetAll<T>(string key);

        /// <summary>
        /// Gets all values for the specified key from the cache.
        /// </summary>
        /// <typeparam name="T">The T Type</typeparam>
        /// <param name="key">The key</param>
        /// <param name="retrievedFromCache">Is retrieved From Cache</param>
        /// <returns>Collection of T items</returns>
        IEnumerable<T> GetAll<T>(string key, out bool retrievedFromCache);
    }
}
