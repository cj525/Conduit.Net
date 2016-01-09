using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pipes.Exceptions;
using Pipes.Extensions;
using Pipes.Implementation;
using Pipes.Interfaces;
using Pipes.Stubs;
using Pipes.Types;

namespace Pipes.Abstraction
{
    public abstract class Pipeline : Pipeline<OperationContext>
    {
        protected internal override OperationContext ConstructContext<TData>(TData data, object meta = null)
        {
            return new OperationContext();
        }
    }

    public abstract class Pipeline<TContext> : IDisposable where TContext : OperationContext
    {
        private readonly Type _thisType;

        private readonly List<Action<Pipeline<TContext>>> _attachActions = new List<Action<Pipeline<TContext>>>();

        private readonly List<IPipelineComponent<TContext>> _components = new List<IPipelineComponent<TContext>>();
        private readonly List<MessageTap<TContext>> _taps = new List<MessageTap<TContext>>();

        private readonly List<IPipelineConstructorStub<TContext>> _ctors = new List<IPipelineConstructorStub<TContext>>();
        private readonly List<InvocationStub<TContext>> _invocations = new List<InvocationStub<TContext>>();

        private readonly List<ReceiverStub<TContext>> _receivers = new List<ReceiverStub<TContext>>();

        // Both these are [senderType,[dataType,{entry}]]
        private readonly Dictionary<Type, Dictionary<Type, TransmitterStub<TContext>>> _transmitters = new Dictionary<Type, Dictionary<Type, TransmitterStub<TContext>>>();
        // This merges multiple receiver (tubes) into a single call
        private readonly Dictionary<Type, Dictionary<Type, Conduit<TContext>>> _conduits = new Dictionary<Type, Dictionary<Type, Conduit<TContext>>>();

        // TODO: Probably don't need to track these 
        private readonly List<Route<TContext>> _routes = new List<Route<TContext>>();

        private bool _terminated;

        // Atomic write, dirty read
        private int _totalMessagesInFlight;

        protected static Task NoOp => Target.EmptyTask;

        //public IEnumerable<IPipelineComponent<TContext>> Components { get { return _components; } } 

        protected Pipeline()
        {
            _thisType = GetType();
        }

        /// <summary>
        /// Derived class should use the IPipelineBuilder to describe 
        /// the components and conduits which comprise this pipeline.
        /// </summary>
        /// <param name="thisPipeline"></param>
        protected abstract void Describe(IPipelineBuilder<TContext> thisPipeline);

        public virtual void Initialize()
        {
            Build();
        }

        protected internal abstract TContext ConstructContext<TData>(TData data, object meta = null) where TData : class;

        protected internal virtual IPipelineComponent<TContext>[] ConstructComponents()
        {
            var results = _ctors.Select(ctor => ctor.Construct()).ToArray();
            results.Apply(component => component.AttachTo(this));
            return results;
        }


        protected virtual void MessageInFlight<TData>(IPipelineMessage<TData, TContext> message) where TData : class
        {
            Interlocked.Increment(ref _totalMessagesInFlight);
            message.Context.MessageInFlight();
        }

        protected virtual void MessageCompleted<TData>(IPipelineMessage<TData, TContext> message) where TData : class
        {
            message.Context.MessageCompleted();
            Interlocked.Decrement(ref _totalMessagesInFlight);
        }

        protected virtual void HandleUnroutableMessage<T>(IPipelineMessage<T, TContext> message) where T : class
        {
            throw new UnroutablePipelineMessageException(typeof (T), message);
        }

        protected internal virtual void HandleException(PipelineException<TContext> pipelineException)
        {
            // Default is to fault the context using the original exception
            pipelineException.PipelineMessage.Context.Fault(pipelineException.InnerException).Wait();
        }


        /// <summary>
        /// Build and attach all pipeline components
        /// </summary>
        protected void Build()
        {
            // Build the pipeline
            ConstructWith(new Builder<TContext>(this));

            // Attach components 
            AttachAll();

            // Connect messaging system
            //GenerateAndConnectConduits();
        }

        private void AttachAll()
        {
            // This created our component list
            // Attach components attached from building blocks
            _ctors.Cast<Stub<TContext>>().Apply(ctor => ctor.AttachTo(this));
            _invocations.Apply(invocation => invocation.AttachTo(this));

            // Figure out what the components do
            _components.Apply(component => component.Build());

            // Apply their building blocks
            _components.Apply(component => component.AttachTo(this));

            // No longer need component references
            _components.Clear();

            // Connect external communication
            // This will add new rx's
            _taps.Apply(tap => tap.AttachTo(this));

            // Connect internal communication
            _receivers.Apply(rx => rx.AttachTo(this));
            _transmitters.Apply(type => type.Value.Apply(tx => tx.Value.AttachTo(this)));
        }

        //private void GenerateAndConnectConduits()
        //{
        //    // Gather sender's types
        //    var senderTypes = _components.Select(component => component.GetType()).Distinct();

        //    // For each sender, including null (tap), create conduit for each type of sendable message
        //    foreach (var senderType in senderTypes)
        //    {
        //        // Localize loop variable
        //        var sType = senderType;
        //        var sCtor = _ctors.FirstOrDefault(ctor => ctor.ContainedType == sType);

        //        // Add conduit sender slot
        //        var senderSlot = new Dictionary<Type, List<Route<TContext>>>();
        //        _conduits.Add(senderType, senderSlot);

        //        // Create tx/rx pairs
        //        foreach (var tx in _transmitters.Where(tx => tx.Component.GetType() == sType))
        //        {
        //            // Localize loop variable
        //            var ltx = tx;
        //            var rxs = _receivers.Where(rx => rx.Component != null && rx.ContainedType.IsAssignableFrom(ltx.ContainedType) && rx.Component != tx.Component).ToArray(); // rx != tx !!!
        //            foreach (var rx in rxs)
        //            {
        //                // Localize loop variable
        //                var lrx = rx;
        //                var messageType = rx.ContainedType;
        //                var tCtor = _ctors.FirstOrDefault(ctor => ctor.ContainedType == lrx.Component.GetType());

        //                // Create message slot for this sender
        //                if (!senderSlot.ContainsKey(messageType))
        //                    senderSlot.Add(messageType, new List<Route<TContext>>());

        //                var messageSlot = senderSlot[messageType];

        //                ////// If this is a manifold, create/use manifold entry
        //                ////if (tCtor is ConstructorManifoldStub<TContext>)
        //                ////{
        //                ////    var manifolds = messageSlot.Where(slot => slot.Target == tCtor && slot.Source == sCtor).ToArray();
        //                ////    if (!manifolds.Any())
        //                ////    {
        //                ////        var conduit = new Conduit<TContext>(sCtor, tCtor);
        //                ////        messageSlot.Add(conduit);
        //                ////        messageSlot = conduit.AsManifold();
        //                ////    }
        //                ////    else
        //                ////    {
        //                ////        messageSlot = manifolds.First().AsManifold();
        //                ////    }
        //                ////}

        //                // Find declared conduits
        //                if (!IsAutoWired)
        //                {
        //                    var existingTubes = _tubes.Where(tube => tube.Source == sCtor && tube.Target == tCtor && (tube.DataType == null || tube.DataType == messageType)).ToArray();

        //                    // If more than one specified
        //                    if (existingTubes.Length > 1)
        //                    {
        //                        // Use the typed channel
        //                        existingTubes = existingTubes.Where(tube => tube.DataType != null).ToArray();

        //                        // Not just one?  That's a problem
        //                        if (existingTubes.Length != 1)
        //                            throw new NotSupportedException("Overly specified conduit.. too many (typed) matching routes");
        //                    }

        //                    // No routes
        //                    if (existingTubes.Length != 1)
        //                        continue;

        //                    //    throw new NotSupportedException($"Under specified conduit.. no matching routes for {senderType.Name} to send a {tx.ContainedType.Name}");

        //                    // Tube exists, use it! (well, copy it first)..  (now it's promote to conduit)
        //                    messageSlot.Add(existingTubes.First().Clone(rx));
        //                }
        //                else
        //                {
        //                    // And add said conduit
        //                    messageSlot.Add(new Route<TContext>(sCtor, tCtor, messageType) { Receiver = rx });
        //                }
        //            }
        //        }
        //    }


        //    var tapSlots = AttachTaps();

        //    // Lastly add invocations
        //    foreach (var invocation in _invocations)
        //    {
        //        var messageType = invocation.ContainedType;
        //        if (!tapSlots.ContainsKey(messageType))
        //            tapSlots.Add(messageType, new List<Route<TContext>>());
        //        var mSlot = tapSlots[messageType];
        //        var linv = invocation;
        //        // ReSharper disable once LoopCanBeConvertedToQuery
        //        foreach (var rx in _receivers.Where(rx => rx.Component != null && rx.Component.GetType() == linv.Target.ContainedType && rx.ContainedType.IsAssignableFrom(messageType)))
        //        {
        //            var lrx = rx;
        //            var tCtor = _ctors.FirstOrDefault(ctor => ctor.ContainedType == lrx.Component.GetType());
        //            mSlot.Add(new Route<TContext>(invocation, tCtor, messageType) { Receiver = rx });
        //        }

        //    }
        //}

        //private Dictionary<Type, List<Route<TContext>>> AttachTaps()
        //{
        //    // Add taps sender slot
        //    var tapSlots = new Dictionary<Type, List<Route<TContext>>>();
        //    _conduits.Add(_thisType, tapSlots);

        //    // Now add all the taps
        //    foreach (var tap in _taps)
        //    {
        //        var ltap = tap;
        //        var messageType = tap.DataType;

        //        // Get transmitters for this message
        //        var senders = _transmitters.Where(tx => messageType.IsAssignableFrom(tx.ContainedType)).ToArray();
        //        var txTypes = senders.Select(target => target.Component.GetType()).Distinct().ToArray();
        //        //foreach (var ctor in targetComponents.Select(targetComponent => _ctors.FirstOrDefault(c => c.Component == targetComponent)))

        //        // Apply the tap for every tx that matches
        //        foreach (var txType in txTypes)
        //        {
        //            var slot = _conduits[txType];
        //            if (!slot.ContainsKey(messageType))
        //                slot.Add(messageType, new List<Route<TContext>>());
        //            //slot[messageType].Add(tap);
        //        }
        //        foreach (var txType in txTypes)
        //        {
        //            var slot = _conduits[txType];
        //            foreach (var s in slot)
        //            {
        //                if (s.Key.IsAssignableFrom(messageType))
        //                    s.Value.Add(tap);
        //            }
        //        }

        //        // Create message slot for this sender
        //        if (!tapSlots.ContainsKey(messageType))
        //            tapSlots.Add(messageType, new List<Route<TContext>>());
        //        var mSlot = tapSlots[messageType];
        //        mSlot.Add(tap);

        //        //mSlot.Add(tap);
        //    }
        //    return tapSlots;
        //}

        [DebuggerHidden]
        internal void RouteMessage<TData>(IPipelineMessage<TData, TContext> message, Stub<TContext> invocationTarget = null ) where TData : class
        {
            // Locals
            var context = message.Context;

            // Don't send messages that in a terminated pipeline
            if (_terminated)
            {
                throw new OperationCanceledException("Pipeline has been terminated.");
            }

            // Don't send messages in a terminated context
            if (!context.CompletionStateIsUnset)
                throw new OperationCanceledException("PipelineContext is " + (context.IsFaulted ? "faulted" : context.IsCancelled ? "cancelled" : "completed.  Can't route message."));

            try
            {
                // Track messages in flight (both total and per context)
                MessageInFlight(message);

                // Sender is null during invocation, in which case, use the pipeline itself instead of a component instance.
                var senderType = message.Sender?.GetType() ?? _thisType;
                var dataType = typeof (TData);

                // Find the conduit
                var conduit = LookupConduit<TData>(senderType, dataType, invocationTarget);

                // Invoke conduit
                conduit.Transport(message);
            }
            catch (NoConduitException)
            {
                // Could not find any routes
                HandleUnroutableMessage(message);
            }
            catch (Exception exception)
            {
                // Wrap exception so message and context are available
                var exceptionMessage = new PipelineException<TContext>(this, message, exception);

                // Abstraction
                HandleException(exceptionMessage);
            }
            finally
            {
                // This message is done, no matter how it got that way
                MessageCompleted(message);
            }
        }

        private Conduit<TContext> LookupConduit<TData>(Type senderType, Type dataType, Stub<TContext> invocationTarget = null) where TData : class
        {
            // Locals
            var senderRoutes = default(Dictionary<Type, Conduit<TContext>>);

            // ReSharper disable once InconsistentlySynchronizedField
            if (!_conduits.ContainsKey(senderType))
            {
                lock (senderType)
                {
                    if (!_conduits.ContainsKey(senderType))
                    {
                        senderRoutes = new Dictionary<Type, Conduit<TContext>>();
                        _conduits.Add(senderType, senderRoutes);
                    }
                    else
                    {
                        senderRoutes = _conduits[senderType];
                    }
                }
            }
            else
            {
                // ReSharper disable once InconsistentlySynchronizedField
                senderRoutes = _conduits[senderType];
            }

            // Locate the conduit for this type of message data
            var conduit = default(Conduit<TContext>);
            if (!senderRoutes.ContainsKey(dataType))
            {
                lock (dataType)
                {
                    if (!senderRoutes.ContainsKey(dataType))
                    {
                        conduit = GenerateConduit<TData>(senderType, invocationTarget);
                        senderRoutes.Add(dataType, conduit);
                    }
                    else
                    {
                        conduit = senderRoutes[dataType];
                    }
                }
            }
            else
            {
                // Single data-typed cast for all contained routes
                conduit = senderRoutes[dataType];
            }

            // Found or created conduit
            return conduit;
        }

        private Conduit<TContext> GenerateConduit<TData>(Type senderType, Stub<TContext> invocationTarget = null) where TData : class
        {
            // Localize
            Type dataType = typeof (TData);
            var routes = default(Route<TContext>[]);
            if (senderType != _thisType)
            {
                // Ensure transmitter type is known
                if (!_transmitters.ContainsKey(senderType))
                    throw new NotAttachedException($"Sender {senderType.Name} is not registered in pipeline {_thisType.Name}");

                // Ensure transmitter exists for data type
                var senderTransmitter = _transmitters[senderType];
                if (!senderTransmitter.ContainsKey(dataType))
                    throw new NotAttachedException($"Sender {senderType.Name} did not register emission of type {dataType.Namespace} in pipeline {_thisType.Name}.");
                //senderTransmitter[dataType].Routes.ToArray();
                routes = _routes.Where(route => route.Source?.ContainedType == senderType && (route.DataType == null || dataType.CanBeCastedTo(route.DataType))).ToArray();
            }
            else
            {
                routes = _routes.Where(route => route.Target == invocationTarget && dataType.CanBeCastedTo(route.DataType)).ToArray();
            }

            // Is explicitly wired
            if (!routes.Any())
            {
                //throw new NotAttachedException($"Sender {senderType.Name} did not register routes for type {dataType.Name} in pipeline {_thisType.Name}.");
                throw new NoConduitException();
            }

            var delegates = new List<Func<IPipelineComponent, IPipelineMessage<TContext>, TData, Task>>();
            var indexes = new List<int>();
            var targets = new List<Target>();
            foreach (var route in routes)
            {
                // Private
                if (!route.IsPrivate)
                {
                    // Find applicable taps
                    targets.AddRange(_taps.Where(tap => dataType.CanBeCastedTo(tap.DataType)).Select(tap => tap.Receiver.Target));
                }
                // Broadcast
                if (route.Target == null)
                {
                    targets.AddRange(_receivers.Where(rx => dataType.CanBeCastedTo(rx.ContainedType)).Select(rx => rx.Target));
                }
                // Invocation or fully connexted
                else //if (route.Source == null)
                {
                    // rx.Component is null on taps
                    // target.contained type is component on invocation
                    targets.AddRange(_receivers.Where(rx => rx.Component != null && rx.Component.GetType() == route.Target.ContainedType && dataType.CanBeCastedTo(rx.ContainedType)).Select(rx => rx.Target));
                }
            }

            if (!targets.Any())
            {
                throw new NoConduitException();
            }

            return new Conduit<TContext>(this, targets.Select(target => new Thunk<TData,TContext>(target)));
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public void ConstructWith(IPipelineBuilder<TContext> builder)
        {
            // Figure out what I need and what I do (get my building blocks)
            Describe(builder);

            // Apply building block actions for pipeline
            this.ApplyOver(_attachActions);
        }

        public void Reset()
        {
            _attachActions.Clear();
            _ctors.Clear();
            _invocations.Clear();
            _receivers.Clear();
            _transmitters.Clear();
            _components.Clear();
            _taps.Clear();
            _routes.Clear();
        }

        public async Task WaitForIdle(int waitTimeSliceMs = 50)
        {
            while (_totalMessagesInFlight > 0)
            {
                await Task.Delay(waitTimeSliceMs);
            }
        }

        ///// <summary>
        ///// Shuts down a pipeline
        ///// </summary>
        //public void ShutdownConduitThreads()
        //{
        //    // This method (and the entire code graph under it) must be idempotent
        //    _conduits.SelectMany(kv => kv.Value).SelectMany(kv => kv.Value).Apply(conduit => conduit.ShutdownThreads());
        //}

        public IPipelineMessageTap<T,TContext> CreateMessageTap<T>() where T : class
        {
            var tap = new MessageTap<T,TContext>(this);
            _taps.Add(tap);
            return tap;
        }

        [DebuggerHidden]
        internal void EmitMessage<T>(IPipelineMessage<T,TContext> message) where T : class
        {
            RouteMessage(message);
        }

        public virtual void Dispose()
        {
            WaitForIdle().Wait();
        }

        /// <summary>
        /// Should only be used for a hard-shutdown of all active contexts.  Not recommended, may be depreciated.
        /// </summary>
        public async Task Terminate(int waitTimeSliceMs = 50)
        {
            _terminated = true;
            await WaitForIdle(waitTimeSliceMs);
        }

        #region - Building -

        internal void OnAttach(Action<Pipeline<TContext>> action)
        {
            _attachActions.Add(action);
        }

        internal void AttachComponent(IPipelineComponent<TContext> component)
        {
            _components.Add(component);
        }


        internal void AddCtor(IPipelineConstructorStub<TContext> ctor)
        {
            _ctors.Add(ctor);
        }

        //internal void AddCtorManifold<TComponent>(ConstructorManifoldStub<TComponent, TContext> ctors) where TComponent : IPipelineComponent<TContext>
        //{
        //    _ctors.Add(ctors);
        //}

        internal void AddInvocation(InvocationStub<TContext> invocation)
        {
            _invocations.Add(invocation);
        }

        internal void AddTx(TransmitterStub<TContext> tx)
        {
            var componentType = tx.Component.GetType();
            if (!_transmitters.ContainsKey(componentType))
            {
                _transmitters.Add(componentType, new Dictionary<Type, TransmitterStub<TContext>>());
            }

            if (_transmitters[componentType].ContainsKey(tx.ContainedType))
                throw new NotSupportedException($"Duplicate Transmitter in {componentType.Name} for message of type {tx.ContainedType.Name}");

            _transmitters[componentType].Add(tx.ContainedType, tx);
        }

        internal void AddRx(ReceiverStub<TContext> rx, IPipelineComponent<TContext> component)
        {
            _receivers.Add(rx);
        }

        internal void AddRoutes(List<Route<TContext>> routes)
        {
            _routes.AddRange(routes);
        }


        //// ReSharper disable once MemberCanBePrivate.Global
        //internal void Emit<T>(IPipelineComponent<TContext> sender, T data, TContext context) where T : class
        //{
        //    var message = new PipelineMessage<T, TContext>(this, sender, data, context);
        //    EmitMessage(message);
        //}

        //// ReSharper disable once MemberCanBePrivate.Global
        //internal Task EmitAsync<T>(IPipelineComponent<TContext> sender, T data, TContext context) where T : class
        //{
        //    var message = new PipelineMessage<T, TContext>(this, sender, data, context);
        //    return EmitMessageAsync(message);
        //}


        #endregion

        #region - Declaration -

        protected static Stub<TContext> Component => default(Stub<TContext>);

        #endregion

        public class UnroutablePipelineMessageException : Exception
        {
            public IPipelineMessage<TContext> PipelineMessage { get; private set; }

            public Type DataType { get; private set; }

            public UnroutablePipelineMessageException(Type type, IPipelineMessage<TContext> message)
            {
                PipelineMessage = message;
                DataType = type;
            }
        }
    }
}