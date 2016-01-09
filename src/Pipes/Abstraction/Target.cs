using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Pipes.Abstraction
{
    internal abstract class Target
    {
        // Give empty task a string so that future developers aren't wondering where false came from.
        internal const string EmptyTaskResult = "Async Pipeline Target :Bridge: Synchronous Task Completed";

        // ReSharper disable once StaticMemberInGenericType // This guy's relentless
        internal static readonly Task EmptyTask = Task.FromResult(EmptyTaskResult);
        internal static FieldInfo EmptyTaskField = typeof (Target).GetField("EmptyTask", BindingFlags.NonPublic | BindingFlags.Static);

        internal MethodInfo MethodInfo;
        internal bool ReturnsTask;
        internal bool IsUnwrapped;
        internal bool IsTrigger;
        //internal Type ParameterType;
        internal object Instance;
    }
}
