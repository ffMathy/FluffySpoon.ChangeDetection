﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FluffySpoon.ChangeDetection
{
    public class ChangeDetector
    {
        public static Change GetChange(object oldObject, object newObject)
        {
            var type = GetTypeFromObjects(oldObject, newObject);
            if (!IsSimpleType(type))
            {
                throw new InvalidOperationException(
                    $"Can't detect simple changes from complex objects ({type.FullName}). {nameof(GetChange)} is made for simple types. Use {nameof(GetChanges)} instead.");
            }

            return GetChanges(oldObject, newObject).SingleOrDefault();
        }

        public static Change GetChange<T>(T oldObject, T newObject, Expression<Func<T, object>> expression = null)
        {
            var a = GetValueOfExpressionFor(oldObject, expression);
            var b = GetValueOfExpressionFor(newObject, expression);

            return GetChange(a, b);
        }

        public static IChangeCollection GetChanges(object oldObject, object newObject)
        {
            using (var context = new ContextPair(oldObject, newObject))
                return GetRecursiveChanges(context, null);
        }

        public static IChangeCollection GetChanges<T>(T oldObject, T newObject, Expression<Func<T, object>> expression = null)
        {
            var a = GetValueOfExpressionFor(oldObject, expression);
            var b = GetValueOfExpressionFor(newObject, expression);

            var propertyPath = PropertyPathHelper.GetPropertyPath(expression);

            using (var context = new ContextPair(a, b))
                return GetRecursiveChanges(context, propertyPath);
        }

        public static bool HasChanges(object oldObject, object newObject)
        {
            return DoesEnumerableHaveContents(
                GetChanges(oldObject, newObject));
        }

        public static bool HasChanges<T>(T oldObject, T newObject, Expression<Func<T, object>> expression = null)
        {
            return DoesEnumerableHaveContents(
                GetChanges(oldObject, newObject, expression));
        }

        private static bool DoesEnumerableHaveContents(IEnumerable<Change> changes)
        {
            return changes.GetEnumerator().MoveNext();
        }

        private static IChangeCollection GetRecursiveChanges(ContextPair contextPair, string basePropertyPath)
        {
            var a = contextPair.A.Instance;
            var b = contextPair.B.Instance;

            var type = GetTypeFromObjects(a, b);
            if (type == null)
                return new ChangeCollection();

            if (IsSimpleType(type))
            {
                var change = GetShallowChange(basePropertyPath, a, b);
                if (change == Change.Empty)
                    return new ChangeCollection();

                return new ChangeCollection(new[]
                {
                    change
                });
            }

            var seenObjectsA = contextPair.A.SeenObjects;
            var seenObjectsB = contextPair.B.SeenObjects;

            seenObjectsA.Add(a);
            seenObjectsB.Add(b);

            var objectPathQueue = new Queue<ObjectPath>();
            EnqueueObjectPathQueues(objectPathQueue, basePropertyPath, a, b);

            var result = new ChangeCollection();

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

                    if(objectPath.OldInstance != null)
                        seenObjectsA.Add(objectPath.OldInstance);

                    if (objectPath.NewInstance != null)
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
                    OldInstance = a == null ? null : property.GetValue(a),
                    NewInstance = b == null ? null : property.GetValue(b),
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
                   type.IsValueType ||
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
