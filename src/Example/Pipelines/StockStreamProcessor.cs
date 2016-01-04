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
using Pipes.Example.PipelineMessages;
using Pipes.Example.Schema;
using Pipes.Example.Schema.Abstraction;
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
                .Constructs(()=>new PocoWriter<PocoWithId>())
                .Into(ref writer);

            thisPipeline
                .Constructs(()=>new StockStatisticUpdater())
                .Into(ref updater);

            reader
                // When each line is sent
                .SendsMessage<StreamLine>()
                // Create a subcontext for each line
                .WithSubcontext<StockStreamContext>(message => new LineNumberContext {Line = message.Data.LineNumber})
                .To(parser)
                // Ensure completion 
                .WithCompletion( _ => _
                    // And throttle to 500 concurrent messages
                    .WithMaximumConcurrency( 500 )
                    .OnCancellation(HandleCancellation)
                );

            emitter
                .SendsMessage<StockTick>()
                .WithSubcontext<PocoWithIdContext>(message => new PocoWithIdContext {Type = message.Data?.GetType(), StockId = message.Data?.Id ?? -1})
                .To(writer);
        }

        private void HandleCancellation(IPipelineMessage<StreamLine, IOperationContext> message)
        {
            // TODO: Could reach into data stack?  Would need to make a new message.
            // source -> [HasCompletion] -> Other -> [Cancellation]
            //                  ^ This message             ^ Message that was cancelled
            // Maybe devise test for this
            // For now, escalate the cancel to a fault
            message.Context.Fault( "Cancel escalated to fault!" );
        }

        public async Task ProcessStream(string name, Stream stream)
        {
            var context = new StockStreamContext { Name = name };
            await _entryPoint(stream, context);
        }
    }
}
