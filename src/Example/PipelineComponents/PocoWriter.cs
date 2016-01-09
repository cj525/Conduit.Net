using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Example.Implementation;
using Pipes.Example.PipelineMessages;
using Pipes.Example.PipelineMeta;
using Pipes.Example.Schema.Abstraction;
using Pipes.Interfaces;
using Pipes.Types;

namespace Pipes.Example.PipelineComponents
{
    class PocoWriter<T> : PipelineComponent where T : PocoWithId
    {
        protected override void Describe(IPipelineComponentBuilder<OperationContext> thisComponent)
        {
            thisComponent
                .Receives<T>()
                .WhichCallsAsync(Commit);

            thisComponent
                .Emits<PocoCommited<T>>();
        }

        private async Task Commit(IPipelineMessage<T, OperationContext> message)
        {
            // Localize
            var context = message.Context;
            var data = message.Data;

            // Store the data
            await PretendDb.StoreAsync(data.Id, data);

            // Emit indicator that the poco is commited
            Emit(message, new PocoCommited<T>(data), new PocoWithIdMeta { StockId = data.Id, Type = typeof(T) } );
        }
    }
}
