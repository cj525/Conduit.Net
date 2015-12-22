using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Example.PipelineAdjuncts;
using Pipes.Example.PipelineMessages;
using Pipes.Example.Schema;
using Pipes.Interfaces;
using StreamMessage = Pipes.Interfaces.IPipelineMessage<System.IO.Stream, Pipes.Interfaces.IOperationContext>;

namespace Pipes.Example.PipelineComponents
{
    class StreamLineReader : PipelineComponent
    {
        

        private static readonly string[] DelimitersToTry = { ",", "\t"};

        protected override void Describe(IPipelineComponentBuilder<IOperationContext> thisComponent)
        {
            thisComponent
                .Receives<Stream>()
                .WhichCallsAsync(ReadStreamLinesAsync);

            thisComponent
                .Emits<StreamLine>()
                .Emits<StreamEnded>();
        }

        private async Task ReadStreamLinesAsync(StreamMessage message)
        {
            var context = message.Context;
            var stream = message.Data;

            using (var textReader = new StreamReader(stream))
            {
                var header = await textReader.ReadLineAsync();

                foreach (var delimiter in DelimitersToTry)
                {
                    if (header.Contains(delimiter))
                    {
                        context.Store(new ParserConfig {Delimiters = new[] {delimiter}});
                        break;
                    }
                }

                if( !context.ContainsAdjunctOfType<ParserConfig>())
                    context.Fault("Can't determine delimiter");
                else
                {
                    await Loop(context, new StreamLoop(this, message, textReader));

                    await EmitAsync(message, new StreamEnded());
                }
            }
        }

        class StreamLoop : ConcurrentLoopState<StreamLine>
        {
            private readonly TextReader _reader;
            private string _line;

            internal StreamLoop(StreamLineReader component, StreamMessage message, TextReader reader) : base(component, message)
            {
                _reader = reader;
            }

            protected override StreamLine Current => new StreamLine {Data = _line};

            protected override async Task<bool> AdvanceAsync()
            {
                _line = await _reader.ReadLineAsync();

                return _line != null;
            }
        }
    }
}
