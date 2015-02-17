using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace SCDataAccess
{
    public class LocalMemoryCache : ICache, IDisposable
    {
        private MemoryCache cache;
        private bool disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalMemoryCache" /> class.
        /// </summary>
        public LocalMemoryCache()
        {
            cache = MemoryCache.Default;
        }

        /// <summary>
        /// Puts the specified key/value pair in the cache
        /// with no expiration.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Put<T>(string key, T value)
        {
            // In memory cache is only 
            cache.Set(key, value, new DateTimeOffset(DateTime.UtcNow.AddMinutes(45)));
        }

        /// <summary>
        /// Removes a specifc entry from the cache
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns></returns>
        public bool Remove(string key)
        {
            return cache.Remove(key) != null;
        }

        /// <summary>
        /// Puts the specified key/value pair in the cache with the given expiration period.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="expiration">The expiration.</param>
        public void Put<T>(string key, T value, TimeSpan expiration)
        {
            cache.Set(key, value, new DateTimeOffset(DateTime.UtcNow.Add(expiration)));
        }

        /// <summary>
        /// Gets value for the specified key from the cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public T Get<T>(string key)
        {
            object value = this.cache.Get(key);
            if (value == null)
            {
                return default(T);
            }

            return (T)value;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed && disposing)
            {
                this.cache.Dispose();
                disposed = true;
            }
        }


        public T Get<T>(string key, out bool retrievedFromCache)
        {
            object value = this.cache.Get(key);
            if (value == null)
            {
                retrievedFromCache = false;
                return default(T);
            }

            retrievedFromCache = true;
            return (T)value;
        }

        /// <summary>
        /// Gets CacheItem for the specified key from the cache.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>(object)CacheItem</returns>
        public object GetCacheItem(string key)
        {
            return this.cache.GetCacheItem(key);
        }

        /// <summary>
        /// Gets all values for the specified key from the cache.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>List of T items</returns>
        public IEnumerable<T> GetAll<T>(string key)
        {
            object value = this.cache.Get(key) as IEnumerable<T>;
            if (value == null)
            {
                return default(IEnumerable<T>);
            }

            return (IEnumerable<T>)value;
        }

        public IEnumerable<T> GetAll<T>(string key, out bool retrievedFromCache)
        {
            object value = this.cache.Get(key) as IEnumerable<T>;
            if (value == null)
            {
                retrievedFromCache = false;
                return default(IEnumerable<T>);
            }

            retrievedFromCache = true;
            return (IEnumerable<T>)value;
        }
    }
}
