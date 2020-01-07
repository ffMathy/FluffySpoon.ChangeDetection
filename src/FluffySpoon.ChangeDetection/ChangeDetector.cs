using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FluffySpoon.ChangeDetection
{
    public class ChangeDetector
    {
        public static IEnumerable<Change> GetChangesRecursively(object oldObject, object newObject)
        {
            using (var context = new ContextPair(oldObject, newObject))
                return GetRecursiveChanges(context, null);
        }

        public static IEnumerable<Change> GetChangesRecursively<T>(T oldObject, T newObject, Expression<Func<T, object>> expression = null)
        {
            var a = GetValueOfExpressionFor(oldObject, expression);
            var b = GetValueOfExpressionFor(newObject, expression);

            var propertyPath = PropertyPathHelper.GetPropertyPath(expression);

            using (var context = new ContextPair(a, b))
                return GetRecursiveChanges(context, propertyPath);
        }

        public static bool HasChangedRecursively(object oldObject, object newObject)
        {
            return DoesEnumerableHaveContents(
                GetChangesRecursively(oldObject, newObject));
        }

        public static bool HasChangedRecursively<T>(T oldObject, T newObject, Expression<Func<T, object>> expression = null)
        {
            return DoesEnumerableHaveContents(
                GetChangesRecursively(oldObject, newObject, expression));
        }

        private static bool DoesEnumerableHaveContents(IEnumerable<Change> changes)
        {
            return changes.GetEnumerator().MoveNext();
        }

        private static IEnumerable<Change> GetRecursiveChanges(ContextPair contextPair, string basePropertyPath)
        {
            var a = contextPair.A.Instance;
            var b = contextPair.B.Instance;

            var type = GetTypeFromObjects(a, b);
            if (type == null)
                return Array.Empty<Change>();

            if (IsSimpleType(type))
            {
                var change = GetShallowChange(basePropertyPath, a, b);
                if (change == Change.Empty)
                    return Array.Empty<Change>();

                return new[]
                {
                    change
                };
            }

            var seenObjectsA = contextPair.A.SeenObjects;
            var seenObjectsB = contextPair.B.SeenObjects;

            seenObjectsA.Add(a);
            seenObjectsB.Add(b);

            var objectPathQueue = new Queue<ObjectPath>();
            EnqueueObjectPathQueues(objectPathQueue, basePropertyPath, a, b);

            var result = new HashSet<Change>();

            while (objectPathQueue.Count > 0)
            {
                var objectPath = objectPathQueue.Dequeue();

                var objectPathType = GetTypeFromObjects(
                    objectPath.OldInstance,
                    objectPath.NewInstance);
                if (objectPathType == null)
                    continue;

                if (IsSimpleType(objectPathType))
                {
                    var shallowChange = GetShallowChange(
                        objectPath.BasePropertyPath,
                        objectPath.OldInstance,
                        objectPath.NewInstance);
                    if (shallowChange != Change.Empty)
                        result.Add(shallowChange);
                }
                else
                {
                    if (seenObjectsA.Contains(objectPath.OldInstance) || seenObjectsB.Contains(objectPath.NewInstance))
                        continue;

                    seenObjectsA.Add(objectPath.OldInstance);
                    seenObjectsB.Add(objectPath.NewInstance);

                    EnqueueObjectPathQueues(
                        objectPathQueue,
                        objectPath.BasePropertyPath,
                        objectPath.OldInstance,
                        objectPath.NewInstance);
                }
            }

            return result;
        }

        private static void EnqueueObjectPathQueues(
            Queue<ObjectPath> objectPathQueue,
            string basePropertyPath,
            object a,
            object b)
        {
            var type = GetTypeFromObjects(a, b);
            if (type == null)
                throw new InvalidOperationException("The type could not be determined.");

            var properties = type.GetProperties();
            foreach (var property in properties)
            {
                objectPathQueue.Enqueue(new ObjectPath()
                {
                    OldInstance = property.GetValue(a),
                    NewInstance = property.GetValue(b),
                    Properties = property.PropertyType.GetProperties(),
                    BasePropertyPath = !string.IsNullOrEmpty(basePropertyPath) ?
                        basePropertyPath + "." + property.Name :
                        property.Name
                });
            }
        }

        private static Type GetTypeFromObjects(object a, object b)
        {
            var type = a?.GetType() ?? b?.GetType();
            return type;
        }

        private static Change GetShallowChange(string parentPropertyPath, object oldValue, object newValue)
        {
            if (oldValue == null && newValue == null)
                return Change.Empty;

            if (oldValue == null)
                return new Change(parentPropertyPath, null, newValue);

            if (newValue == null)
                return new Change(parentPropertyPath, oldValue, null);

            var hasChanged = !oldValue.Equals(newValue);
            return hasChanged ?
                new Change(parentPropertyPath, oldValue, newValue) :
                Change.Empty;
        }

        private static bool IsSimpleType(Type type)
        {
            return type.IsPrimitive ||
                type == typeof(string);
        }

        private static object GetValueOfExpressionFor<T>(T obj, Expression<Func<T, object>> expression = null)
        {
            if (expression == null)
                return obj;

            var compiledExpression = expression.Compile();
            return compiledExpression(obj);
        }

        private class ContextPair : IDisposable
        {
            public Context A
            {
                get;
            }

            public Context B
            {
                get;
            }

            public ContextPair(object a, object b)
            {
                A = new Context(a);
                B = new Context(b);
            }

            public void Dispose()
            {
                A.Dispose();
                B.Dispose();
            }
        }

        private class Context : IDisposable
        {
            public object Instance
            {
                get;
            }

            public ICollection<object> SeenObjects
            {
                get;
            }

            public Context(object instance)
            {
                Instance = instance;

                SeenObjects = new HashSet<object>();
            }

            public void Dispose()
            {
                SeenObjects.Clear();
            }
        }

        private class ObjectPath
        {
            public object OldInstance
            {
                get; set;
            }

            public object NewInstance
            {
                get; set;
            }

            public PropertyInfo[] Properties
            {
                get; set;
            }

            public string BasePropertyPath
            {
                get; set;
            }
        }
    }
}
