using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipes.Example.Schema.Abstraction
{
    abstract class PocoWithId
    {
        public virtual long Id { get; }
    }
}
