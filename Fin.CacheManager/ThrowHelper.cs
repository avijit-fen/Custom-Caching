using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fin.CacheManager
{
    internal static class ThrowHelper
    {
        public static void IfNullThrow(object key)
        {
            if(null == key) throw new ArgumentNullException("key");
        }
    }
}
