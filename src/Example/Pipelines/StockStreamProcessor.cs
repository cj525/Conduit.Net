using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Example.PipelineComponents;
using Pipes.Example.PipelineContexts;
using Pipes.Example.Schema;
using Pipes.Interfaces;
using Pipes.Types;

namespace Pipes.Example.Pipelines
{
    class StockStreamProcessor : Pipeline
    {
        private Func<Stream, IOperationContext, Task> _entryPoint;

        protected override void Describe(IPipelineBuilder<IOperationContext> thisPipeline)
        {
            var reader = Component;
            var parser = Component;
            var emitter = Component;
            var writer = Component;
            var updater = Component;

            thisPipeline
                .IsInvokedAsyncBy(ref _entryPoint);

            thisPipeline
                .Constructs(() => new StreamLineReader())
                .Into(ref reader);

            thisPipeline
                .Constructs(()=>new LineParser())
                .Into(ref parser);

            thisPipeline
                .Constructs(()=>new PocoEmitter<StockTick>())
                .Into(ref emitter);

            thisPipeline
                .Constructs(()=>new PocoWriter<StockTick>())
                .Into(ref writer);

            thisPipeline
                .Constructs(()=>new StockStatisticUpdater())
                .Into(ref updater);

        }

        public async Task ProcessStream(string name, Stream stream)
        {
            var context = new PocosFromStreamContext { Name = name };
            await _entryPoint(stream, context);
        }
    }
}
