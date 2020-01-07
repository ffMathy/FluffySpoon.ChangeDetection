using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace FluffySpoon.ChangeDetection
{
    public class ChangeDetector
    {
        public static bool HasChangedRecursively(object objectA, object objectB)
        {
            using (var context = new ContextPair(objectA, objectB))
                return HaveObjectsChangedRecursively(context);
        }

        public static bool HasChangedRecursively<T>(T objectA, T objectB, Expression<Func<T, object>> expression = null)
        {
            var a = GetValueOfExpressionFor(objectA, expression);
            var b = GetValueOfExpressionFor(objectB, expression);

            return HasChangedRecursively(a, b);
        }

        private static bool HaveObjectsChangedRecursively(ContextPair contextPair)
        {
            var a = contextPair.A.Instance;
            var b = contextPair.B.Instance;

            var type = GetTypeFromObjects(a, b);
            if (type == null)
                return false;

            if (IsSimpleType(type))
                return HasChangedShallow(a, b);

            var seenObjectsA = contextPair.A.SeenObjects;
            var seenObjectsB = contextPair.B.SeenObjects;

            seenObjectsA.Add(a);
            seenObjectsB.Add(b);

            var objectPathQueue = new Queue<ObjectPath>();
            EnqueueObjectPathQueues(objectPathQueue, a, b);

            while (objectPathQueue.Count > 0)
            {
                var objectPath = objectPathQueue.Dequeue();

                var objectPathType = GetTypeFromObjects(
                    objectPath.InstanceA,
                    objectPath.InstanceB);
                if (objectPathType == null)
                    continue;

                if (IsSimpleType(objectPathType))
                {
                    var hasChangedShallow = HasChangedShallow(
                        objectPath.InstanceA,
                        objectPath.InstanceB);
                    if (hasChangedShallow)
                        return true;
                }
                else
                {
                    if (seenObjectsA.Contains(objectPath.InstanceA) || seenObjectsB.Contains(objectPath.InstanceB))
                        continue;

                    seenObjectsA.Add(objectPath.InstanceA);
                    seenObjectsB.Add(objectPath.InstanceB);

                    EnqueueObjectPathQueues(
                        objectPathQueue,
                        objectPath.InstanceA,
                        objectPath.InstanceB);
                }
            }

            return false;
        }

        private static void EnqueueObjectPathQueues(Queue<ObjectPath> objectPathQueue, object a, object b)
        {
            var type = GetTypeFromObjects(a, b);
            if (type == null)
                throw new InvalidOperationException("The type could not be determined.");

            var properties = type.GetProperties();
            foreach (var property in properties)
            {
                objectPathQueue.Enqueue(new ObjectPath()
                {
                    InstanceA = property.GetValue(a),
                    InstanceB = property.GetValue(b),
                    Properties = property.PropertyType.GetProperties()
                });
            }
        }

        private static Type GetTypeFromObjects(object a, object b)
        {
            var type = a?.GetType() ?? b?.GetType();
            return type;
        }

        private static bool HasChangedShallow(object a, object b)
        {
            if (a == null && b == null)
                return true;

            if (a == null)
                return true;

            if (b == null)
                return true;

            return !a.Equals(b);
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
                get; set;
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
            public object InstanceA
            {
                get; set;
            }

            public object InstanceB
            {
                get; set;
            }

            public PropertyInfo[] Properties
            {
                get; set;
            }
        }
    }
}
