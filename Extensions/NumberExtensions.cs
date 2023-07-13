using System;

namespace EverLoader.Extensions
{
    public static class NumberExtensions
    {
        public static string ToSize(this long bytes)
        {
            var unit = 1000;
            if (bytes < unit) { return $"{bytes} B"; }

            var exp = (int)(Math.Log(bytes) / Math.Log(unit));
            return $"{bytes / Math.Pow(unit, exp):F2} {("KMGTPE")[exp - 1]}B";
        }
    }
}
