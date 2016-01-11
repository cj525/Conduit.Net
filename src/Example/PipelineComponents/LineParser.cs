using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Example.PipelineAdjuncts;
using Pipes.Example.PipelineMessages;
using Pipes.Example.PipelineMeta;
using Pipes.Interfaces;
using Pipes.Types;

namespace Pipes.Example.PipelineComponents
{
    class LineParser : PipelineComponent
    {
        protected override void Describe(IPipelineComponentBuilder<OperationContext> thisComponent)
        {
            thisComponent
                .Receives<StreamLine>()
                .WhichCalls(Parse);

            thisComponent
                .Emits<FieldedData>();
        }

        private void Parse(IPipelineMessage<StreamLine, OperationContext> message)
        {
            // Localize
            var data = message.Data;
            var context = message.Context;

            // Get state
            var config = context.Retrieve<LineParserConfig>();

            // Do parsing
            foreach (var entry in data.Entries)
            {
                var parts = entry.Split(config.Delimiters, StringSplitOptions.None);

                // If we don't have a header, simply store that in state
                if (!config.HaveHeader)
                {
                    config.Header = parts;
                }

                // Otherwise drop the
                else
                {
                    var fields = new FieldedData(config.Header, parts);
                    Emit(message, fields);
                }
            }
        }
    }
}
