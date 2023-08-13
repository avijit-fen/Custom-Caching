using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fin.CacheManager
{
    internal static class Config
    {
        /// <summary>
        /// Polling time to remove expired Items
        /// </summary>
        public static int PollingInterval
        {
            get
            {
                int value = 20;
                int.TryParse(ConfigurationManager.AppSettings["PollingInterval"], out value);
                return value;
            }
        }
    }
}
