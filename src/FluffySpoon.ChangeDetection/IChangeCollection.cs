using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace FluffySpoon.ChangeDetection
{
    public interface IChangeCollection<T> : IChangeCollection
    {
        bool HasChangeFor(Expression<Func<T, object?>> expression);
    }

    public interface IChangeCollection : IReadOnlyCollection<Change>
    {
        bool HasChangeFor(string propertyPath);
    }
}
