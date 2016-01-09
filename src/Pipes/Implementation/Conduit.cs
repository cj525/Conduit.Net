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
                // Locals
                var context = message.Context;
                var thunks = default(Thunk[]);
                // Cache targeted version of thunk
                if (!context.Thunks.ContainsKey(this))
                {
                    lock (this)
                    {
                        if (!context.Thunks.ContainsKey(this))
                        {
                            thunks = _thunks.Select(thunk => thunk.AttachTo<TData,TContext>(context.Components)).ToArray();
                            context.Thunks.Add( this, thunks);
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

                await Task.WhenAll(thunks.Select(thunk => ((Thunk<TContext>)thunk).Invoke(message)));
            }
            catch (Exception exception)
            {
                // Wrap exception with message (containing context) due to task not being available here (on purpose)
                var pipelineException = new PipelineException<TContext>(_pipeline, message, exception);

                // Handle the exception
                _pipeline.HandleException(pipelineException);
            }
        }


    }
}
