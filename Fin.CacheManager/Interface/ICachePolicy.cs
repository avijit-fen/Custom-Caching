using Fin.CacheManager.POCO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fin.CacheManager
{
    public interface ICachePolicy
    {
        CachePolicyType CachePolicyType { get; set; }
        TimeSpan? SlidingExpiration { get; set; }
        DateTime? SlidingExpirationUtc { get; set; }
    }
}
