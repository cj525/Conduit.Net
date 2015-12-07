using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Pipes.Extensions
{
    public static class EnumerableExtensions
    {
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] Apply<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source == null)
                return default(T[]);

            // Prevent multiple applies as side-effect to multiple enumeration 
            var array = source.ToArray();

            foreach (var item in array)
                action(item);

            return array;
        }

        //[DebuggerStepThrough]
        //public static void ApplyAndWait<T>(this IEnumerable<T> source, Func<T, Task> action)
        //{
        //    Task.WaitAll(source.Select(action).ToArray());
        //}



        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static IEnumerable<T> If<T>(this IEnumerable<T> source, bool test, Func<IEnumerable<T>, IEnumerable<T>> apply)
        {
            return test ? apply(source) : source;
        }
    }
}
