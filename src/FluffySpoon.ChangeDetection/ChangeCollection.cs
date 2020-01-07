using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace FluffySpoon.ChangeDetection
{
    class ChangeCollection : IChangeCollection, ICollection<Change>
    {
        private readonly HashSet<Change> _changes;

        public int Count => _changes.Count;

        public bool IsReadOnly => false;

        public ChangeCollection()
        {
            _changes = new HashSet<Change>();
        }

        public ChangeCollection(
            IEnumerable<Change> changes) : this()
        {
            foreach (var change in changes)
                Add(change);
        }

        public IEnumerator<Change> GetEnumerator()
        {
            return _changes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(Change item)
        {
            _changes.Add(item);
        }

        public void Clear()
        {
            _changes.Clear();
        }

        public bool Contains(Change item)
        {
            return _changes.Contains(item);
        }

        public void CopyTo(Change[] array, int arrayIndex)
        {
            _changes.CopyTo(array, arrayIndex);
        }

        public bool Remove(Change item)
        {
            return _changes.Remove(item);
        }

        public bool HasChangeFor<T>(Expression<Func<T, object>> expression)
        {
            return _changes.Any(x => x.Matches(expression));
        }

        public bool HasChangeFor(string propertyPath)
        {
            return _changes.Any(x => x.Matches(propertyPath));
        }

        public bool HasChangeWithin<T>(Expression<Func<T, object>> expression)
        {
            return _changes.Any(x => x.IsWithin(expression));
        }

        public bool HasChangeWithin(string propertyPath)
        {
            return _changes.Any(x => x.IsWithin(propertyPath));
        }
    }
}
