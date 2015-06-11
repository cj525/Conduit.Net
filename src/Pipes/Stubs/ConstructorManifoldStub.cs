using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using Pipes.Abstraction;
using Pipes.Extensions;

namespace Pipes.Stubs
{
    public class ConstructorManifoldStub<T> : Stub where T : PipelineComponent
    {
        private readonly Lazy<IEnumerable<T>> _instances;
        private readonly List<ConstructorStub<T>> _contents;
        private Action<T> _initializer;

        internal bool IsSupportClass { get; set; }

        public ConstructorManifoldStub(PipelineComponent component) : base(component,typeof(T))
        {
            _contents = new List<ConstructorStub<T>>();
            _instances = new Lazy<IEnumerable<T>>(() =>
                _contents
                    .Select(item => item.Ctor())
                    .If(_initializer != null, source => source.Apply(_initializer))
                    .ToArray()
                );
        }

        internal void Add(ConstructorStub<T> ctor)
        {
            _contents.Add(ctor);
        }

        internal void SetInitializer(Action<T> init)
        {
            _initializer = init;
        }

        internal override void AttachTo(Pipeline pipeline)
        {
            _contents.Apply(ctor => ctor.AttachTo(pipeline));
            base.AttachTo(pipeline);
        }
    }
}