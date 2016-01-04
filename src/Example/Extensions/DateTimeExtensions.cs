using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipes.Example.Extensions
{
    static class DateTimeExtensions
    {
        private static DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long ToEpoch(this DateTime dateTime)
        {
            return Convert.ToInt64((dateTime.ToUniversalTime() - _epoch).TotalSeconds);
        }

        public static DateTime EpocToDateTime(this long value)
        {
            return _epoch.AddSeconds(value);
        }
    }
}
