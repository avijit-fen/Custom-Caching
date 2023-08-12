using Fin.Logger;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Fin.CacheManager
{
        /// <summary>
        /// Main singleton Cache class 
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        public class LRUCache<K, V>
        {
            private int capacity;
            //concurrent dictionary to store cache object
            private ConcurrentDictionary<K, LinkedListNode<LRUCacheItem<K, V>>> cacheMap = new ConcurrentDictionary<K, LinkedListNode<LRUCacheItem<K, V>>>();
            // Linked list used to track LRU algorithm
            private LinkedList<LRUCacheItem<K, V>> lruList = new LinkedList<LRUCacheItem<K, V>>();
            private static readonly object Instancelock = new object();
            private static LRUCache<K,V> Instance = null;
            private ILogger _logger;
            //subscribe to this event to know which key got evicted
            public EventHandler<EvictionArgs<K>> OnEviction;
            
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="capacity"></param>
            LRUCache(int capacity)
            {
                this.capacity = capacity;
                var cts = new CancellationToken();

                        RecurringTask(()=>
                        {
                            DeleteIfExpired();

                        }, 2, cts);
            }
            /// <summary>
            /// Overloaded Constuctor with Logger , use dependency injection to implement your logger
            /// </summary>
            /// <param name="capacity"></param>
            /// <param name="logger"></param>
            LRUCache(int capacity, ILogger logger)
            {
                this.capacity = capacity;
                this._logger = logger;
                
                var cts = new CancellationToken();

                        RecurringTask(()=>
                        {
                            DeleteIfExpired();

                        }, 2, cts);

            }
            /// <summary>
            /// One of Single tone instance creation
            /// </summary>
            /// <param name="capacity"></param>
            /// <returns></returns>
            public static LRUCache<K,V> GetInstance(int capacity)
            {
                lock (Instancelock)
                { 
                    if (Instance == null)
                    {
                        Instance = new LRUCache<K,V>(capacity);
                        

                    }
                }

                return Instance;
            }
        /// <summary>
        /// Scheduled task to check expired , it will start once
        /// </summary>
        /// <param name="action"></param>
        /// <param name="seconds"></param>
        /// <param name="token"></param>
        static void RecurringTask(Action action, int seconds, CancellationToken token)
        {
            if (action == null)
                return;
            Task.Run(async () => {
                while (!token.IsCancellationRequested)
                {
                    action();
                    await Task.Delay(TimeSpan.FromSeconds(seconds), token);
                }
            }, token);
        }
        /// <summary>
        /// Method to check expired cache and remove
        /// </summary>
        private void DeleteIfExpired()
        {
            _logger?.Info("Checking delete if expired:" + Thread.CurrentThread.ManagedThreadId);
            if(cacheMap == null) { return; }

                foreach(var item in cacheMap.Keys) 
                {
                    var cacheObj = cacheMap[item];
                    var cacheitem = cacheObj.Value;
                    if(cacheitem  != null && cacheitem.Expired != null) 
                    {
                        _logger?.Info(cacheitem.Expired + " " + DateTime.Now);
                        if(cacheitem.Expired >= DateTime.Now) {
                        OnEviction?.Invoke(this, new EvictionArgs<K> { Key = item, EvictionReason = "Expired time Exceeded" });
                        remove(item); 
                        }
                    }
                }
        }
            
            /// <summary>
            /// Overload Singletone Instance Creation , you can inject your logger
            /// </summary>
            /// <param name="capacity"></param>
            /// <param name="logger"></param>
            /// <returns></returns>
            public static LRUCache<K, V> GetInstance(int capacity,ILogger logger)
            {
                lock (Instancelock)
                {
                    if (Instance == null)
                    {
                        Instance = new LRUCache<K, V>(capacity,logger);
                    }
                }
                return Instance;
            }

        /// <summary>
        /// Get Item from Cache
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
            public V get(K key)
            {
                LinkedListNode<LRUCacheItem<K, V>> node;
                if (cacheMap.TryGetValue(key, out node))
                {
                    V value = node.Value.value;
                    lruList.Remove(node);
                    lruList.AddLast(node);
                    _logger?.Info("Cache Hit:" + key);
                    return value;
                }
                else
                {
                    _logger?.Info("Cache Miss:" + key);
                }
                return default(V);
            }
            
            /// <summary>
            /// Add Item in Cache. If you add same Key item then it would override the prev value
            /// </summary>
            /// <param name="key"></param>
            /// <param name="val"></param>
            [MethodImpl(MethodImplOptions.Synchronized)]
            public void add(K key, V val)
            {
                if (cacheMap.TryGetValue(key, out var existingNode))
                {
                    lruList.Remove(existingNode);
                }
                else if (cacheMap.Count > capacity)
                {
                    RemoveFirst();
                }

                LRUCacheItem<K, V> cacheItem = new LRUCacheItem<K, V>(key, val,null);
                LinkedListNode<LRUCacheItem<K, V>> node = new LinkedListNode<LRUCacheItem<K, V>>(cacheItem);
                lruList.AddLast(node);
                cacheMap[key] = node;
            }

        /// <summary>
        /// Add Item in Cache. If you add same Key item then it would override the prev value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void add(K key, V val , CachePolicy cachePolicy)
        {
            if (cacheMap.TryGetValue(key, out var existingNode))
            {
                lruList.Remove(existingNode);
            }
            else if (cacheMap.Count > capacity)
            {
                RemoveFirst();
            }

            DateTime? expired;
            switch (cachePolicy.CachePolicyType)
            {
                case CachePolicyType.None:
                    expired = null;
                    break;
                case CachePolicyType.SlidingExpiration:
                    expired = DateTime.Now.Add(cachePolicy.SlidingExpiration.Value);
                    break;
                case CachePolicyType.AbsoluteExpiration:
                    expired = cachePolicy.SlidingExpirationUtc;
                    break;
                default:
                    expired = null; break;
;            }

            LRUCacheItem<K, V> cacheItem = new LRUCacheItem<K, V>(key, val, expired);
            LinkedListNode<LRUCacheItem<K, V>> node = new LinkedListNode<LRUCacheItem<K, V>>(cacheItem);
            lruList.AddLast(node);
            cacheMap[key] = node;
        }
        /// <summary>
        /// Remove Key from Cache
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public K remove(K key)
            {
                if (cacheMap.TryGetValue(key, out var existingNode))
                {
                    lruList.Remove(existingNode);
                    cacheMap.TryRemove(key, out var linkedListNode);
                    return linkedListNode.Value.key;
                }
                _logger?.Info("Key do not exist:" + key);
                return default(K);
            }

            /// <summary>
            /// Remove Item from cache and fire delegate
            /// </summary>
            private void RemoveFirst()
            {
                LinkedListNode<LRUCacheItem<K, V>> node = lruList.First;
                LinkedListNode<LRUCacheItem<K,V >> item = null; 
                lruList.RemoveFirst();

                if (cacheMap.TryRemove(node.Value.key, out item))
                {
                    Console.WriteLine(cacheMap.Count);
                    OnEviction?.Invoke(this, new EvictionArgs<K> { Key =  node.Value.key , EvictionReason = "Item count Exceeded"});
                }
            }

            
        }
 
        

        
    
}
