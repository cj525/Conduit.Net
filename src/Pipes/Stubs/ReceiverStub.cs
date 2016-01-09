using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.SqlServer.Server;
using Pipes.Abstraction;
using Pipes.Interfaces;
using Pipes.Types;

namespace Pipes.Stubs
{
    public abstract class ReceiverStub<TContext> : Stub<TContext> where TContext : OperationContext
    {
        internal Target Target;

        protected ReceiverStub(IPipelineComponent<TContext> component, Type containedType) : base(component,containedType)
        {
        }
    }

    public class ReceiverStub<TData, TContext> : ReceiverStub<TContext> where TData : class
        where TContext : OperationContext
    {

        public ReceiverStub(IPipelineComponent<TContext> component) : base(component, typeof(TData))
        {
            Pipeline = null;
        }

        public ReceiverStub(Pipeline<TContext> pipeline) : base(null,typeof(TData))
        {
            Pipeline = pipeline;
        }
    }
}