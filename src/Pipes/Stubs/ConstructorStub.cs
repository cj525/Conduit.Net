using System;
using Pipes.Abstraction;
using Pipes.Exceptions;

namespace Pipes.Stubs
{
    public class ConstructorStub<T> : Stub
    {
        private readonly Lazy<T> _instance;
        private readonly Func<T> _ctor;

        internal bool OncePerConstruct { private get; set; }
        internal bool IsSupportClass { get; set; }

        public ConstructorStub(PipelineComponent component, Func<T> ctor) : base(component, typeof(T)) 
        {
            _ctor = ctor;
            _instance = new Lazy<T>(ctor);
        }

        internal Func<T> GetDelegate()
        {
            if (OncePerConstruct)
            {
                return () => _instance.Value;
            }
            else
            {
                return _ctor;
            }
        }

        internal override void AttachTo(Pipeline pipeline)
        {
            var value = _instance.Value;

            if (!IsSupportClass)
            {
                var component = value as PipelineComponent;

                if (component == null)
                {
                    throw BadAttachmentException.Exception<T>("Is not a PipelineComponent.");
                }

                Component = component;
                pipeline.AttachComponent(value as PipelineComponent);
            }
            else
            {
                // TODO: Add self to disposables collection
                var disposable = value as IDisposable;
                if (disposable != null)
                {
                    pipeline.AddDisposable(disposable);
                }
            }

            base.AttachTo(pipeline);
        }
    }
}