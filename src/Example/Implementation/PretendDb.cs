using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pipes.Example.Implementation
{
    class PretendDb
    {
        public static int ReadDelayMs = 1;
        public static int WriteDelayMs = 1;
        public static int ScanDelayMs = 2;
        public static bool IsGlitchy = true;

        private static readonly Dictionary<Type,Dictionary<long, object>> Storage = new Dictionary<Type, Dictionary<long, object>>();
        private static readonly object Mutex = new { };
        private static readonly Random _random = new Random();


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
            //Thread.Sleep(15);

            if (IsGlitchy && _random.NextDouble() > 0.95d)
                throw new RandomException();
        }

        public static async Task<T> RetrieveAsync<T>(long id, Func<T> defaultValue) where T: class, new()
        {
            var result = default(T);
            var type = typeof(T);
            lock (Mutex)
            {
                if (!Storage.ContainsKey(type))
                {
                    return defaultValue();
                }
            }
            var bucket = Storage[type];
            lock (bucket)
            {
                if (bucket.ContainsKey(id))
                {
                    result = (T)bucket[id];
                }
                else
                {
                    result = defaultValue();
                }
            }

            await Task.Delay(ReadDelayMs);
            //Thread.Sleep(15);

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

        public static int Count<T>()
        {
            var type = typeof(T);
            lock (Mutex)
            {
                if (!Storage.ContainsKey(type))
                {
                    return 0;
                }
            }
            var bucket = Storage[type];

            return bucket.Count;
        }
        public static void Clear()
        {
            lock (Mutex)
            {
                Storage.Clear();
            }
        }



    }

    internal class RandomException : Exception
    {
    }
}
