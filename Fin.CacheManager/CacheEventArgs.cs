using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fin.CacheManager
{
    /// <summary>
    /// This class used for eviction event args
    /// </summary>
    /// <typeparam name="K"></typeparam>
    public class EvictionArgs<K> : EventArgs
    {
        public K Key { get; set; }
        public string EvictionReason { get; set; }
    }
}
