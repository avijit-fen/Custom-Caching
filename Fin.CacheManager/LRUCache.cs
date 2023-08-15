using Fin.CacheManager.Events;
using Fin.CacheManager.Interface;
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
        /// Main singleton Cache class with cluster
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        public class LRUCache<K, V> : ICache<K, V> where K : IComparable
        {
            private int capacity;
            
            //concurrent dictionary to store cache object
            private ConcurrentDictionary<K, LRUCacheItem<K, V>> cacheMap = new ConcurrentDictionary<K, LRUCacheItem<K, V>>();
            // Linked list used to track LRU algorithm
            private LinkedList<K> lruList;
            private static readonly object Instancelock = new object();
            private static LRUCache<K,V> Instance = null;
            private ILogger _logger;
            //subscribe to this event to know which key got evicted
            public EventHandler<EvictionArgs<K>> OnEviction;
            private object cache_lock = new object(); // Used to ensure thread-safe operations
            

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="capacity"></param>
        LRUCache(int capacity)
            {
                this.capacity = capacity;
                this.lruList = new LinkedList<K>();
                // standard .net strategy implementation - every 20 sec remove expire items
            var cts = new CancellationToken();

                        RecurringTask(()=>
                        {
                            DeleteIfExpired();

                        }, Config.PollingInterval, cts);
            }
            /// <summary>
            /// Overloaded Constuctor with Logger , use dependency injection to implement your logger
            /// </summary>
            /// <param name="capacity"></param>
            /// <param name="logger"></param>
            LRUCache(int capacity, ILogger logger)
            {
                this.capacity = capacity;
                this.lruList = new LinkedList<K>();
                this._logger = logger;
                // standard .net strategy implementation - every 20 sec remove expire items
                var cts = new CancellationToken();

                        RecurringTask(()=>
                        {
                            DeleteIfExpired();

                        }, Config.PollingInterval, cts);

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
                    var cacheitem = cacheMap[item];
                    
                    if(cacheitem  != null && cacheitem.IsExpired) 
                    {
                        _logger?.Info(cacheitem.Expired + " " + DateTime.Now);
                        OnEviction?.Invoke(this, new EvictionArgs<K> { Key = item, EvictionReason = "Expired time Exceeded" });
                        remove(item); 
                        
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
        
            public V get(K key)
            {
  
                ThrowHelper.IfNullThrow(key);
                
                try
                {
                lock (cache_lock)
                {

                    LRUCacheItem<K, V> item;
                    if (cacheMap.TryGetValue(key, out item))
                    {
                        V value = item.value;
                        if (item.IsExpired)
                        {
                            remove(item.key);
                            OnEviction?.Invoke(this, new EvictionArgs<K> { Key = item.key, EvictionReason = "Item Expiry Exceeded" });
                            return default(V);
                        }
                        else
                        {
                            lruList.Remove(key);
                            LRUCacheItem<K, V> cacheItem = new LRUCacheItem<K, V>(item);
                            cacheMap[key] = cacheItem;
                            LinkedListNode<K> newnode = new LinkedListNode<K>(key);
                            lruList.AddLast(newnode);
                            _logger?.Info("Cache Hit:" + key);
                            CacheStats.HitCount++;
                            return value;
                        }
                    }
                    else
                    {
                        _logger?.Info("Cache Miss:" + key);
                        CacheStats.MissCount++;
                    }
                    return default(V);

                }
            }
                catch(Exception ex)
                {
                    throw new OperationCanceledException(ex.Message, ex);
                }

                
                
            }
            
            

        /// <summary>
        /// Add Item in Cache. If you add same Key item then it would override the prev value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        public void add(K key, V val , ICachePolicy cachePolicy = null)
        {
            try
            {
                lock (cache_lock)
                {
                    if (cacheMap.TryGetValue(key, out var existingNode))
                    {
                        lruList.Remove(existingNode.key);
                    }

                    LRUCacheItem<K, V> cacheItem = new LRUCacheItem<K, V>(key, val, cachePolicy);
                    LinkedListNode<K> node = new LinkedListNode<K>(key);
                    lruList.AddLast(node);
                    cacheMap[key] = cacheItem;

                    if (cacheMap.Count > capacity)
                    {
                        RemoveFirst();
                    }
                }
            }
            catch(Exception ex)
            {
                throw new OperationCanceledException(ex.Message, ex);
            }
            
            
        }
        /// <summary>
        /// Remove Key from Cache
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public K remove(K key)
        {
            try
            {
                lock (cache_lock)
                {
                    if (cacheMap.TryGetValue(key, out var existingNode))
                    {
                        lruList.Remove(existingNode.key);
                        cacheMap.TryRemove(key, out var ItemNode);
                        return ItemNode.key;
                    }
                    _logger?.Info("Key do not exist:" + key);
                    return default(K);
                }
            }
            catch (Exception ex)
            {
                throw new OperationCanceledException(ex.Message, ex);
            }
            
                
        }

        public void Clear()
        {
            try
            {
                lock (cache_lock)
                {
                    cacheMap.Clear();
                    lruList.Clear();
                }
            }
            catch(Exception ex)
            {
                throw new OperationCanceledException(ex.Message, ex);
            }

            
        }

        /// <summary>
        /// Remove Item from cache and fire delegate
        /// </summary>
        private void RemoveFirst()
        {
                LinkedListNode<K> node = lruList.First;
                LRUCacheItem<K, V> cacheItem  = null; 
                lruList.RemoveFirst();

                if (cacheMap.TryRemove(node.Value, out cacheItem))
                {
                    //Console.WriteLine(cacheMap.Count);
                    OnEviction?.Invoke(this, new EvictionArgs<K> { Key =  node.Value , EvictionReason = "Item count Exceeded"});
                }
        }
        

        



    }





}
