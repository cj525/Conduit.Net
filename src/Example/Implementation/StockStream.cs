using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipes.Example.Implementation
{
    class StockStream : IEnumerable<string[]>
    {
        private readonly Random _rand;
        private readonly int _entriesTotal;
        private bool _headerSent;
        private int _entriesSent;

        public StockStream(int entryCount)
        {
            _entriesTotal = entryCount;
            _rand = new Random();
        }

        private string[] CreateEntry()
        {
            var results = new List<string>();
            var count = _rand.Next(1,5);
            for (int index = 0; index < count && _entriesSent < _entriesTotal; index++)
            {
                if (!_headerSent)
                {
                    results.Add("Timestamp,Symbol,Value");
                    _headerSent = true;
                    break;
                }
                    var change = Math.Round(_rand.NextDouble(), 2) * 10d;
                var symbol = "XYZ" + (_entriesSent % 10);
                var data = String.Join(",", _entriesSent, symbol, change);
                results.Add( data );
                _entriesSent++;
            }

            return results.ToArray();
        }


        public IEnumerator<string[]> GetEnumerator()
        {
            while (_entriesSent < _entriesTotal)
            {
                yield return CreateEntry();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
