//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Pipes.Abstraction;
//using Pipes.Extensions;
//using Pipes.Interfaces;
//using Pipes.Types;

//namespace Pipes.BuildingBlocks
//{
//    public class Completor<TData,TContext> : BuildingBlock<TContext>, ICompletionSourceBuilder<TData, TContext> where TContext : OperationContext where TData : class
//    {
//        private readonly List<Func<IPipelineMessage<TData, TContext>, Task>> _completionActions;
//        private readonly List<Func<IPipelineMessage<TData, TContext>, Task>> _cancelActions;
//        private readonly List<Func<IPipelineMessage<TData, TContext>, Task>> _faultActions;
//        private CompletionBuffer<TData> _completionBuffer;
//        private int _maxConcurrentSlots;
//        private bool _hasMaxConcurrency;

//        public Completor(Pipeline<TContext> pipeline) : base(pipeline)
//        {
//            _completionActions = new List<Func<IPipelineMessage<TData, TContext>, Task>>();
//            _cancelActions = new List<Func<IPipelineMessage<TData, TContext>, Task>>();
//            _faultActions = new List<Func<IPipelineMessage<TData, TContext>, Task>>();
//        }

//        protected override void AttachPipeline(Pipeline<TContext> pipeline)
//        {
//            _completionBuffer = new CompletionBuffer<TData>();
//        }

//        private void DoComplete()
//        {
//            _completionActions.ApplyAndWait(fn=>fn());
//        }

//        public ICompletionSourceBuilder<TData, TContext> WithMaxConcurrency(int max)
//        {
//            _hasMaxConcurrency = true;
//            _maxConcurrentSlots = max;
//        }

//        public ICompletionSourceBuilder<TData, TContext> OnComplete(Func<IPipelineMessage<TData, TContext>, Task> asyncAction)
//        {
//            _completionActions.Add(asyncAction);
//        }

//        public ICompletionSourceBuilder<TData, TContext> OnCancel(Func<IPipelineMessage<TData, TContext>, Task> asyncAction)
//        {
//            _cancelActions.Add(asyncAction);
//        }

//        public ICompletionSourceBuilder<TData, TContext> OnFault(Func<IPipelineMessage<TData, TContext>, Task> asyncAction)
//        {
//            _faultActions.Add(asyncAction);
//        }
//    }
//}
