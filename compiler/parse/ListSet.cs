using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.parse
{
    /// <summary>
    /// 按加入顺序排序的Set
    /// </summary>
    public class ListSet<T> : ICollection<T>, IList<T>
    {
        private readonly HashSet<T> dict = new HashSet<T>();
        private readonly List<T> list = new List<T>();

        public T this[int index]
        {
            get { return list[index]; }
            set { throw new NotImplementedException(); }
        }

        public int Count
        {
            get { return list.Count; }
        }

        public bool IsReadOnly { get { return false; } }

        public void Add(T item)
        {
            if (dict.Contains(item))
                return;

            dict.Add(item);
            list.Add(item);
        }

        public void Clear()
        {
            dict.Clear();
            list.Clear();
        }

        public bool Contains(T item)
        {
            return dict.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
