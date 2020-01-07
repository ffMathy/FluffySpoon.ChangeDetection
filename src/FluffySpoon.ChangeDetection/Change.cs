using System;
using System.Linq.Expressions;

namespace FluffySpoon.ChangeDetection
{
    public struct Change
    {
        /// <summary>
        /// The path of the property that changed.
        /// <example><see cref="string.Empty"/> for simple values.</example>
        /// <example>"MyComplexObject.MyComplexSubObject.MySimpleValue" when comparing two MyComplexObject instances.</example>
        /// </summary>
        public string PropertyPath
        {
            get;
        }

        /// <summary>
        /// The old value.
        /// </summary>
        public object OldValue
        {
            get; 
        }

        /// <summary>
        /// The new value.
        /// </summary>
        public object NewValue
        {
            get;
        }

        public static readonly Change Empty;

        public Change(string propertyPath) : this(propertyPath, null, null)
        {
        }

        public Change(string propertyPath, object oldValue, object newValue)
        {
            PropertyPath = propertyPath;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public bool Matches<T>(Expression<Func<T, object>> expression)
        {
            return PropertyPath == PropertyPathHelper.GetPropertyPath(expression);
        }

        public static bool operator ==(Change a, Change b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Change a, Change b)
        {
            return !a.Equals(b);
        }

        public override string ToString()
        {
            var result = string.Empty;

            if (!string.IsNullOrEmpty(PropertyPath))
                result += PropertyPath + ": ";

            result += $"{{{OldValue}}} -> {{{NewValue}}}";

            return result;
        }
    }
}