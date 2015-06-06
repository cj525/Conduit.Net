using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pipes.Exceptions;
using Pipes.Extensions;
using Pipes.Implementation;
using Pipes.Interfaces;
using Pipes.Stubs;
using Pipes.Types;

namespace Pipes.Abstraction
{
    public abstract class Pipeline : IDisposable
    {
        private readonly Type _thisType;

        private readonly List<Action<Pipeline>> _attachActions = new List<Action<Pipeline>>();
        
        private readonly List<PipelineComponent> _components = new List<PipelineComponent>();
        private readonly List<MessageTap> _taps = new List<MessageTap>();

        private readonly List<Stub> _ctors = new List<Stub>();
        private readonly List<InvocationStub> _invocations = new List<InvocationStub>();

        private readonly List<ReceiverStub> _receivers = new List<ReceiverStub>();
        private readonly List<TransmitterStub> _transmitters = new List<TransmitterStub>();

        private readonly Dictionary<Type, Dictionary<Type, List<Conduit>>> _conduits = new Dictionary<Type, Dictionary<Type, List<Conduit>>>(); 
        private readonly List<Conduit> _tubes = new List<Conduit>();

        private Action<PipelineException> _exceptionHandler;

        public PipelineException FatalException { get; private set; }

        protected Pipeline()
        {
            _thisType = GetType();
        }

        /// <summary>
        /// Derived class should use the <see cref="IPipelineBuilder"/> to describe 
        /// the components and conduits which comprise this pipeline.
        /// </summary>
        /// <param name="thisPipeline"></param>
        protected abstract void Describe(IPipelineBuilder thisPipeline);

        protected virtual void HandleUnknownMessage<T>(IPipelineMessage<T> message) where T : class
        {
            Console.WriteLine( "** Unknown message in pipeline of type: " + typeof(T) );
        }

        /// <summary>
        /// Build and attach all pipeline components
        /// </summary>
        protected void Build()
        {
            // Figure out what I need and what I do (get my building blocks)
            Describe(new Builder(this));

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

        public void Shutdown()
        {
            _conduits.SelectMany(kv => kv.Value).SelectMany(kv => kv.Value).Where(conduit => conduit.OffThread).Apply(conduit => conduit.Shutdown());
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
            var senderTypes = _components.Select(component => component.GetType());

            // Find all the potential message types
            var messageTypes = _transmitters.Select(tx => tx.ContainedType).Union(_taps.Select( tap => tap.MessageType )).Distinct().ToArray();

            //_tubes.Apply(tube => _receivers.Where(rx => rx.ContainedType == tube.Target.ContainedType).Apply(rx => tube.Receiver = rx));

            // For each sender, including null (tap), create conduit for each type of sendable message
            foreach (var senderType in senderTypes)
            {
                // Localize loop variable
                var sType = senderType;
                var sCtor = _ctors.FirstOrDefault(ctor => ctor.ContainedType == sType);

                // Add conduit sender slot
                var senderSlot = new Dictionary<Type, List<Conduit>>();
                _conduits.Add(senderType, senderSlot);

                // Create tx/rx pairs
                foreach (var tx in _transmitters.Where(tx => tx.Component.GetType() == sType))
                {
                    // Localize loop variable
                    var ltx = tx;
                    foreach (var rx in _receivers.Where(rx => rx.Component != null && rx.ContainedType.IsAssignableFrom(ltx.ContainedType)))
                    {
                        // Localize loop variable
                        var lrx = rx;
                        var messageType = rx.ContainedType;
                        var tCtor = _ctors.FirstOrDefault(ctor => ctor.ContainedType == lrx.Component.GetType());

                        // Create message slot for this sender
                        if (!senderSlot.ContainsKey(messageType))
                            senderSlot.Add(messageType, new List<Conduit>());

                        var messageSlot = senderSlot[messageType];

                        // Find declared conduits
                        var existingTubes = _tubes.Where(tube => tube.Source == sCtor && tube.Target == tCtor && (tube.MessageType == null || tube.MessageType == messageType)).ToArray();
                        if ( existingTubes.Any())
                        {
                            // If more than one specified
                            if (existingTubes.Length > 1)
                            {
                                // Use the typed channel
                                existingTubes = existingTubes.Where(tube => tube.MessageType != null).ToArray();

                                // Not just one?  That's a problem
                                if( existingTubes.Length != 1 )
                                    throw new NotSupportedException("Overly specified conduit.. too many (typed) matching routes");
                            }

                            if (existingTubes.Length != 1)
                                throw new NotSupportedException("Under specified conduit.. no matching routes");

                            // Tube exists, use it! (well, copy it first)..  (now it's promote to conduit)
                            messageSlot.Add(existingTubes.First().Procreate(rx));
                        }
                        else
                        {
                            // And add said conduit
                            senderSlot[messageType].Add(new Conduit(sCtor, tCtor, messageType) { Receiver = rx });
                        }
                    }
                }
            }


            // Add taps sender slot
            var tapSlot = new Dictionary<Type, List<Conduit>>();
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
                        slot.Add(messageType, new List<Conduit>());
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
                    tapSlot.Add(messageType, new List<Conduit>());
                var mSlot = tapSlot[messageType];
                mSlot.Add(tap);

                //mSlot.Add(tap);
            }

            // Lastly add invocations
            foreach (var invocation in _invocations)
            {
                var messageType = invocation.ContainedType;
                if (!tapSlot.ContainsKey(messageType))
                    tapSlot.Add(messageType, new List<Conduit>());
                var mSlot = tapSlot[messageType];
                var linv = invocation;
                foreach (var rx in _receivers.Where(rx => rx.Component != null && rx.Component.GetType() == linv.Target.ContainedType))
                {
                    var lrx = rx;
                    var tCtor = _ctors.FirstOrDefault(ctor => ctor.ContainedType == lrx.Component.GetType());
                    mSlot.Add(new Conduit(invocation, tCtor, messageType) { Receiver = rx } );
                }
                    
            }
        }

        // <Sender,<MessageType,Conduits>
        

        internal Task RouteMessage<T>(IPipelineMessage<T> message) where T : class
        {
            //Console.WriteLine(" -Sending {0} on Thread {1}", ((IPipelineMessage<object>)message).Data.GetType().Name, Thread.CurrentThread.ManagedThreadId);
            // First search taps
            //var taps = 
            var senderType = message.Sender == null ? _thisType : message.Sender.GetType();
            if (!_conduits.ContainsKey(senderType))
                throw new NotAttachedException("No handler for sender: " + senderType.Name);

            Conduit[] targets = null;
            var sender = _conduits[senderType];
            var dataType = typeof (T);

            if (!sender.ContainsKey(dataType))
            {
                sender = _conduits[_thisType];

                targets = sender.Values.SelectMany(knownType => knownType.Where(container => container.MessageType.IsAssignableFrom(dataType))).ToArray();

                if( !targets.Any())
                {
                    HandleUnknownMessage(message);
                    return Target.EmptyTask;
                }
            }
            else
            {
                targets = _conduits[senderType][dataType].ToArray();
            }
            
            
            
            //var targets = lookup.Where( item => item.).SelectMany( list => list.Value ).SelectMany( list => list.Value ).ToArray();
            try
            {
                if (targets.Length == 0)
                    return Target.EmptyTask;

                if (targets.Length > 1)
                {
                    return Task.WhenAll(targets.Select(each => each.Invoke(message)));
                }

                return targets[0].Invoke(message);
            }
            // ReSharper disable once UnusedVariable
            catch (PipelineException exception)
            {
                // Maybe someone will inspect this outside of the upcoming explosion's catcher
                FatalException = exception;

                // There was no handler so shutdown the pipe and explode!
                Dispose();

                // And boom goes the dynamite
                throw;
            }
        }

        internal bool HandleException(PipelineException pipelineException)
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


        public IPipelineMessageTap<T> CreateMessageTap<T>() where T : class
        {
            var tap = new MessageTap<T>(this);
            _taps.Add(tap);
            return tap;
        }

        public void SuppressExceptions( Action<PipelineException> exceptionHandler )
        {
            _exceptionHandler = exceptionHandler;
        }

        public void Emit<T>(T data) where T : class
        {
            Emit(null, data);
        }

        public async Task EmitAsync<T>(T data) where T : class
        {
            await EmitAsync(null, data);
        }


        public void EmitMessage<T>(IPipelineMessage<T> message) where T : class
        {
            //DispatchToMessageTaps(message);

            // Blind broadcast ATM
            RouteMessage(message);
        }

        public async Task EmitMessageAsync<T>(IPipelineMessage<T> message) where T : class
        {
            //DispatchToMessageTaps(message);

            // Blind broadcast ATM
            await RouteMessage(message);
        }

        public void Dispose()
        {
            // Go through all queue threads and order instant death
            // Dispose of things
        }

        public void Terminate()
        {
            
        }

        public void Terminate(PipelineComponent source)
        {
            throw new NotImplementedException();
        }

        #region - Internal Inteface -

        internal void OnAttach(Action<Pipeline> action)
        {
            _attachActions.Add(action);
        }

        internal void AttachComponent(PipelineComponent component)
        {
            _components.Add(component);
        }


        internal void AddCtor<T>(ConstructorStub<T> ctor)
        {
            _ctors.Add(ctor);
        }

        internal void AddCtorManifold<T>(ConstructorManifoldStub<T> ctors)
        {
            _ctors.Add(ctors);
        }

        internal void AddInvocation(InvocationStub invocation)
        {
            _invocations.Add(invocation);
        }

        internal void AddTx(TransmitterStub tx, PipelineComponent component)
        { 
            _transmitters.Add(tx);
        }

        internal void AddRx(ReceiverStub rx, PipelineComponent component)
        {
            _receivers.Add(rx);
        }

        internal void AddDisposable(IDisposable disposable)
        {
            Console.WriteLine("*** Fix pipeline disposables ***");
            //throw new NotImplementedException();
        }

        internal void AddTubes(List<Conduit> tubes)
        {
            _tubes.AddRange(tubes);
        }


        internal void Emit<T>(PipelineComponent sender, T data) where T : class
        {
            var message = new PipelineMessage<T>(this, sender, data);
            EmitMessage(message);
        }

        internal async Task EmitAsync<T>(PipelineComponent sender, T data) where T : class
        {
            var message = new PipelineMessage<T>(this, sender, data);
            await EmitMessageAsync(message);
        }


        #endregion

        #region - Declaration -

        // ReSharper disable once MemberCanBeProtected.Global
        public static Func<T> Constructor<T>()
        {
            return default(Func<T>);
        }

        // ReSharper disable once MemberCanBeProtected.Global
        public static Stub Component
        {
            get { return default(Stub); }
        }

        #endregion




        
    }
}