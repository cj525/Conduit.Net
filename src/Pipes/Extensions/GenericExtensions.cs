using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Pipes.Extensions
{
    public static class GenericExtensions
    {
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ApplyOver<T>(this T target, IEnumerable<Action<T>> source)
        {
            source.Apply(item => item(target));
        }
    }
}
