using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Interfaces;

namespace Pipes.Types
{
    public abstract class PipelineMessage<TContext> : IPipelineMessage<TContext> where TContext : class, IOperationContext
    {
        // ReSharper disable once MemberCanBeProtected.Global
        private readonly Pipeline<TContext> _pipeline;

        public IPipelineComponent<TContext> Sender { get; }

        public IEnumerable<IPipelineMessage<TContext>> Stack { get; }

        public TContext Context { get; }

        public object Meta { get; }

        public bool IsCancelled { get; set; }

        
        protected PipelineMessage(Pipeline<TContext> pipeline, IPipelineComponent<TContext> sender, TContext context, IPipelineMessage<TContext> previous = null, object meta = null)
        {
            _pipeline = pipeline;
            Sender = sender;
            Context = context;
            Meta = meta;
            if (previous != null)
            {
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                // ^ Too unreadable
                if (previous.Stack != null)
                    Stack = previous.Stack.Concat(new[] {previous});
                else
                    Stack = new[] {previous};
            }
            else
                Stack = Enumerable.Empty<IPipelineMessage<TContext>>();
        }

        public IEnumerable<object> DataStack
        {
            get { return FullStack.Cast<IPipelineMessage<object,TContext>>().Select( message => message.Data ); }
        }

        public IEnumerable<object> MetaStack
        {
            get { return FullStack.Cast<IPipelineMessage<object, TContext>>().Select(message => message.Meta); }
        } 

        public IEnumerable<IPipelineMessage<TContext>> FullStack
        {
            get
            {
                return Stack.Concat(new[] { this });
            }
        }

        //[DebuggerHidden]
        //public void Emit<TData>(TData data, TContext context = default(TContext)) where TData : class
        //{
        //    if (context == null || context.Equals(default(TContext)))
        //    {
        //        context = Context;
        //    }
        //
        //    _pipeline.EmitMessage(new PipelineMessage<TData, TContext>(_pipeline, Sender, data, context, this));
        //}

        //[DebuggerHidden]
        //public Task EmitAsync<TData>(TData data, TContext context = default(TContext)) where TData : class
        //{
        //    if (context == null || context.Equals(default(TContext)))
        //    {
        //        context = Context;
        //    }
        //    
        //    return _pipeline.EmitMessageAsync(new PipelineMessage<TData, TContext>(_pipeline, Sender, data, context, this));
        //}

        [DebuggerHidden]
        public void Chain<TData>(IPipelineComponent<TContext> origin, TData data, object meta = null) where TData : class
        {
            _pipeline.EmitMessage(new PipelineMessage<TData, TContext>(_pipeline, origin, data, Context, this, meta));
        }

        [DebuggerHidden]
        public Task ChainAsync<TData>(IPipelineComponent<TContext> origin, TData data, object meta = null) where TData : class
        {
            return _pipeline.EmitMessageAsync(new PipelineMessage<TData, TContext>(_pipeline, origin, data, Context, this, meta));
        }

        public bool HandleException(Exception exception)
        {
            var pipelineException = new PipelineException<TContext>(_pipeline, this, exception);

            return _pipeline.HandleException(pipelineException);
        }
    }


    public class PipelineMessage<TData, TContext> : PipelineMessage<TContext>, IPipelineMessage<TData, TContext>
        where TData : class
        where TContext : class, IOperationContext
    {
        internal PipelineMessage(Pipeline<TContext> pipeline, IPipelineComponent<TContext> sender, TData data, TContext context, IPipelineMessage<TContext> previous = null, object meta = null) : base( pipeline, sender, context, previous )
        {
            Data = data;
        }

        public TData Data { get; }
    }
}
