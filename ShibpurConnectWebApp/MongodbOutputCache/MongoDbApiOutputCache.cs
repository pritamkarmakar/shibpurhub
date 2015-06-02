﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using WebApi.OutputCache.Core.Cache;

namespace ShibpurConnectWebApp.MongodbOutputCache
{
    public class MongoDbApiOutputCache : IApiOutputCache
    {
        internal readonly MongoCollection MongoCollection;

        public MongoDbApiOutputCache(MongoDatabase mongoDatabase)
            : this(mongoDatabase, "cache")
        { }

        static MongoDbApiOutputCache()
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(CachedItem)))
                BsonClassMap.RegisterClassMap<CachedItem>(cm =>
                {
                    cm.MapIdField(x => x.Key);
                    cm.MapProperty(x => x.Value).SetElementName("value");
                    cm.MapProperty(x => x.ExpireAt).SetElementName("expireAt");
                    cm.MapField(x => x.ValueType).SetElementName("valueType");

                    cm.SetIgnoreExtraElements(true);
                });
        }

        public MongoDbApiOutputCache(MongoDatabase mongoDatabase, string cacheCollectionName)
        {
            MongoCollection = mongoDatabase.GetCollection(cacheCollectionName);

            MongoCollection.EnsureIndex(
                IndexKeys.Ascending("expireAt"),
                IndexOptions.SetTimeToLive(TimeSpan.FromMilliseconds(0))
                );
        }

        public void RemoveStartsWith(string key)
        {
            MongoCollection.Remove(Query.Matches("_id", new BsonRegularExpression("^" + key)));
        }

        public T Get<T>(string key) where T : class
        {
            var item = MongoCollection
                .FindOneAs<CachedItem>(Query.EQ("_id", new BsonString(key)));

            if (item == null)
                return null;

            return CheckItemExpired(item)
                ? null
                : JsonSerializer.DeserializeFromString<T>(item.Value);
        }

        public object Get(string key)
        {            
            var item = MongoCollection
                .FindOneAs<CachedItem>(Query.EQ("_id", new BsonString(key)));

            var type = Type.GetType(item.ValueType);
            return JsonSerializer.DeserializeFromString(item.Value, type);
        }

        public void Remove(string key)
        {
            MongoCollection.Remove(Query.EQ("_id", new BsonString(key)));
        }

        public bool Contains(string key)
        {
            return MongoCollection
                .FindAs<CachedItem>(Query.EQ("_id", new BsonString(key)))
                .Count() == 1;
        }

        public void Add(string key, object o, DateTimeOffset expiration, string dependsOnKey = null)
        {
            if (key.Length > 256) //saves calling getByteCount if we know it could be less than 1024 bytes
                if (Encoding.UTF8.GetByteCount(key) >= 1024)
                    throw new KeyTooLongException();

            var cachedItem = new CachedItem(key, o, expiration.DateTime);

            MongoCollection.Save(cachedItem);
        }

        private bool CheckItemExpired(CachedItem item)
        {
            if (item.ExpireAt >= DateTime.Now)
                return false;

            //does the work of TTL collections early - TTL is only "fired" once a minute or so
            MongoCollection.Remove(Query.EQ("_id", item.Key));

            return true;
        }

        public IEnumerable<string> AllKeys
        {
            get
            {
                return MongoCollection.FindAllAs<CachedItem>().Select(m => m.Key);
            }
        }
    }
}