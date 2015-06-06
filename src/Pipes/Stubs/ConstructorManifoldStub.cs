using System;
using System.Collections.Generic;
using System.Linq;
using Pipes.Abstraction;
using Pipes.Extensions;

namespace Pipes.Stubs
{
    public class ConstructorManifoldStub<T> : Stub
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
                    .Select(item => item.GetDelegate())
                    .Select(ctor => ctor())
                    .If(_initializer != null, source => source.Apply(_initializer))
                    .ToArray()
                );
        }

        internal void Add(ConstructorStub<T> ctor)
        {
            _contents.Add(ctor);
        }

        internal IEnumerable<T> GetInstances()
        {
            return _instances.Value;
        }

        internal void SetInitializer(Action<T> init)
        {
            _initializer = init;
        }


    }
}