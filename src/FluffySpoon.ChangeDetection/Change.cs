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
        public string? PropertyPath
        {
            get;
        }

        /// <summary>
        /// The old value.
        /// </summary>
        public object? OldValue
        {
            get; 
        }

        /// <summary>
        /// The new value.
        /// </summary>
        public object? NewValue
        {
            get;
        }

        public static readonly Change Empty = new Change();

        public Change(string? propertyPath, object? oldValue = null, object? newValue = null)
        {
            PropertyPath = propertyPath;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public bool Matches(string propertyPath)
        {
            return PropertyPath == propertyPath;
        }

        public bool Matches<T>(Expression<Func<T, object>> expression)
        {
            return Matches(PropertyPathHelper.GetPropertyPath(expression));
        }

        public static bool operator ==(Change a, Change b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Change a, Change b)
        {
            return !a.Equals(b);
        }

        public override bool Equals(object obj)
        {
            if(!(obj is Change change))
                return false;

            if (OldValue == null)
                return change.OldValue == null;

            if (NewValue == null)
                return change.NewValue == null;

            return
                PropertyPath == change.PropertyPath &&
                OldValue.Equals(change.OldValue) &&
                NewValue.Equals(change.NewValue);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = PropertyPath.GetHashCode();
                hashCode = (hashCode * 397) ^ (OldValue != null ? OldValue.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (NewValue != null ? NewValue.GetHashCode() : 0);
                return hashCode;
            }
        }

        public bool Equals(Change other)
        {
            return PropertyPath == other.PropertyPath && 
                   Equals(OldValue, other.OldValue) && 
                   Equals(NewValue, other.NewValue);
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