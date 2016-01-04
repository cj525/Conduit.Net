using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pipes.Types;

namespace Pipes.Example.PipelineContexts
{
    class StockStreamContext : OperationContext
    {
        public string Name { get; set; }
    }
}
