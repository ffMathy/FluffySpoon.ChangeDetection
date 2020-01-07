using System;
using System.Linq;
using System.Linq.Expressions;

namespace FluffySpoon.ChangeDetection
{
    internal static class PropertyPathHelper
    {
        public static string GetPropertyPath<T>(Expression<Func<T, object>> expression)
        {
            if (expression == null)
                return string.Empty;

            var visitor = new PropertyPathExpressionVisitor();
            visitor.Visit(expression);

            return string.Join(".", visitor.Path.Select(p => p.Name));
        }
    }
}