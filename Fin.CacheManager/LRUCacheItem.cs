using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fin.CacheManager
{
    internal class LRUCacheItem<K, V>
    {
        public LRUCacheItem(K k, V v, DateTime? expired)
        {
            key = k;
            value = v;
            Expired = expired;
        }
        public K key;
        public V value;
        public DateTime? Expired;
    }
}
