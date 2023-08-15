using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fin.CacheManager.POCO
{
    public class CachePolicy : ICachePolicy
    {
        public CachePolicy() { }

        public CachePolicyType CachePolicyType { get; set; }
        public TimeSpan? SlidingExpiration { get; set; }
        public DateTime? SlidingExpirationUtc { get; set; }
    }

    public enum CachePolicyType
    {
        None = 0,
        SlidingExpiration = 1,
        AbsoluteExpiration = 2
    }
}
