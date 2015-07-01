using System;
using System.Collections.Generic;
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
    public abstract class Pipeline : Pipeline<Object> { }

    public abstract class Pipeline<TScope> : IDisposable  
    {
        private readonly Type _thisType;

        private readonly List<Action<Pipeline<TScope>>> _attachActions = new List<Action<Pipeline<TScope>>>();

        private readonly List<IPipelineComponent<TScope>> _components = new List<IPipelineComponent<TScope>>();
        private readonly List<MessageTap<TScope>> _taps = new List<MessageTap<TScope>>();

        private readonly List<Stub<TScope>> _ctors = new List<Stub<TScope>>();
        private readonly List<InvocationStub<TScope>> _invocations = new List<InvocationStub<TScope>>();

        private readonly List<ReceiverStub<TScope>> _receivers = new List<ReceiverStub<TScope>>();
        private readonly List<TransmitterStub<TScope>> _transmitters = new List<TransmitterStub<TScope>>();

        private readonly Dictionary<Type, Dictionary<Type, List<Conduit<TScope>>>> _conduits = new Dictionary<Type, Dictionary<Type, List<Conduit<TScope>>>>();
        private readonly List<Conduit<TScope>> _tubes = new List<Conduit<TScope>>();

        private Action<PipelineException<TScope>> _exceptionHandler;

        private int _messagesInFlight;

        public PipelineException<TScope> FatalException { get; private set; }

        public int MessagesInFlight { get { return _messagesInFlight; } }

        protected Pipeline()
        {
            _thisType = GetType();
        }

        /// <summary>
        /// Derived class should use the IPipelineBuilder to describe 
        /// the components and conduits which comprise this pipeline.
        /// </summary>
        /// <param name="thisPipeline"></param>
        protected abstract void Describe(IPipelineBuilder<TScope> thisPipeline);

        protected virtual async Task HandleUnknownMessage<T>(IPipelineMessage<T, TScope> message) where T : class
        {
            Console.WriteLine("** Unknown message in pipeline of type: " + typeof(T));
            await Target.EmptyTask;
        }

        /// <summary>
        /// Build and attach all pipeline components
        /// </summary>
        protected void Build()
        {
            // Figure out what I need and what I do (get my building blocks)
            Describe(new Builder<TScope>(this));

            // Apply building block actions for pipeline
            this.ApplyOver(_attachActions);

            // Attach components 
            AttachAll();

            // Connect messaging system
            GenerateAndConnectConduits();
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
            _tubes.Clear();
            _exceptionHandler = null;
        }

        public async Task Shutdown()
        {
            //_conduits.SelectMany(kv => kv.Value).SelectMany(kv => kv.Value).Where(conduit => conduit.OffThread).Apply(conduit => conduit.Shutdown());
            while (_messagesInFlight > 0)
            {
                // Continually yield this thread until messages have stopped
                await Task.Delay(15);
            }
        }


        private void AttachAll()
        {
            // This created our component list
            // Attach components attached from building blocks
            _ctors.Apply(ctor => ctor.AttachTo(this));
            _invocations.Apply(invocation => invocation.AttachTo(this));

            // Figure out what the components do
            _components.Apply(component => component.Build());

            // Apply their building blocks
            _components.Apply(component => component.AttachTo(this));

            // Connect external communication
            // This will add new rx's
            _taps.Apply(tap => tap.AttachTo(this));

            // Connect internal communication
            _receivers.Apply(rx => rx.AttachTo(this));
            _transmitters.Apply(tx => tx.AttachTo(this));
        }
        private void GenerateAndConnectConduits()
        {
            // Gather sender's types
            var senderTypes = _components.Select(component => component.GetType()).Distinct();

            // For each sender, including null (tap), create conduit for each type of sendable message
            foreach (var senderType in senderTypes)
            {
                // Localize loop variable
                var sType = senderType;
                var sCtor = _ctors.FirstOrDefault(ctor => ctor.ContainedType == sType);

                // Add conduit sender slot
                var senderSlot = new Dictionary<Type, List<Conduit<TScope>>>();
                _conduits.Add(senderType, senderSlot);

                // Create tx/rx pairs
                foreach (var tx in _transmitters.Where(tx => tx.Component.GetType() == sType))
                {
                    // Localize loop variable
                    var ltx = tx;
                    var rxs = _receivers.Where(rx => rx.Component != null && rx.ContainedType.IsAssignableFrom(ltx.ContainedType)).ToArray();
                    foreach (var rx in rxs)
                    {
                        // Localize loop variable
                        var lrx = rx;
                        var messageType = rx.ContainedType;
                        var tCtor = _ctors.FirstOrDefault(ctor => ctor.ContainedType == lrx.Component.GetType());

                        // Create message slot for this sender
                        if (!senderSlot.ContainsKey(messageType))
                            senderSlot.Add(messageType, new List<Conduit<TScope>>());

                        var messageSlot = senderSlot[messageType];

                        // If this is a manifold, create/use manifold entry
                        if (tCtor is ConstructorManifoldStub<TScope>)
                        {
                            var manifolds = messageSlot.Where(slot => slot.Target == tCtor && slot.Source == sCtor).ToArray();
                            if (!manifolds.Any())
                            {
                                var conduit = new Conduit<TScope>(sCtor, tCtor);
                                messageSlot.Add(conduit);
                                messageSlot = conduit.AsManifold();
                            }
                            else
                            {
                                messageSlot = manifolds.First().AsManifold();
                            }
                        }

                        // Find declared conduits
                        var existingTubes = _tubes.Where(tube => tube.Source == sCtor && tube.Target == tCtor && (tube.MessageType == null || tube.MessageType == messageType)).ToArray();
                        if (existingTubes.Any())
                        {
                            // If more than one specified
                            if (existingTubes.Length > 1)
                            {
                                // Use the typed channel
                                existingTubes = existingTubes.Where(tube => tube.MessageType != null).ToArray();

                                // Not just one?  That's a problem
                                if (existingTubes.Length != 1)
                                    throw new NotSupportedException("Overly specified conduit.. too many (typed) matching routes");
                            }

                            if (existingTubes.Length != 1)
                                throw new NotSupportedException("Under specified conduit.. no matching routes");

                            // Tube exists, use it! (well, copy it first)..  (now it's promote to conduit)
                            messageSlot.Add(existingTubes.First().Clone(rx));
                        }
                        else
                        {
                            // And add said conduit
                            messageSlot.Add(new Conduit<TScope>(sCtor, tCtor, messageType) { Receiver = rx });
                        }
                    }
                }
            }


            // Add taps sender slot
            var tapSlot = new Dictionary<Type, List<Conduit<TScope>>>();
            _conduits.Add(_thisType, tapSlot);

            // Now add all the taps
            foreach (var tap in _taps)
            {
                var ltap = tap;
                var messageType = tap.MessageType;

                // Get transmitters for this message
                var senders = _transmitters.Where(tx => messageType.IsAssignableFrom(tx.ContainedType)).ToArray();
                var txTypes = senders.Select(target => target.Component.GetType()).Distinct().ToArray();
                //foreach (var ctor in targetComponents.Select(targetComponent => _ctors.FirstOrDefault(c => c.Component == targetComponent)))

                // Apply the tap for every tx that matches
                foreach (var txType in txTypes)
                {
                    var slot = _conduits[txType];
                    if (!slot.ContainsKey(messageType))
                        slot.Add(messageType, new List<Conduit<TScope>>());
                    //slot[messageType].Add(tap);
                }
                foreach (var txType in txTypes)
                {
                    var slot = _conduits[txType];
                    foreach (var s in slot)
                    {
                        if (messageType.IsAssignableFrom(s.Key))
                            s.Value.Add(tap);
                    }
                }

                // Create message slot for this sender
                if (!tapSlot.ContainsKey(messageType))
                    tapSlot.Add(messageType, new List<Conduit<TScope>>());
                var mSlot = tapSlot[messageType];
                mSlot.Add(tap);

                //mSlot.Add(tap);
            }

            // Lastly add invocations
            foreach (var invocation in _invocations)
            {
                var messageType = invocation.ContainedType;
                if (!tapSlot.ContainsKey(messageType))
                    tapSlot.Add(messageType, new List<Conduit<TScope>>());
                var mSlot = tapSlot[messageType];
                var linv = invocation;
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var rx in _receivers.Where(rx => rx.Component != null && rx.Component.GetType() == linv.Target.ContainedType && messageType.IsAssignableFrom(rx.ContainedType)))
                {
                    var lrx = rx;
                    var tCtor = _ctors.FirstOrDefault(ctor => ctor.ContainedType == lrx.Component.GetType());
                    mSlot.Add(new Conduit<TScope>(invocation, tCtor, messageType) { Receiver = rx });
                }

            }
        }

        //[DebuggerHidden]
        internal async Task RouteMessage<T>(IPipelineMessage<T,TScope> message) where T : class
        {
            try
            {
                Interlocked.Increment(ref _messagesInFlight);

                var senderType = message.Sender == null ? _thisType : message.Sender.GetType();
                if (!_conduits.ContainsKey(senderType))
                    throw new NotAttachedException("No handler for sender: " + senderType.Name);

                Conduit<TScope>[] targets = null;
                var sender = _conduits[senderType];
                var dataType = typeof(T);

                if (!sender.ContainsKey(dataType))
                {
                    sender = _conduits[_thisType];

                    targets = sender.Values.SelectMany(knownType => knownType.Where(container => container.MessageType.IsAssignableFrom(dataType))).ToArray();

                    if (!targets.Any())
                    {
                        if (dataType.IsAssignableFrom(typeof(Exception)))
                        {
                            message.RaiseException(message.Data as Exception);
                        }
                        else
                        {
                            await HandleUnknownMessage(message);
                        }
                    }
                }
                else
                {
                    targets = _conduits[senderType][dataType].ToArray();
                }

                try
                {
                    if (targets.Length == 0)
                    {
                        await Target.EmptyTask;
                        return;
                    }

                    if (targets.Length > 1)
                    {
                        await Task.WhenAll(targets.Select(each => each.Invoke(message)));
                        return;
                    }

                    await targets[0].Invoke(message);
                }
                catch (PipelineException<TScope> exception)
                {
                    // Maybe someone will inspect this outside of the upcoming explosion's catcher
                    FatalException = exception;

                    // There was no handler so shutdown the pipe and explode!
                    Dispose();

                    // And boom goes the dynamite
                    throw;
                }
            }
            finally
            {
                Interlocked.Decrement(ref _messagesInFlight);
            }

        }

        internal bool HandleException(PipelineException<TScope> pipelineException)
        {
            if (_exceptionHandler == null)
                return false;

            _exceptionHandler(pipelineException);

            return true;
        }



        //protected void Analyze()
        //{
        //    Describe(new Analyzer(this));
        //}


        public IPipelineMessageTap<T,TScope> CreateMessageTap<T>() where T : class
        {
            var tap = new MessageTap<T,TScope>(this);
            _taps.Add(tap);
            return tap;
        }

        public void SuppressExceptions(Action<PipelineException<TScope>> exceptionHandler)
        {
            _exceptionHandler = exceptionHandler;
        }

        [DebuggerHidden]
        public void Emit<T>(T data, TScope scope) where T : class
        {
            Emit(null, data, scope);
        }

        [DebuggerHidden]
        public async Task EmitAsync<T>(T data, TScope scope) where T : class
        {
            await EmitAsync(null, data, scope);
        }

        [DebuggerHidden]
        public void EmitMessage<T>(IPipelineMessage<T,TScope> message) where T : class
        {
            RouteMessage(message).Wait();
        }

        [DebuggerHidden]
        public async Task EmitMessageAsync<T>(IPipelineMessage<T, TScope> message) where T : class
        {
            await RouteMessage(message);
        }

        public virtual void Dispose()
        {
            // Go through all queue threads and order instant death
            // Dispose of things
            Console.WriteLine("Fix pipeline disposal!");
        }

        public void Terminate()
        {
            Console.WriteLine("Fix forced pipeline termination!");
        }

        public void Terminate(IPipelineComponent<TScope> source)
        {
            throw new NotImplementedException();
        }

        #region - Internal Inteface -

        internal void OnAttach(Action<Pipeline<TScope>> action)
        {
            _attachActions.Add(action);
        }

        internal void AttachComponent(IPipelineComponent<TScope> component)
        {
            _components.Add(component);
        }


        internal void AddCtor<TComponent>(ConstructorStub<TComponent,TScope> ctor) where TComponent : IPipelineComponent<TScope>
        {
            _ctors.Add(ctor);
        }

        internal void AddCtorManifold<TComponent>(ConstructorManifoldStub<TComponent, TScope> ctors) where TComponent : IPipelineComponent<TScope>
        {
            _ctors.Add(ctors);
        }

        internal void AddInvocation(InvocationStub<TScope> invocation)
        {
            _invocations.Add(invocation);
        }

        internal void AddTx(TransmitterStub<TScope> tx, IPipelineComponent<TScope> component)
        {
            _transmitters.Add(tx);
        }

        internal void AddRx(ReceiverStub<TScope> rx, IPipelineComponent<TScope> component)
        {
            _receivers.Add(rx);
        }

        internal void AddTubes(List<Conduit<TScope>> tubes)
        {
            _tubes.AddRange(tubes);
        }


        // ReSharper disable once MemberCanBePrivate.Global
        internal void Emit<T>(IPipelineComponent<TScope> sender, T data, TScope scope) where T : class
        {
            var message = new PipelineMessage<T, TScope>(this, sender, data, scope);
            EmitMessage(message);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        internal async Task EmitAsync<T>(IPipelineComponent<TScope> sender, T data, TScope scope) where T : class
        {
            var message = new PipelineMessage<T, TScope>(this, sender, data, scope);
            await EmitMessageAsync(message);
        }


        #endregion

        #region - Declaration -

        // ReSharper disable once MemberCanBeProtected.Global
        public static Stub<TScope> Component
        {
            get { return default(Stub<TScope>); }
        }

        #endregion


    }
}