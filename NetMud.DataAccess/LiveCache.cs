﻿using NetMud.DataStructure.Base.Entity;
using NetMud.DataStructure.Base.Place;
using NetMud.DataStructure.Base.System;
using NetMud.DataStructure.Behaviors.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace NetMud.DataAccess
{
    public class LiveCache
    {
        /* 
         * The general idea here is that we are literally caching everything possible from app start.
         * 
         * We'll need to cache collections of references to things and the things themselves.
         * Caching collections of things will result in flipping the cache constantly
         * 
         * The administrative website will edit the reference data in the database which wont get refreshed
         * until someone tells it to (or the entire thing reboots)
         * 
         * IEntity data is ALWAYS cached and saved to a different place because it is live in-game data and even if
         * we add, say, one damage to the Combat Knife item in the db it doesn't mean all Combat Knife objects in game
         * get retroactively updated. There will be superadmin level website commands to do this and in-game commands for admins.
         * 
         */
        private ObjectCache globalCache;
        private CacheItemPolicy globalPolicy;

        public LiveCache()
        {
            globalCache = MemoryCache.Default;
            globalPolicy = new CacheItemPolicy();
        }

        public bool PreLoadAll<T>() where T : IData
        {
            var backingClass = Activator.CreateInstance(typeof(T)) as IEntityBackingData;

            var implimentingEntityClass = backingClass.EntityClass;

            var dataBacker = new DataAccess.DataWrapper();

            foreach(IData thing in dataBacker.GetAll<T>())
            {
                var entityThing = Activator.CreateInstance(implimentingEntityClass, new object[] { (T)thing }) as IEntity;

                var cacheKey = new LiveCacheKey(implimentingEntityClass, entityThing.BirthMark);

                globalCache.AddOrGetExisting(cacheKey.KeyHash(), entityThing, globalPolicy);
            }

            return true;
        }

        public T Add<T>(T objectToCache) where T : IEntity
        {
            var cacheKey = new LiveCacheKey(typeof(T), objectToCache.BirthMark);

           return (T)globalCache.AddOrGetExisting(cacheKey.KeyHash(), objectToCache, globalPolicy);
        }

        public IEnumerable<T> GetAll<T>()
        {
            return globalCache.Where(keyValuePair => keyValuePair.Value.GetType() == typeof(T)).Select(kvp => (T)kvp.Value);
        }

        /// <summary>
        /// When base type and maintype want to be less ambigious
        /// </summary>
        /// <typeparam name="T">The base type (like ILocation)</typeparam>
        /// <param name="mainType">The inheriting type (like IRoom)</param>
        /// <returns>all the stuff and things</returns>
        public IEnumerable<T> GetAll<T>(Type mainType)
        {
            return globalCache.Where(keyValuePair => keyValuePair.Value.GetType().GetInterfaces()
                .Contains(typeof(T)) && keyValuePair.Value.GetType().GetInterfaces().Contains(mainType))
                .Select(kvp => (T)kvp.Value);
        }

        public T Get<T>(LiveCacheKey key) where T : IEntity
        {
            try
            {
                return (T)globalCache[key.KeyHash()];
            }
            catch
            {
                //TODO: Logging, why were we looking for something that didn't exist?
            }

            return default(T);
        }

        public T Get<T>(long id) where T : IEntity
        {
            try
            {
                var allPlayers = GetAll<T>();

                if (allPlayers.Any(p => ((IEntity)p).DataTemplate.ID.Equals(id)))
                    return allPlayers.First(p => ((IEntity)p).DataTemplate.ID.Equals(id));
            }
            catch
            {
                //TODO: Logging, why were we looking for something that didn't exist?
            }

            return default(T);
        }

        public T Get<T>(long id, Type mainType) where T : IEntity
        {
            try
            {
                var allTheStuff = GetAll<T>(mainType);

                if (allTheStuff.Any(p => ((IEntity)p).DataTemplate.ID.Equals(id)))
                    return allTheStuff.First(p => ((IEntity)p).DataTemplate.ID.Equals(id));
            }
            catch
            {
                //TODO: Logging, why were we looking for something that didn't exist?
            }

            return default(T);
        }

        public void Remove(LiveCacheKey key)
        {
            globalCache.Remove(key.KeyHash());
        }

        public bool Exists(LiveCacheKey key)
        {
            return globalCache.Get(key.KeyHash()) != null;
        }

    }

    public class LiveCacheKey
    {
        public Type ObjectType { get; set; }
        public String BirthMark { get; set; }

        public LiveCacheKey(Type objectType, string marker)
        {
            ObjectType = objectType;
            BirthMark = marker;
        }

        public string KeyHash()
        {
            return String.Format("{0}.{1}", ObjectType.Name, BirthMark.ToString());
        }
    }

    public static class Birthmarker
    {
        /// <summary>
        /// Gets birthmarks for live entities
        /// </summary>
        /// <returns>the birthmark string</returns>
        public static string GetBirthmark(IData obj)
        {
            return String.Format("{0}.{1}.{2}", obj.ID, DateTime.Now.ToBinary(), Guid.NewGuid().ToString().Replace("-", String.Empty));
        }
    }
}
