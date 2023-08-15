using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fin.CacheManager.Interface
{
    public interface ICache<K,V> where K : IComparable
    {
        V get(K key);
        void add(K key, V val, ICachePolicy cachePolicy = null);
        K remove(K key);

    }
}
