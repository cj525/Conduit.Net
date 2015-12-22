using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Example.PipelineAdjuncts;
using Pipes.Example.PipelineMessages;
using Pipes.Example.Schema;
using Pipes.Interfaces;

namespace Pipes.Example.PipelineComponents
{
    class PocoEmitter<T> : PipelineComponent where T : class, new()
    {
        protected override void Describe(IPipelineComponentBuilder<IOperationContext> thisComponent)
        {
            thisComponent
                .Receives<FieldedData>()
                .WhichCallsAsync(Inflate);

            thisComponent
                .Emits<T>();

        }

        private async Task Inflate(IPipelineMessage<FieldedData, IOperationContext> message)
        {
            // Localize
            var context = message.Context;
            var data = message.Data;

            // Get database connection (would be from connection pool factory)
            var cache = context.Ensure(() => new ReflectionCache<T>());

            // Create the poco
            var poco = cache.Inflate(data.Keys, field => data[field]);

            // Drop poco in pipeline
            await EmitAsync(message, poco);
        }
    }
}
