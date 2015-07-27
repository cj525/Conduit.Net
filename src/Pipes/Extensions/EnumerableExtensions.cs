using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Pipes.Extensions
{
    public static class EnumerableExtensions
    {
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T[] Apply<T>(this IEnumerable<T> source, Action<T> action)
        {
            // Prevent multiple applies as side-effect to multiple enumeration 
            var array = source.ToArray();

            foreach (var item in array)
                action(item);

            return array;
        }


        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static IEnumerable<T> If<T>(this IEnumerable<T> source, bool test, Func<IEnumerable<T>, IEnumerable<T>> apply)
        {
            return test ? apply(source) : source;
        }
    }
}
