using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Pipes.Example.Implementation
{
    class PretendDb
    {
        public static int WriteDelayMs = 15;
        public static int ReadDelayMs = 10;
        public static int ScanDelayMs = 75;

        private static readonly Dictionary<Type,Dictionary<long, object>> Storage = new Dictionary<Type, Dictionary<long, object>>();
        private static readonly object Mutex = new { };


        public static async Task StoreAsync<T>(long id, T data)
        {
            var type = typeof(T);
            lock (Mutex)
            {
                if (!Storage.ContainsKey(type))
                {
                    Storage.Add(type, new Dictionary<long, object>());
                }
            }
            var bucket = Storage[type];
            lock (bucket)
            {
                if (bucket.ContainsKey(id))
                {
                    bucket[id] = data;
                }
                else
                {
                    bucket.Add(id, data);
                }
            }

            await Task.Delay(WriteDelayMs);
        }

        public static async Task<T> RetrieveAsync<T>(long id) where T: class, new()
        {
            var result = default(T);
            var type = typeof(T);
            lock (Mutex)
            {
                if (!Storage.ContainsKey(type))
                {
                    return new T();
                }
            }
            var bucket = Storage[type];
            lock (bucket)
            {
                if (bucket.ContainsKey(id))
                {
                    result = (T)bucket[id];
                }
            }

            await Task.Delay(ReadDelayMs);

            return result;
        }

        public static async Task<IEnumerable<T>> ScanPessimisticAsync<T>(long startInclusive, long stopExclusive)
        {
            var results = new List<T>();
            var type = typeof (T);
            lock (Mutex)
            {
                if (!Storage.ContainsKey(type))
                {
                    return Enumerable.Empty<T>();
                }
            }
            var bucket = Storage[type];
            lock ( bucket )
            { 
                for (long index = startInclusive; index < stopExclusive; index++)
                {
                    if (bucket.ContainsKey(index))
                        results.Add((T)bucket[index]);
                }
            }

            await Task.Delay(ScanDelayMs);

            return results;
        }

        public static async Task<IEnumerable<T>> ScanOptimisticAsync<T>(long startInclusive, long stopExclusive)
        {
            var results = new List<T>();

            var type = typeof(T);
            lock (Mutex)
            {
                if (!Storage.ContainsKey(type))
                {
                    return Enumerable.Empty<T>();
                }
            }
            var bucket = Storage[type];

            for (long index = startInclusive; index < stopExclusive; index++)
            {
                if (bucket.ContainsKey(index))
                {
                    lock (bucket)
                    {
                        if (bucket.ContainsKey(index))
                        {
                            results.Add((T)bucket[index]);
                        }
                    }
                }
            }

            await Task.Delay(ScanDelayMs);
            return results;
        }

        public static void Clear()
        {
            lock (Mutex)
            {
                Storage.Clear();
            }
        }



    }
}
