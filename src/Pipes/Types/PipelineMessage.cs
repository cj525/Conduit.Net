using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Interfaces;

namespace Pipes.Types
{
    public abstract class PipelineMessage<TScope> : IPipelineMessage<TScope>
    {
        // ReSharper disable once MemberCanBeProtected.Global
        private readonly Pipeline<TScope> _pipeline;

        public IPipelineComponent<TScope> Sender { get; private set; }

        public IEnumerable<IPipelineMessage<TScope>> Stack { get; private set; }

        public TScope Scope { get; private set; }


        protected PipelineMessage(Pipeline<TScope> pipeline, IPipelineComponent<TScope> sender, TScope scope, IPipelineMessage<TScope> previous = null)
        {
            _pipeline = pipeline;
            Sender = sender;
            Scope = scope;
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
                Stack = Enumerable.Empty<IPipelineMessage<TScope>>();
        }

        public IEnumerable<object> DataStack
        {
            get { return Stack.Concat(new[] {this}).Cast<IPipelineMessage<object,TScope>>().Select( message => message.Data ); }
        }

        public IPipelineMessage<TScope> Top
        {
            get
            {
                return Stack.Concat(new[] { this }).First();
            }
        }

        [DebuggerHidden]
        public void Emit<TData>(TData data, TScope scope = default(TScope)) where TData : class
        {
            if (scope == null || scope.Equals(default(TScope)))
            {
                scope = Scope;
            }
            _pipeline.EmitMessage(new PipelineMessage<TData, TScope>(_pipeline, Sender, data, scope, this));
        }

        [DebuggerHidden]
        public async Task EmitAsync<TData>(TData data, TScope scope = default(TScope)) where TData : class
        {
            if (scope == null || scope.Equals(default(TScope)))
            {
                scope = Scope;
            }
            await _pipeline.EmitMessageAsync(new PipelineMessage<TData, TScope>(_pipeline, Sender, data, scope, this));
        }

        [DebuggerHidden]
        public void EmitChain<TData>(IPipelineComponent<TScope> origin, TData data, TScope scope = default(TScope)) where TData : class
        {
            if (scope == null || scope.Equals(default(TScope)))
            {
                scope = Scope;
            }
            _pipeline.EmitMessage(new PipelineMessage<TData, TScope>(_pipeline, origin, data, scope, this));
        }

        [DebuggerHidden]
        public async Task EmitChainAsync<TData>(IPipelineComponent<TScope> origin, TData data, TScope scope = default(TScope)) where TData : class
        {
            if (scope == null || scope.Equals(default(TScope)))
            {
                scope = Scope;
            }
            await _pipeline.EmitMessageAsync(new PipelineMessage<TData, TScope>(_pipeline, origin, data, scope, this));
        }

        public bool RaiseException(Exception exception, TScope scope = default(TScope))
        {
            var pipelineException = new PipelineException<TScope>(_pipeline, exception, scope, this);

            return _pipeline.HandleException(pipelineException); ;
        }

        public void TerminateSource()
        {
            Sender.TerminateSource(this);
        }
    }


    public class PipelineMessage<TData, TScope> : PipelineMessage<TScope>, IPipelineMessage<TData, TScope> where TData : class
    {
        internal PipelineMessage(Pipeline<TScope> pipeline, IPipelineComponent<TScope> sender, TData data, TScope scope, IPipelineMessage<TScope> previous = null) : base( pipeline, sender, scope, previous )
        {
            Data = data;
        }

        public TData Data { get; private set; }
    }

}
