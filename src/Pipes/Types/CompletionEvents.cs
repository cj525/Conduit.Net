using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pipes.Interfaces;

namespace Pipes.Types
{
    public class CompletionEvents
    {
        public readonly List<CompletionTask> CompletionTasks = new List<CompletionTask>();
        public readonly List<CancellationTask> CancellationTasks = new List<CancellationTask>();
        public readonly List<FaultTask> FaultTasks = new List<FaultTask>();
    }
}
