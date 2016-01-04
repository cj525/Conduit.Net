using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pipes.Types;

namespace Pipes.Example.PipelineContexts
{
    class PocoWithIdContext : OperationContext
    {
        public long StockId { get; set; }
        public Type Type { get; set; }
    }
}
