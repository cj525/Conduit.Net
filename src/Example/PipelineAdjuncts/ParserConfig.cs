using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipes.Example.PipelineAdjuncts
{
    class ParserConfig
    {
        public string[] Delimiters { get; set; }
        public bool HaveHeader { get; set; }
        public string[] Header { get; set; }
    }
}
