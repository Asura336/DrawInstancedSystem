using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Com.Core
{
    public static class HashIntArrExtend
    {
        const int E9A7 = 1000000007;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void HashIntArrFor(ref int sum, int item, int index)
        {
            sum ^= ((item % E9A7) << (index % 32)) % E9A7;
        }

        public static int HashIntArr(this IList<int> vs, int count)
        {
            int sum = 0;
            for (int i = 0; i < count; i++)
            {
                HashIntArrFor(ref sum, vs[i], i);
            }
            return sum % E9A7;
        }
        public static int HashIntArr(params int[] vs) => vs.HashIntArr(vs.Length);
        public static int HashIntArr(this IEnumerable<int> seq)
        {
            int index = 0;
            int sum = 0;
            foreach (var c in seq)
            {
                HashIntArrFor(ref sum, c, index++);
            }
            return sum % E9A7;
        }

        public static unsafe int HashIntArr(int* vs, int count)
        {
            int sum = 0;
            for (int i = 0; i < count; i++)
            {
                HashIntArrFor(ref sum, vs[i], i);
            }
            return sum % E9A7;
        }

        public static unsafe int HashIntArr(this in (int, int) turple)
        {
            int* vs = stackalloc int[2];
            vs[0] = turple.Item1;
            vs[1] = turple.Item2;
            return HashIntArr(vs, 2);
        }
        public static unsafe int HashIntArr(this in (int, int, int) turple)
        {
            int* vs = stackalloc int[3];
            vs[0] = turple.Item1;
            vs[1] = turple.Item2;
            vs[2] = turple.Item3;
            return HashIntArr(vs, 3);
        }
        public static unsafe int HashIntArr(this in (int, int, int, int) turple)
        {
            int* vs = stackalloc int[4];
            vs[0] = turple.Item1;
            vs[1] = turple.Item2;
            vs[2] = turple.Item3;
            vs[3] = turple.Item4;
            return HashIntArr(vs, 4);
        }
        public static unsafe int HashIntArr(this in (int, int, int, int, int) turple)
        {
            int* vs = stackalloc int[5];
            vs[0] = turple.Item1;
            vs[1] = turple.Item2;
            vs[2] = turple.Item3;
            vs[3] = turple.Item4;
            vs[4] = turple.Item5;
            return HashIntArr(vs, 5);
        }
        public static unsafe int HashIntArr(this in (int, int, int, int, int, int) turple)
        {
            int* vs = stackalloc int[6];
            vs[0] = turple.Item1;
            vs[1] = turple.Item2;
            vs[2] = turple.Item3;
            vs[3] = turple.Item4;
            vs[4] = turple.Item5;
            vs[5] = turple.Item6;
            return HashIntArr(vs, 6);
        }
        public static unsafe int HashIntArr(this in (int, int, int, int, int, int, int) turple)
        {
            int* vs = stackalloc int[7];
            vs[0] = turple.Item1;
            vs[1] = turple.Item2;
            vs[2] = turple.Item3;
            vs[3] = turple.Item4;
            vs[4] = turple.Item5;
            vs[5] = turple.Item6;
            vs[6] = turple.Item7;
            return HashIntArr(vs, 7);
        }
        public static unsafe int HashIntArr(this in (int, int, int, int, int, int, int, int) turple)
        {
            int* vs = stackalloc int[8];
            vs[0] = turple.Item1;
            vs[1] = turple.Item2;
            vs[2] = turple.Item3;
            vs[3] = turple.Item4;
            vs[4] = turple.Item5;
            vs[5] = turple.Item6;
            vs[6] = turple.Item7;
            vs[7] = turple.Item8;
            return HashIntArr(vs, 8);
        }
    }
}