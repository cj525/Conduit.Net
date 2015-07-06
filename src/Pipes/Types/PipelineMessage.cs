using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Interfaces;

namespace Pipes.Types
{
    public abstract class PipelineMessage<TContext> : IPipelineMessage<TContext> where TContext : class
    {
        // ReSharper disable once MemberCanBeProtected.Global
        private readonly Pipeline<TContext> _pipeline;

        public IPipelineComponent<TContext> Sender { get; private set; }

        public IEnumerable<IPipelineMessage<TContext>> Stack { get; private set; }

        public TContext Context { get; private set; }


        protected PipelineMessage(Pipeline<TContext> pipeline, IPipelineComponent<TContext> sender, TContext context, IPipelineMessage<TContext> previous = null)
        {
            _pipeline = pipeline;
            Sender = sender;
            Context = context;
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
            get { return Stack.Concat(new[] {this}).Cast<IPipelineMessage<object,TContext>>().Select( message => message.Data ); }
        }

        public IPipelineMessage<TContext> Top
        {
            get
            {
                return Stack.Concat(new[] { this }).First();
            }
        }

        [DebuggerHidden]
        public void Emit<TData>(TData data, TContext context = default(TContext)) where TData : class
        {
            if (context == null || context.Equals(default(TContext)))
            {
                context = Context;
            }
            _pipeline.EmitMessage(new PipelineMessage<TData, TContext>(_pipeline, Sender, data, context, this));
        }

        [DebuggerHidden]
        public Task EmitAsync<TData>(TData data, TContext context = default(TContext)) where TData : class
        {
            if (context == null || context.Equals(default(TContext)))
            {
                context = Context;
            }
            
            return _pipeline.EmitMessageAsync(new PipelineMessage<TData, TContext>(_pipeline, Sender, data, context, this));
        }

        [DebuggerHidden]
        public void EmitChain<TData>(IPipelineComponent<TContext> origin, TData data, TContext context = default(TContext)) where TData : class
        {
            if (context == null || context.Equals(default(TContext)))
            {
                context = Context;
            }
            _pipeline.EmitMessage(new PipelineMessage<TData, TContext>(_pipeline, origin, data, context, this));
        }

        [DebuggerHidden]
        public Task EmitChainAsync<TData>(IPipelineComponent<TContext> origin, TData data, TContext context = default(TContext)) where TData : class
        {
            if (context == null || context.Equals(default(TContext)))
            {
                context = Context;
            }
            
            return _pipeline.EmitMessageAsync(new PipelineMessage<TData, TContext>(_pipeline, origin, data, context, this));
        }

        public bool RaiseException(Exception exception, TContext context = default(TContext))
        {
            var pipelineException = new PipelineException<TContext>(_pipeline, exception, context, this);

            return _pipeline.HandleException(pipelineException); ;
        }

        public void TerminateSource()
        {
            Sender.TerminateSource(this);
        }
    }


    public class PipelineMessage<TData, TContext> : PipelineMessage<TContext>, IPipelineMessage<TData, TContext>
        where TData : class
        where TContext : class
    {
        internal PipelineMessage(Pipeline<TContext> pipeline, IPipelineComponent<TContext> sender, TData data, TContext context, IPipelineMessage<TContext> previous = null) : base( pipeline, sender, context, previous )
        {
            Data = data;
        }

        public TData Data { get; private set; }
    }

}
