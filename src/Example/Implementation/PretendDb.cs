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
        public static int ScanDelayMs = 10;

        private readonly object _mutex = new {};
        private readonly Dictionary<long, object> _data;

        public PretendDb()
        {
            _data = new Dictionary<long, object>();
        }


        public async Task StoreAsync<T>(long id, T data)
        {
            lock (_mutex)
            {
                if (_data.ContainsKey(id))
                {
                    _data[id] = data;
                }
                else
                {
                    _data.Add(id, data);
                }
            }

            await Task.Delay(WriteDelayMs);
        }

        public async Task<T> RetrieveAsync<T>(long id)
        {
            var result = default(T);
            lock (_mutex)
            {
                if (_data.ContainsKey(id))
                {
                    result = (T) _data[id];
                }
            }

            await Task.Delay(ReadDelayMs);

            return result;
        }

        public async Task<IEnumerable<T>> ScanPessimisticAsync<T>(long startInclusive, long stopExclusive)
        {
            var results = new List<T>();
            lock (_mutex)
            {
                for (long index = startInclusive; index < stopExclusive; index++)
                {
                    if (_data.ContainsKey(index))
                        results.Add((T)_data[index]);
                }
            }

            await Task.Delay(ScanDelayMs);

            return results;
        }

        public async Task<IEnumerable<T>> ScanOptimisticAsync<T>(long startInclusive, long stopExclusive)
        {
            var results = new List<T>();

            for (long index = startInclusive; index < stopExclusive; index++)
            {
                if (_data.ContainsKey(index))
                {
                    lock (_mutex)
                    {
                        if (_data.ContainsKey(index))
                        {
                            results.Add((T)_data[index]);
                        }
                    }
                }
            }

            await Task.Delay(ScanDelayMs);
            return results;
        }





    }
}
