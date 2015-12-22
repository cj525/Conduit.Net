using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pipes.Example.Schema.Abstraction;

namespace Pipes.Example.Schema
{
    class StockStatistic : PocoWithId
    {
        public override long Id => Symbol.GetHashCode();

        public string Symbol { get; set; }

        public decimal? High { get; set; }

        public decimal? Low { get; set; }

        public decimal CurrentValue{ get; set; }
    }
}
