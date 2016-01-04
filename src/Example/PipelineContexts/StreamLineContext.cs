using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pipes.Types;

namespace Pipes.Example.PipelineContexts
{
    class LineNumberContext : OperationContext
    {
        public int Line { get; set; }
    }
}
