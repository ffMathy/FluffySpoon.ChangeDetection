using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace FluffySpoon.ChangeDetection
{
    class ChangeCollection<T> : ChangeCollection, IChangeCollection<T>
    {
        public bool HasChangeFor(Expression<Func<T, object>> expression)
        {
            return Changes.Any(x => x.Matches(expression));
        }
    }

    class ChangeCollection : IChangeCollection, ICollection<Change>
    {
        protected readonly HashSet<Change> Changes;

        public int Count => Changes.Count;

        public bool IsReadOnly => false;

        public ChangeCollection()
        {
            Changes = new HashSet<Change>();
        }

        public ChangeCollection(
            IEnumerable<Change> changes) : this()
        {
            foreach (var change in changes)
                Add(change);
        }

        public IEnumerator<Change> GetEnumerator()
        {
            return Changes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(Change item)
        {
            Changes.Add(item);
        }

        public void Clear()
        {
            Changes.Clear();
        }

        public bool Contains(Change item)
        {
            return Changes.Contains(item);
        }

        public void CopyTo(Change[] array, int arrayIndex)
        {
            Changes.CopyTo(array, arrayIndex);
        }

        public bool Remove(Change item)
        {
            return Changes.Remove(item);
        }

        public bool HasChangeFor(string propertyPath)
        {
            return Changes.Any(x => x.Matches(propertyPath));
        }
    }
}
