using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Example.Implementation;
using Pipes.Example.PipelineMessages;
using Pipes.Example.Schema.Abstraction;
using Pipes.Interfaces;

namespace Pipes.Example.PipelineComponents
{
    class PocoWriter<T> : PipelineComponent where T : PocoWithId
    {
        protected override void Describe(IPipelineComponentBuilder<IOperationContext> thisComponent)
        {
            thisComponent
                .Receives<T>()
                .WhichCallsAsync(Commit);

            thisComponent
                .Emits<PocoCommited>();
        }

        private async Task Commit(IPipelineMessage<T, IOperationContext> message)
        {
            // Localize
            var context = message.Context;
            var data = message.Data;

            // Get database connection (would be from connection pool factory)
            var db = context.Ensure(() => new PretendDb());

            // Store the data
            await db.StoreAsync(data.Id, data);

            // Emit indicator that the poco is commited
            await EmitAsync(message, new PocoCommited<T>(data));
        }
    }
}
