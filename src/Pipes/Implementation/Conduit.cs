using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Extensions;
using Pipes.Interfaces;
using Pipes.Stubs;
using Pipes.Types;

namespace Pipes.Implementation
{
    internal class Conduit<TContext> where TContext : OperationContext
    {
        private readonly Pipeline<TContext> _pipeline;
        private readonly Thunk[] _thunks;

        public Conduit(Pipeline<TContext> pipeline, IEnumerable<Thunk> thunks )
        {
            _pipeline = pipeline;
            _thunks = thunks.ToArray();
        }

        public async void Transport<TData>(IPipelineMessage<TData, TContext> message) where TData : class
        {
            try
            {
                // Message is in flight
                _pipeline.MessageInFlight(message);

                // Localize
                var thunks = GetInstanceThunks<TData>(message.Context);
                var tasks = thunks.Select(thunk => ((Thunk<TData, TContext>) thunk).Invoke(message));
                
                // Message is in flight simulataneously
                await Task.WhenAll(tasks);
            }
            catch (Exception exception)
            {
                await _pipeline.MessageException(message, exception);
            }
            finally
            {
                // Message has completed
                _pipeline.MessageCompleted(message);
            }
        }

        private Thunk[] GetInstanceThunks<TData>(TContext context) where TData : class
        {
            var thunks = default(Thunk[]);

            // Cache targeted version of thunk
            if (!context.Thunks.ContainsKey(this))
            {
                lock (this)
                {
                    if (!context.Thunks.ContainsKey(this))
                    {
                        thunks = _thunks.Select(thunk => thunk.Retarget<TData,TContext>(context.Components)).ToArray();
                        context.Thunks.Add(this, thunks);
                    }
                    else
                    {
                        thunks = context.Thunks[this];
                    }
                }
            }
            else
            {
                thunks = context.Thunks[this];
            }

            return thunks;
        }
    }
}
