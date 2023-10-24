using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProNav
{
    /// <summary>
    /// Wraps <see cref="List{T}"/> with locks for thread-safe-ish useage.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LockedList<T> : IEnumerable<T>
    {
        private List<T> _list;

        public LockedList()
        {
            _list = new List<T>();
        }

        public LockedList(List<T> values)
        {
            _list = new List<T>(values);
        }

        public void Add(T value)
        {
            lock (_list)
            {
                _list.Add(value);
            }
        }

        public void ForEach(Action<T> action)
        {
            lock (_list)
            {
                for (int i = 0; i < _list.Count; i++)
                {
                    action(_list[i]);
                }
            }
        }

        public bool Remove(T item)
        {
            lock (_list)
            {
                return _list.Remove(item);
            }
        }

        public void AddRange(IEnumerable<T> values)
        {
            lock (_list)
            {
                _list.AddRange(values);
            }
        }

        public int Count
        {
            get
            {
                lock (_list)
                {
                    return _list.Count;
                }
            }
        }

        public T this[int index]
        {
            get
            {
                lock (_list)
                {
                    return _list[index];
                }
            }

            set
            {
                lock (_list)
                {
                    _list[index] = value;
                }
            }
        }

        public void Clear()
        {
            lock (_list)
            {
                _list.Clear();
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock (_list)
            {
                return _list.GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (_list)
            {
                return _list.GetEnumerator();
            }
        }
    }
}
