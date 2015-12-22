using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pipes.Example.Schema.Abstraction;

namespace Pipes.Example.PipelineMessages
{
    class PocoCommited
    {
        public Type Type { get; protected set; }
        public long Id { get; protected set; }
    }

    class PocoCommited<T> : PocoCommited where T: PocoWithId
    {
        public PocoCommited( T data )
        {
            Type = typeof (T);
            Id = data.Id;
        }
    }
}
