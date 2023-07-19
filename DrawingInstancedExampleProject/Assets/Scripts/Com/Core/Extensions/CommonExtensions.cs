using System.Runtime.CompilerServices;

namespace Com.Core
{
    public static class CommonExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Assign<T>(this T self, out T value)
        {
            value = self;
            return self;
        }
    }
}