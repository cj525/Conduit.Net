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

            var stockTick = await PretendDb.RetrieveAsync<StockTick>(data.Id);

            if (stockTick.Value == 0)
            {
                await context.Cancel("Change was zero");
            }
            else
            {
                var stat = await PretendDb.RetrieveAsync<StockStatistic>(new StockStatistic { Symbol = stockTick.Symbol }.Id);

                UpdateStatForStock(stat, stockTick);

                await PretendDb.StoreAsync(stat.Id, stat);

            }
        }

        public static void UpdateStatForStock(StockStatistic stat, StockTick stockTick)
        {
            var newValue = stat.CurrentValue + stockTick.Value;

            if (!stat.High.HasValue || newValue > stat.High)
                stat.High = newValue;

            if (!stat.Low.HasValue || newValue < stat.Low)
                stat.Low = newValue;
        }
    }
}
