using System;
using System.Collections.Generic;

namespace Com.Core
{
    public static class CollectionExtensions
    {
        /// <summary>
        /// 关于点对应列表的集合，如果键已存在，将对象加入对应的列表，否则建立新列表
        /// </summary>
        /// <typeparam name="P">键类型</typeparam>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="self">字典为键对应对象列表</param>
        /// <param name="p">列表对应的键</param>
        /// <param name="obj">要加入列表的对象</param>
        public static void CollectInListSet<P, T>(this Dictionary<P, List<T>> self, P p, T obj)
        {
            if (self.TryGetValue(p, out List<T> ts)) { ts.Add(obj); }
            else { self.Add(p, new List<T> { obj }); }
        }

        public static T Tail<T>(this IList<T> self) => self[self.Count - 1];

        /// <summary>
        /// 连续加入多个元素。会产生额外的 GC，不推荐用在频繁调用的部分
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="param"></param>
        public static void Add<T>(this List<T> self, params T[] param)
        {
            for (int i = 0; i < param.Length; i++) { self.Add(param[i]); }
        }

        public static int FindIndexFirst<T>(this IList<T> collection, in T value) where T : IEquatable<T>
        {
            for (int i = 0; i < collection.Count; i++)
            {
                if (collection[i].Equals(value)) { return i; }
            }
            return -1;
        }

        /// <summary>
        /// 判断两个列表的内容是否对应同一个环，O(n^2)
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="vs_1"></param>
        /// <param name="vs_2"></param>
        /// <param name="equals"></param>
        /// <returns></returns>
        public static bool EqualsRing<T1, T2>(IList<T1> vs_1, IList<T2> vs_2, Func<T1, T2, bool> equals)
        {
            int len = vs_1.Count;
            if (vs_2.Count != len || len < 3) { return false; }

            for (int i = 0; i < len; ++i)
            {
                if (CheckEqualsRing(vs_1, vs_2, len, i, equals))
                {
                    return true;
                }
            }
            return false;
        }
        static bool CheckEqualsRing<T1, T2>(IList<T1> vs_1, IList<T2> vs_2,
            int len, int offset_2, Func<T1, T2, bool> equals)
        {
            for (int i = 0; i < len; ++i)
            {
                if (!equals(vs_1[i], vs_2[(i + offset_2) % len])) { return false; }
            }
            return true;
        }

        /// <summary>
        /// 判断两个列表的内容是否对应同一个环，O(n^2)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="vs_1"></param>
        /// <param name="vs_2"></param>
        /// <returns></returns>
        public static bool EqualsRing<T>(IList<T> vs_1, IList<T> vs_2)
            where T : IEquatable<T>
        {
            return EqualsRing(vs_1, vs_2, Internal_Equals);
        }
        static bool Internal_Equals<T>(T a, T b) where T : IEquatable<T> => a.Equals(b);
    }
}