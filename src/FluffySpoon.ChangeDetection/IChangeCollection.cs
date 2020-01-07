using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace FluffySpoon.ChangeDetection
{
    public interface IChangeCollection : IReadOnlyCollection<Change>
    {
        bool HasChangeFor<T>(Expression<Func<T, object>> expression);
        bool HasChangeFor(string propertyPath);
        bool HasChangeWithin<T>(Expression<Func<T, object>> expression);
        bool HasChangeWithin(string propertyPath);
    }
}