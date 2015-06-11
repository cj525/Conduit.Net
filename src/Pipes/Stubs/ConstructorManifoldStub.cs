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
        private readonly int _count;
        private ConstructorStub<T>[] _contents;

        internal bool IsSupportClass { get; set; }

        public ConstructorManifoldStub(PipelineComponent component, int count) : base(component,typeof(T))
        {
            _count = count;
            _contents = new ConstructorStub<T>[] {};
        }

        internal void Add(ConstructorStub<T> ctor)
        {
            _contents = Enumerable.Range(0, _count).Select(_ => ctor).ToArray();
        }

        internal override void AttachTo(Pipeline pipeline)
        {
            _contents.Apply(ctor => ctor.AttachTo(pipeline));
            base.AttachTo(pipeline);
        }
    }
}