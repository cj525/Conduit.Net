﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Example.PipelineAdjuncts;
using Pipes.Example.PipelineMessages;
using Pipes.Example.PipelineMeta;
using Pipes.Example.Schema;
using Pipes.Interfaces;
using Pipes.Types;

namespace Pipes.Example.PipelineComponents
{
    class PocoEmitter<T> : PipelineComponent where T : class, new()
    {
        protected override void Describe(IPipelineComponentBuilder<OperationContext> thisComponent)
        {
            thisComponent
                .Receives<FieldedData>()
                .WhichCalls(Inflate);

            thisComponent
                .Emits<T>();

        }

        private void Inflate(IPipelineMessage<FieldedData, OperationContext> message)
        {
            // Localize
            var context = message.Context;
            var data = message.Data;

            // Get database connection (would be from connection pool factory)
            var cache = context.EnsureAdjunct(() => new ReflectionCache<T>());

            // Create the poco
            var poco = cache.Inflate(data.Keys, field => data[field]);

            // Drop poco in pipeline
            Emit(message, poco);
        }
    }
}
