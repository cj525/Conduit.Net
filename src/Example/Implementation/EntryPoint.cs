using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pipes.Example.PipelineAdjuncts;
using Pipes.Example.PipelineComponents;
using Pipes.Example.PipelineMessages;
using Pipes.Example.Pipelines;
using Pipes.Example.Schema;
using Pipes.Interfaces;

namespace Pipes.Example.Implementation
{
    class EntryPoint
    {
        const int EntryCount = 100000;
        static int _entriesCommited;


        static void Main()
        {
            RunApproaches().Wait();

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press any key to exit");
                Console.ReadKey(true);
            }
        }

        static async Task RunApproaches()
        {
            await PipelineApproach();
            await NonPipelineApproach();
        }

        static async Task NonPipelineApproach()
        {
            Console.WriteLine($"Running non-pipeline for {EntryCount} stock ticks");
            var stamp = DateTime.UtcNow;

            var stream = new StockTickGenerator(10);

            using (TextReader reader = new StreamReader(stream))
            {
                // Locals
                var delimiter = new [] {","};
                var tickCache = new ReflectionCache<StockTick>();
                var line = default(string);
                _entriesCommited = 0;

                // Line Parser
                var headerData = await reader.ReadLineAsync();
                var header = headerData.Split(delimiter, StringSplitOptions.None);

                // Stream line reader
                while ((line = await reader.ReadLineAsync()) != null )
                {
                    // Line Parser
                    var parts = line.Split(delimiter, StringSplitOptions.None);
                    var fields = new FieldedData(header, parts);

                    // Poco Emitter
                    var tick = tickCache.Inflate(header, field => fields[field]);

                    // Poco Writer
                    await PretendDb.StoreAsync(tick.Id, tick);
                    EntryCommited();

                    // Stock Statistic Updater
                    var stat = await PretendDb.RetrieveAsync<StockStatistic>(new StockStatistic {Symbol = tick.Symbol}.Id);
                    StockStatisticUpdater.UpdateStatForStock(stat, tick);
                    await PretendDb.StoreAsync(stat.Id, stat);
                }
            }

            var time = DateTime.UtcNow - stamp;
            Console.WriteLine($"Ran for {time.TotalMilliseconds} milliseconds");
        }

        static async Task PipelineApproach()
        {
            _entriesCommited = 0;
            Console.WriteLine($"Running pipeline for {EntryCount} stock ticks");
            var stamp = DateTime.UtcNow;

            var pipeline = new StockStreamPipeline();
            pipeline.CreateMessageTap<PocoCommited>().WhichTriggers(EntryCommited);
            pipeline.Initialize();

            await pipeline.ProcessStream("Stock Stream Test", new StockTickGenerator(EntryCount));

            var time = DateTime.UtcNow - stamp;
            Console.WriteLine($"Ran for {time.TotalMilliseconds} milliseconds");
        }

        private static void EntryCommited()
        {
            var count = Interlocked.Increment(ref _entriesCommited);
            if ( count % 10000 == 0)
            {
                Console.WriteLine($"{count} stock ticks commited");
            }
        }
    }
}
