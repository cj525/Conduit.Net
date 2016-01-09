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
        static int EntryCount = 10000;
        static int _entriesCommited;


        static void Main()
        {
            new EntryPoint().RunApproaches().Wait();

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press any key to exit");
                Console.ReadKey(true);
            }
        }

        async Task RunApproaches()
        {
            await PipelineApproach();
            await NonPipelineApproach();
        }

        async Task PipelineApproach()
        {
            _entriesCommited = 0;
            Console.WriteLine($"Running pipeline for {EntryCount} stock ticks");
            var stamp = DateTime.UtcNow;

            var pipeline = new StockStreamPipeline();
            pipeline.CreateMessageTap<PocoCommited<StockTick>>().WhichTriggers(EntryCommited);
            pipeline.Initialize();

            await pipeline.ProcessStream("Stock Stream Test", new StockStream(EntryCount));

            //Console.WriteLine("Completing...");

            Console.WriteLine($"{PretendDb.Count<StockTick>()} stock ticks");

            var time = DateTime.UtcNow - stamp;
            Console.WriteLine($"Ran for {time.TotalMilliseconds} milliseconds");
        }

        static void EntryCommited()
        {
            var count = Interlocked.Increment(ref _entriesCommited);
            if ( count % 1000 == 0)
            {
                Console.WriteLine($"{count} stock ticks commited");
            }
        }


        async Task NonPipelineApproach()
        {
            EntryCount /= 100;
            Console.WriteLine($"Running non-pipeline for {EntryCount} stock ticks");
            var stamp = DateTime.UtcNow;

            var stream = new StockStream(EntryCount);

            using (var enumerator = stream.GetEnumerator())
            {
                // Locals
                var delimiter = new[] { "," };
                var tickCache = new ReflectionCache<StockTick>();
                _entriesCommited = 0;

                // Get header
                enumerator.MoveNext();
                var headers = enumerator.Current;
                var header = headers[0].Split(delimiter, StringSplitOptions.None);

                // Stream line reader
                while (enumerator.MoveNext())
                {
                    var lines = enumerator.Current;
                    foreach (var line in lines)
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
                        var prototype = new StockStatistic { Symbol = tick.Symbol };
                        var stat = await PretendDb.RetrieveAsync(prototype.Id, () => prototype);
                        StockStatisticUpdater.UpdateStatForStock(stat, tick);
                        await PretendDb.StoreAsync(stat.Id, stat);
                    }
                }
            }

            var time = DateTime.UtcNow - stamp;
            Console.WriteLine($"Ran for {time.TotalMilliseconds} milliseconds");
        }

    }
}
