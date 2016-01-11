using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Example.Implementation;
using Pipes.Example.PipelineAdjuncts;
using Pipes.Example.PipelineMessages;
using Pipes.Example.Schema;
using Pipes.Interfaces;
using Pipes.Types;
using StreamMessage = Pipes.Interfaces.IPipelineMessage<Pipes.Example.Implementation.StockStream, Pipes.Types.OperationContext>;

namespace Pipes.Example.PipelineComponents
{
    class StockStreamReader : PipelineComponent
    {

        protected override void Describe(IPipelineComponentBuilder<OperationContext> thisComponent)
        {
            thisComponent
                .Receives<StockStream>()
                .WhichCallsAsync(ReadStreamLinesAsync);

            thisComponent
                .Emits<StreamLine>()
                .Emits<StreamEnded>();
        }

        private async Task ReadStreamLinesAsync(StreamMessage message)
        {
            var context = message.Context;
            var stream = message.Data;

            if (!context.ContainsAdjunctOfType<LineParserConfig>())
                await context.Fault("Can't determine line parser config!");
            else
            {
                var lineNumber = 0;
                foreach (var entry in stream)
                {
                    lineNumber++;
                    Emit(message, new StreamLine { Entries = entry, LineNumber = lineNumber });
                }
                //using (var enumerator = stream.GetEnumerator())
                //{

                //    await Loop(context, new StreamLoop(this, message, enumerator));

                //    Emit(message, new StreamEnded());
                //}
            }
        }

        class StreamLoop : LoopState<StreamLine>
        {
            private readonly IEnumerator<string[]> _reader;
            private string[] _line;
            private int _lineNumber;
            internal StreamLoop(StockStreamReader component, StreamMessage message, IEnumerator<string[]> reader) : base(component, message)
            {
                _reader = reader;
            }

            protected override StreamLine Current => new StreamLine {Entries = _line, LineNumber = _lineNumber};

            protected override Task<bool> AdvanceAsync()
            {
                var ready = _reader.MoveNext();
                if (ready)
                {
                    _line = _reader.Current;
                    Meta = ++_lineNumber;
                }

                return Task.FromResult(ready);
            }
        }
    }
}
