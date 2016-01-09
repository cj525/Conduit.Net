using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipes.Example.PipelineAdjuncts
{
    class LineParserConfig
    {
        public string[] Delimiters { get; set; }

        public bool HaveHeader => Header != null;

        public string[] Header { get; set; }
    }
}
