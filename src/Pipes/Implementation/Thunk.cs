using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Interfaces;
using Pipes.Types;

namespace Pipes.Implementation
{
    internal class Thunk
    {
        internal Target Target;
        internal object Instance;
        //internal int? Index;

        public Thunk AttachTo<TData,TContext>(object[] components) where TContext : OperationContext where TData : class
        {
            if (Target.Instance != null)
            {
                Instance = components.FirstOrDefault(c => c.GetType() == Target.Instance.GetType());
            }
            return new Thunk<TData, TContext>((Thunk<TData, TContext>) this, Instance ?? Target.Instance);
        }
    }
    internal class Thunk<TContext> : Thunk where TContext :OperationContext
    {
        internal virtual Task Invoke(IPipelineMessage<TContext> message)
        {
            throw new NotImplementedException("Thunk failure");
        }
    }

    /*

public static void DoTest( TargetClass var1, int var2 )
    .maxstack 8
    L_0000: nop 
    L_0001: ldarg.0 
    L_0002: ldarg.1 
    L_0003: callvirt instance void ILTesting.TargetClass::TestMethod(int32)
    L_0008: nop 
    L_0009: ret 

public static Task DoTest( TargetClass var1, int var2 )
    .maxstack 2
    .locals init (
        [0] class [mscorlib]System.Threading.Tasks.Task task)
    L_0000: nop 
    L_0001: ldarg.0 
    L_0002: ldarg.1 
    L_0003: callvirt instance class [mscorlib]System.Threading.Tasks.Task ILTesting.TargetClass::TestMethod(int32)
    L_0008: stloc.0 
    L_0009: br.s L_000b
    L_000b: ldloc.0 
    L_000c: ret 

var result = Target.EmptyTask
    L_0009: ldsfld class [mscorlib]System.Threading.Tasks.Task ILTesting.Target::EmptyTask
    L_000e: stloc.0 
    L_000f: br.s L_0011
    L_0011: ldloc.0 


    */


    internal class Thunk<TData,TContext> : Thunk<TContext> where TContext : OperationContext where TData : class
    {
        private Func<object, IPipelineMessage<TData, TContext>, TData, Task> _delegate;

        internal Thunk(Thunk<TData, TContext> thunk, object instance)
        {
            _delegate = thunk._delegate;
            Instance = instance;
        }

        internal override Task Invoke(IPipelineMessage<TContext> message)
        {
            var msg = (IPipelineMessage<TData, TContext>) message;
            return _delegate(Instance, msg, msg.Data);
        }

        internal Thunk(Target target)
        {
            if (target.MethodInfo == null)
                throw new ArgumentNullException(nameof(target.MethodInfo));

            if (target.MethodInfo.DeclaringType == null)
                throw new ArgumentNullException(nameof(target.MethodInfo.DeclaringType));

            Target = target;

            //var targetInstance = FormatterServices.GetSafeUninitializedObject(target.MethodInfo.DeclaringType);
            //if (target.IsTrigger)
            //{
            //    if (target.ReturnsTask)
            //    {
            //        var d = (TriggerAsync) Delegate.CreateDelegate(typeof (TriggerAsync), targetInstance, target.MethodInfo);
            //        _delegate = (instance, message, data) => d(instance);
            //    }
            //    else
            //    {
            //        var d = (Trigger) Delegate.CreateDelegate(typeof (Trigger), targetInstance, target.MethodInfo);
            //        _delegate = (instance, message, data) =>
            //        {
            //            d(instance);
            //            return Target.EmptyTask;
            //        };
            //    }
            //}
            //else if (target.IsUnwrapped)
            //{
            //    if (target.ReturnsTask)
            //    {
            //        var d = (UnwrappedAsync)Delegate.CreateDelegate(typeof(TriggerAsync), targetInstance, target.MethodInfo);
            //        _delegate = (instance, message, data) => d(instance, data);
            //    }
            //    else
            //    {
            //        var d = (Unwrapped)Delegate.CreateDelegate(typeof(Trigger), targetInstance, target.MethodInfo);
            //        _delegate = (instance, message, data) =>
            //        {
            //            d(instance, data);
            //            return Target.EmptyTask;
            //        };
            //    }

            //}
            //else // Message
            //{
            //    if (target.ReturnsTask)
            //    {
            //        var d = (WrappedAsync)Delegate.CreateDelegate(typeof(TriggerAsync), targetInstance, target.MethodInfo);
            //        _delegate = (instance, message, data) => d(instance, message);
            //    }
            //    else
            //    {
            //        var d = (Wrapped)Delegate.CreateDelegate(typeof(Trigger), targetInstance, target.MethodInfo);
            //        _delegate = (instance, message, data) =>
            //        {
            //            d(instance, message);
            //            return Target.EmptyTask;
            //        };
            //    }
            //}
            //return;

            // Localize all the needed information
            // What type of data
            var dataType = typeof(TData);
            //var parameterType = target.ParameterType;

            // What type of context
            var contextType = typeof(TContext);

            // Data+Context gives message type
            var messageType = typeof(IPipelineMessage<,>).MakeGenericType(dataType, contextType);

            // The method we will call
            var methodInfo = target.MethodInfo;

            // The type of the instance to call said method
            var rxType = methodInfo.DeclaringType;

            // This will probably never happen
            if (rxType == null)
                throw new NotSupportedException("Target must have a declaring type!");

            // Name is MyNamespace.MyComponent.MyReceiver.DataType (yes, it's legal to subname a method)
            var name = $"{rxType.Namespace}.{rxType.Name}.{methodInfo.Name}.{dataType.Name}";
            //var name = String.Empty;

            // The arguments, as available in slots 0, 1, and 2, respectively
            var argTypes = new[] { typeof(object), messageType, dataType };

            // Create a dynamic method which is associated with the receiver (for access) and skips access validation
            var dm = new DynamicMethod(name, typeof(Task), argTypes, rxType, true);

            // Generate IL for this method
            var il = dm.GetILGenerator();

            // Load object for [obj].Method
            if (!methodInfo.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
            }
            var methodParameters = methodInfo.GetParameters();
            // Load parameters
            if (!target.IsTrigger)
            {
                var parameterType = methodParameters[0].ParameterType;
                // If unwrapped, ship data, otherwise ship the message
                if (target.IsUnwrapped)
                {
                    il.Emit(OpCodes.Ldarg_2);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_1);
                    //parameterType = parameterType.GenericTypeArguments[0];
                }
                //il.Emit(target.IsUnwrapped ? OpCodes.Ldarg_2 : OpCodes.Ldarg_1);
                il.Emit(OpCodes.Castclass, parameterType);
            }

            // Call method
            if (methodInfo.IsStatic)
            {
                il.Emit(OpCodes.Call, methodInfo);
                //if (methodParameters.Any())
                //{
                //    il.EmitCall(OpCodes.Call, methodInfo, methodParameters.Select(p => p.ParameterType).ToArray());
                //}
                //else
                //{
                //    il.EmitCall(OpCodes.Call, methodInfo).ToArray());
                //}
            }
            else
            {
                il.Emit(OpCodes.Callvirt, methodInfo);
            }

            // Need a fake task?
            if (!target.ReturnsTask)
            {
                il.Emit(OpCodes.Ldsfld, Target.EmptyTaskField);
            }

            // Done, return
            il.Emit(OpCodes.Ret);

            // Store the delegate to be called as needed
            _delegate = (Func<object, IPipelineMessage<TData, TContext>, TData, Task>)dm.CreateDelegate(typeof(Func<object, IPipelineMessage<TData, TContext>, TData, Task>));
        }
    }
}
