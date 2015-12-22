using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Example.PipelineAdjuncts;
using Pipes.Example.PipelineMessages;
using Pipes.Interfaces;

namespace Pipes.Example.PipelineComponents
{
    class LineParser : PipelineComponent
    {
        protected override void Describe(IPipelineComponentBuilder<IOperationContext> thisComponent)
        {
            thisComponent
                .Receives<string>()
                .WhichCallsAsync(Parse);

            thisComponent
                .Emits<FieldedData>();
        }

        private async Task Parse(IPipelineMessage<string, IOperationContext> message)
        {
            // Localize
            var data = message.Data;
            var context = message.Context;

            // Get state
            var config = context.Retrieve<ParserConfig>();

            // Do parsing
            var parts = data.Split(config.Delimiters, StringSplitOptions.None);

            // If we don't have a header, simply store that in state
            if (!config.HaveHeader)
            {
                config.Header = parts;
            }

            // Otherwise drop the
            else
            {
                var fields = new FieldedData(config.Header,parts);
                await EmitAsync(message, fields);
            }
        }
    }
}
