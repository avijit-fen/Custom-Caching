using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Fin.CacheManager
{
    internal class LRUCacheItem<K, V>
    {
        public LRUCacheItem(K k, V v, CachePolicy cachePolicy)
        {
            
            key = k;
            value = v;
            CachePolicyItem = cachePolicy;
            InternalKey = Guid.NewGuid();
            setExpiry(cachePolicy);
            
        }

        public LRUCacheItem(LRUCacheItem<K,V> cacheItem)
        {
            key = cacheItem.key;
            value = cacheItem.value;
            CachePolicyItem = cacheItem.CachePolicyItem; 
            InternalKey = cacheItem.InternalKey;
            setExpiry(CachePolicyItem);
        }
        public K key;
        public V value;
        public DateTime? Expired { private set; get; }
        public DateTime? AbsoluteExpired { private set;get; }
        public Guid? InternalKey { private set; get; }

        public CachePolicy CachePolicyItem;

        private void setExpiry(CachePolicy cachePolicy)
        {
            if(cachePolicy == null) { return; }
            switch (cachePolicy.CachePolicyType) { 
            
                case CachePolicyType.None:
                    Expired = null;
                    AbsoluteExpired = null;
                break;
                case CachePolicyType.SlidingExpiration:
                    Expired = DateTime.UtcNow.Add(cachePolicy.SlidingExpiration.Value);
                    break;
                case CachePolicyType.AbsoluteExpiration:
                    AbsoluteExpired = cachePolicy.SlidingExpirationUtc;
                    break;
                default:
                    Expired = null;
                    AbsoluteExpired = null;
                break;
            }
        }

        public bool IsExpired { 
            get {

                if(CachePolicyItem == null) { return false; }

                if (CachePolicyItem.CachePolicyType == CachePolicyType.SlidingExpiration)
                {
                    if (!Expired.HasValue) { return true; }
                    else { if (Expired < DateTime.UtcNow) { return true; } }
                }
                else if(CachePolicyItem.CachePolicyType == CachePolicyType.AbsoluteExpiration)
                {
                    if (!AbsoluteExpired.HasValue) { return true; }
                    else { if (AbsoluteExpired < DateTime.UtcNow) { return true; } }
                }
                return false;
            } 
        }
    }


    public static class CacheStats
    {
        static CacheStats() { 
            
        }
        public static int HitCount { get; set; }
        public static int MissCount { get; set; }
    }

    
}
