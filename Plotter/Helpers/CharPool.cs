using System.Collections.Concurrent;

namespace Plotter
{
    public static class CharPool
    {
        private static readonly ConcurrentBag<char[]> _pool = [];
        private const int InitialBagSize = 8192;
        private const int BufferSize = 128;

        static CharPool()
        {
            for (int i = 0; i < InitialBagSize; i++)
                _pool.Add(new char[BufferSize]);
        }

        internal static char[] Rent()
            => _pool.TryTake(out var buffer) ? buffer : new char[BufferSize];
        

        internal static void Return(char[] buffer)
            => _pool.Add(buffer);
    }
}