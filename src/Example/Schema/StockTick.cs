using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pipes.Example.Schema.Abstraction;

namespace Pipes.Example.Schema
{
    class StockTick : PocoWithId
    {
        public DateTime Timestamp { get; set; }

        public string Symbol { get; set; }

        public decimal Value { get; set; }
    }
}
