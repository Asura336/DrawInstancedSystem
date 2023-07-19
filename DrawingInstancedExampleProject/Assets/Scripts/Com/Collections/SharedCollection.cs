using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace Com.Collections
{
    /// <summary>
    /// 容器池
    /// </summary>
    /// <typeparam name="T">池中元素的类型</typeparam>
    public static class SharedCollection<T>
    {
        const int CELL_CAPACITY = 2048;

        public sealed class ListCell : IList<T>, IDisposable
        {
            static readonly Stack<ListCell> _recycledLists = new Stack<ListCell>(CELL_CAPACITY);

            readonly List<T> list;
            public List<T> Body => list;
            public static implicit operator List<T>(ListCell lc) => lc.Body;

            bool active;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            System.Diagnostics.StackTrace _st;
#endif

            //static uint count = 0;
            ListCell()
            {
                //Debug.Log($"count of {typeof(T)} = {++count}");

                list = new List<T>(CELL_CAPACITY);
                active = true;
                if (_recycledLists.Count > (1 << 16))
                {
                    Debug.Log($"{list.GetType()} allocated {_recycledLists.Count} entities.");
                    Debug.LogError("可能需要检查泄露");
                    while (_recycledLists.Count != 0)
                    {
                        _recycledLists.Pop();
                    }
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
            }
            ~ListCell()
            {
                Dispose(false);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Profiler.BeginSample("Debug Build Only");
                Debug.Log($"Finalizer -> {typeof(T)}\n{_st}");
                Profiler.EndSample();
#endif
            }

            public static ListCell Create(int initLen = 0, T initValue = default)
            {
                ListCell cell;
                if (_recycledLists.Count == 0)
                {
                    cell = new ListCell();
                }
                else
                {
                    cell = _recycledLists.Pop();
                    cell.active = true;
                }
                for (int i = 0; i < initLen; i++) { cell.Add(initValue); }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Profiler.BeginSample("Debug Build Only");
                cell._st = new System.Diagnostics.StackTrace(true);
                Profiler.EndSample();
#endif
                return cell;
            }

            public static ListCell Create(IEnumerable<T> source)
            {
                var cell = Create();
                foreach (var c in source) { cell.Add(c); }
                return cell;
            }

            public static ListCell CreateUnchecked()
            {
                ListCell cell;
                if (_recycledLists.Count == 0)
                {
                    cell = new ListCell();
                }
                else
                {
                    cell = _recycledLists.Pop();
                    cell.active = true;
                }
                return cell;
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            void Dispose(bool dispose)
            {
                if (active)
                {
                    active = false;
                    list.Clear();
                    if (dispose)
                    {
                        _recycledLists.Push(this);
                    }
                }
            }

            public bool Contains(T item, IEqualityComparer<T> comparer)
            {
                for (int i = 0; i < Body.Count; i++)
                {
                    if (comparer.Equals(Body[i], item)) { return true; }
                }
                return false;
            }

            #region IList
            public T this[int index] { get => ((IList<T>)list)[index]; set => ((IList<T>)list)[index] = value; }

            public int Count => ((IList<T>)list).Count;

            public bool IsReadOnly => ((IList<T>)list).IsReadOnly;

            public void Add(T item)
            {
                ((IList<T>)list).Add(item);
            }

            public void Clear()
            {
                ((IList<T>)list).Clear();
            }

            public bool Contains(T item)
            {
                return ((IList<T>)list).Contains(item);
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                ((IList<T>)list).CopyTo(array, arrayIndex);
            }

            public IEnumerator<T> GetEnumerator()
            {
                return ((IList<T>)list).GetEnumerator();
            }

            public int IndexOf(T item)
            {
                return ((IList<T>)list).IndexOf(item);
            }

            public void Insert(int index, T item)
            {
                ((IList<T>)list).Insert(index, item);
            }

            public bool Remove(T item)
            {
                return ((IList<T>)list).Remove(item);
            }

            public void RemoveAt(int index)
            {
                ((IList<T>)list).RemoveAt(index);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IList<T>)list).GetEnumerator();
            }
            #endregion
        }

        public sealed class CollectionCell<C> : ICollection<T>, IDisposable
            where C : ICollection<T>, new()
        {
            static readonly Stack<CollectionCell<C>> _recycles = new Stack<CollectionCell<C>>();
            readonly C body;
            public C UnWrap => body;
            bool active;

            public static implicit operator C(CollectionCell<C> self) => self.UnWrap;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            System.Diagnostics.StackTrace _st;
#endif

            private CollectionCell()
            {
                body = new C();
                active = true;
                if (_recycles.Count > (1 << 16))
                {
                    Debug.Log($"{body.GetType()} allocated {_recycles.Count} entities.");
                    Debug.LogError("可能需要检查泄露");
                    while (_recycles.Count != 0)
                    {
                        _recycles.Pop();
                    }
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
            }
            ~CollectionCell()
            {
                Dispose(false);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Profiler.BeginSample("Debug Build Only");
                Debug.Log($"Finalizer -> {typeof(T)}\n{_st}");
                Profiler.EndSample();
#endif
            }

            public static CollectionCell<C> Create()
            {
                CollectionCell<C> cell;
                if (_recycles.Count == 0)
                {
                    cell = new CollectionCell<C>();
                }
                else
                {
                    cell = _recycles.Pop();
                    cell.active = true;
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Profiler.BeginSample("Debug Build Only");
                cell._st = new System.Diagnostics.StackTrace(true);
                Profiler.EndSample();
#endif
                return cell;
            }

            public static CollectionCell<C> Create(IEnumerable<T> source)
            {
                var cell = Create();
                foreach (var val in source) { cell.Add(val); }
                return cell;
            }

            public static CollectionCell<C> CreateUnchecked()
            {
                CollectionCell<C> cell;
                if (_recycles.Count == 0)
                {
                    cell = new CollectionCell<C>();
                }
                else
                {
                    cell = _recycles.Pop();
                    cell.active = true;
                }
                return cell;
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            void Dispose(bool disposing)
            {
                if (active)
                {
                    active = false;
                    body.Clear();
                    if (disposing)
                    {
                        _recycles.Push(this);
                    }
                }
            }

            #region ICollection
            public void Add(T item) => body.Add(item);

            public void Clear() => body.Clear();

            public bool Contains(T item) => body.Contains(item);

            public void CopyTo(T[] array, int arrayIndex) => body.CopyTo(array, arrayIndex);

            public bool Remove(T item) => body.Remove(item);

            public int Count => body.Count;

            public bool IsReadOnly => body.IsReadOnly;

            public IEnumerator<T> GetEnumerator() => body.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)body).GetEnumerator();
            #endregion
        }
    }
}