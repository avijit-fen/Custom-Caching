using Fin.CacheManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LRUCacheApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var cache = LRUCache<int, string>.GetInstance(5);
            cache.OnEviction += Eviction_Event;

            List<int> list = Enumerable.Range(1, 500).ToList();
            var random = new Random();
            // checking with Mutithreading if cache works fine
            Parallel.ForEach(list, new ParallelOptions() { MaxDegreeOfParallelism = 10 }, i =>
            {
                int r = random.Next(list.Count);
                string item = r + "_" + "CacheItem";
                
                cache.add(r, item , new CachePolicy() { CachePolicyType = CachePolicyType.SlidingExpiration , SlidingExpiration = TimeSpan.FromSeconds(60) });

                int r1 = random.Next(list.Count);
                var itemget = cache.get(r1);
                Console.WriteLine(itemget + " at: " + r1);

            });

            var c = cache;
            
            Console.ReadLine();

        }

        private static void Eviction_Event(object sender, EvictionArgs<int> e)
        {
            Console.WriteLine("Evicted Key:" + e.Key);
            Console.WriteLine("Evicted Reason:" + e.EvictionReason);
        }

        
    }
}
