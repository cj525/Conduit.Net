using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pipes.Interfaces;
using Pipes.Types;

namespace Pipes.Abstraction
{
    public abstract class PipelineMessage : IPipelineMessage
    {
        // ReSharper disable once MemberCanBeProtected.Global
        private readonly Pipeline _pipeline;

        public PipelineComponent Sender { get; private set; }

        public IEnumerable<IPipelineMessage> Stack { get; private set; }

        protected PipelineMessage(Pipeline pipeline, PipelineComponent sender, IPipelineMessage previous = null )
        {
            _pipeline = pipeline;
            Sender = sender;
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
                Stack = Enumerable.Empty<IPipelineMessage>();
        }

        public IEnumerable<object> DataStack
        {
            get { return Stack.Concat(new[] {this}).Cast<IPipelineMessage<object>>().Select( message => message.Data ); }
        }

        public IPipelineMessage Top
        {
            get
            {
                return Stack.Concat(new[] { this }).First();
            }
        }

        public void Emit<T>(T data) where T : class
        {
            _pipeline.EmitMessage(new PipelineMessage<T>(_pipeline, Sender, data, this));
        }

        public async Task EmitAsync<T>(T data) where T : class
        {
            await _pipeline.EmitMessageAsync(new PipelineMessage<T>(_pipeline, Sender, data, this));
        }

        public void EmitChain<T>(PipelineComponent origin, T data) where T : class
        {
            _pipeline.EmitMessage(new PipelineMessage<T>(_pipeline, origin, data, this));
        }

        public async Task EmitChainAsync<T>(PipelineComponent origin, T data) where T : class
        {
            await _pipeline.EmitMessageAsync(new PipelineMessage<T>(_pipeline, origin, data, this));
        }

        public bool RaiseException(Exception exception)
        {
            var pipelineException = new PipelineException(_pipeline, exception, this);

            return _pipeline.HandleException(pipelineException); ;
        }

        public void TerminateSource()
        {
            Sender.TerminateSource(this);
        }
    }


    public class PipelineMessage<T> : PipelineMessage, IPipelineMessage<T> where T : class
    {
        internal PipelineMessage(Pipeline pipeline, PipelineComponent sender, T data, IPipelineMessage previous = null) : base( pipeline, sender, previous )
        {
            Data = data;
        }

        public T Data { get; private set; }
    }
}
