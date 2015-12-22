using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Example.Implementation;
using Pipes.Example.PipelineMessages;
using Pipes.Example.Schema;
using Pipes.Example.Schema.Abstraction;
using Pipes.Interfaces;

namespace Pipes.Example.PipelineComponents
{
    class StockStatisticUpdater : PipelineComponent
    {
        protected override void Describe(IPipelineComponentBuilder<IOperationContext> thisComponent)
        {
            thisComponent
                .Receives<PocoCommited<StockTick>>()
                .WhichCallsAsync(UpdateStock);
        }

        private async Task UpdateStock(IPipelineMessage<PocoCommited<StockTick>, IOperationContext> message)
        {
            var context = message.Context;
            var data = message.Data;

            var db = context.Retrieve<PretendDb>();
            var stockTick = await db.RetrieveAsync<StockTick>(data.Id);
            var stat = await db.RetrieveAsync<StockStatistic>(new StockStatistic {Symbol = stockTick.Symbol}.Id);

            var newValue = stat.CurrentValue + stockTick.Value;

            if (!stat.High.HasValue || newValue > stat.High)
                stat.High = newValue;

            if (!stat.Low.HasValue || newValue < stat.Low)
                stat.Low = newValue;

            await db.StoreAsync(stat.Id, stat);
        }
    }
}
