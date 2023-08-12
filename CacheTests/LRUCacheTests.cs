using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fin.CacheManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Fin.CacheManager.Tests
{
    /// <summary>
    /// Main Unit test cases against cache
    /// </summary>
    [TestClass()]
    public class LRUCacheTests
    {
        [TestMethod()]
        public void CacheTest_String()
        {
            var cache = LRUCache<int, string>.GetInstance(5);

            cache.add(1, "A");
            cache.add(2, "B");
            cache.add(3, "C");
            cache.add(4, "D");  
            cache.add(5, "E");
            cache.add(6, "F");

            cache.OnEviction += Eviction_Event;
        }

        private void Eviction_Event(object sender, EvictionArgs<int> e)
        {
            Assert.AreEqual(1, e.Key);
        }

        [TestMethod()]
        public void CacheTest_Get()
        {
            var cache = LRUCache<int, string>.GetInstance(5);

            cache.add(1, "A");
            cache.add(2, "B");
            cache.add(3, "C");
            cache.add(4, "D");
            cache.add(5, "E");
            
            var k = cache.get(1);
            Assert.AreEqual("A", k);

            cache.add(6, "k");

            cache.OnEviction += Eviction_Event1;

        }

        private void Eviction_Event1(object sender, EvictionArgs<int> e)
        {
            Assert.AreEqual(2, e.Key);
        }

        [TestMethod()]
        public void CacheTest_Same_Key_Add()
        {
            var cache = LRUCache<int, string>.GetInstance(5);

            cache.add(1, "A");
            cache.add(2, "B");

            cache.add(1, "C");
            var k = cache.get(1);

            Assert.AreEqual("C",k);

        }

        [TestMethod()]
        public void CacheTest_Key_Remove()
        {
            var cache = LRUCache<int, string>.GetInstance(5);

            cache.add(1, "A");
            cache.add(2, "B");
            cache.add(3, "C");


            var k = cache.remove(1);

            Assert.AreEqual(1, k);

        }

        [TestMethod()]
        public void CacheTest_Key_DoNot_Exist()
        {
            var cache = LRUCache<int, string>.GetInstance(5);

            var k = cache.get(100);

            Assert.AreEqual(null, k);

        }

        [TestMethod()]
        public void CacheTest_Sliding_Expiration()
        {
            var cache = LRUCache<int, string>.GetInstance(5);

            cache.add(1,"A",new CachePolicy() { CachePolicyType = CachePolicyType.SlidingExpiration , SlidingExpiration = TimeSpan.FromSeconds(120) });
            
            cache.OnEviction += Eviction_Event2;

            Thread.Sleep(1000);

            var r = cache.get(1);

        }

        private void Eviction_Event2(object sender, EvictionArgs<int> e)
        {
            Assert.IsNotNull(e.EvictionReason);
        }
    }
}