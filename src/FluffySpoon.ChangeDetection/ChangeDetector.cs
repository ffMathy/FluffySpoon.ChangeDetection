using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FluffySpoon.ChangeDetection
{
    public class ChangeDetector
    {
        public static Change GetChange(object? oldObject, object? newObject)
        {
            var type = GetTypeFromObjects(oldObject, newObject);
            if (!IsSimpleType(type))
            {
                throw new InvalidOperationException(
                    $"Can't detect simple changes from complex objects ({type.FullName}). {nameof(GetChange)} is made for simple types. Use {nameof(GetChanges)} instead.");
            }

            return GetChanges(oldObject, newObject).SingleOrDefault();
        }

        public static Change GetChange<T>(T oldObject, T newObject, Expression<Func<T, object>>? expression = null)
        {
            var a = GetValueOfExpressionFor(oldObject, expression);
            var b = GetValueOfExpressionFor(newObject, expression);

            return GetChange(a, b);
        }

        public static IChangeCollection GetChanges(object? oldObject, object? newObject)
        {
            using var context = new ContextPair(oldObject, newObject);
            return GetRecursiveChanges<ChangeCollection>(context, null);
        }

        public static IChangeCollection<T> GetChanges<T>(T oldObject, T newObject, Expression<Func<T, object>>? expression = null)
        {
            var a = GetValueOfExpressionFor(oldObject, expression);
            var b = GetValueOfExpressionFor(newObject, expression);

            var propertyPath = PropertyPathHelper.GetPropertyPath(expression);

            using var context = new ContextPair(a, b);
            return GetRecursiveChanges<ChangeCollection<T>>(context, propertyPath);
        }

        public static bool HasChanges(object oldObject, object newObject)
        {
            return DoesEnumerableHaveContents(
                GetChanges(oldObject, newObject));
        }

        public static bool HasChanges<T>(T oldObject, T newObject, Expression<Func<T, object>>? expression = null)
        {
            return DoesEnumerableHaveContents(
                GetChanges(oldObject, newObject, expression));
        }

        private static bool DoesEnumerableHaveContents(IEnumerable<Change> changes)
        {
            return changes.GetEnumerator().MoveNext();
        }

        private static TCollection GetRecursiveChanges<TCollection>(ContextPair contextPair, string? basePropertyPath) where TCollection : ChangeCollection, new()
        {
            var oldInstance = contextPair.OldInstanceContext.Instance;
            var newInstance = contextPair.NewInstanceContext.Instance;

            var type = GetTypeFromObjects(oldInstance, newInstance);
            if (type == null)
                return new TCollection();

            if (IsSimpleType(type))
            {
                var change = GetShallowChange(basePropertyPath, oldInstance, newInstance);
                return change == Change.Empty ? 
                    new TCollection() : 
                    new TCollection { change };
            }

            var result = new TCollection();

            ScanForChanges(contextPair, new ObjectPath()
            {
                OldInstance = oldInstance,
                NewInstance = newInstance,
                BasePropertyPath = null
            }, result);

            var objectPathQueue = contextPair.ObjectPathQueue;
            while (objectPathQueue.Count > 0)
            {
                var objectPath = objectPathQueue.Dequeue();

                var objectPathType = GetTypeFromObjects(
                    objectPath.OldInstance,
                    objectPath.NewInstance);
                if (objectPathType == null)
                    continue;

                ScanForChanges(contextPair, objectPath, result);
            }

            return result;
        }

        private static bool IsEnumerableType(Type? objectPathType)
        {
            if (objectPathType == null)
                return false;
            
            var isAssignableToEnumerable = typeof(IEnumerable<object>).IsAssignableFrom(objectPathType);
            if (isAssignableToEnumerable)
                return true;

            var interfaces = objectPathType.GetInterfaces();

            var genericInterfaces = interfaces.Where(x => x.IsGenericType);
            return genericInterfaces.Any(genericInterfaceType => genericInterfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }

        private static void ScanForChanges(ContextPair contextPair, ObjectPath objectPath, ChangeCollection result)
        {
            var pathType = GetTypeFromObjects(
                objectPath.OldInstance,
                objectPath.NewInstance);
            if (IsSimpleType(pathType))
            {
                var shallowChange = GetShallowChange(
                    objectPath.BasePropertyPath,
                    objectPath.OldInstance,
                    objectPath.NewInstance);
                if (shallowChange == Change.Empty) 
                    return;
                
                result.Add(shallowChange);
                if (objectPath.ParentChanges == null) 
                    return;
                
                foreach (var parentChange in objectPath.ParentChanges)
                {
                    result.Add(parentChange);
                }
            }
            else if (IsEnumerableType(pathType))
            {
                if (pathType == null)
                    throw new InvalidOperationException("Type was enumerable despite being null.");
                
                Array? itemsOldArray;
                Array? itemsNewArray;

                if (pathType.IsArray)
                {
                    itemsOldArray = (Array?)objectPath.OldInstance;
                    itemsNewArray = (Array?)objectPath.NewInstance;
                }
                else
                {
                    var genericType = pathType
                        .GetInterfaces()
                        .Single(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                        .GetGenericArguments()
                        .Single();

                    var toArrayMethod = typeof(Enumerable)
                        .GetMethods()
                        .Single(x =>
                            x.Name == nameof(Enumerable.ToArray) &&
                            x.IsGenericMethod)
                        .MakeGenericMethod(genericType);

                    var emptyMethod = typeof(Array)
                        .GetMethods()
                        .Single(x =>
                            x.Name == nameof(Array.Empty) &&
                            x.IsGenericMethod)
                        .MakeGenericMethod(genericType);

                    itemsOldArray = objectPath.OldInstance == null
                        ? (Array)emptyMethod.Invoke(null, Array.Empty<object>())
                        : (Array)toArrayMethod.Invoke(null, new[] { objectPath.OldInstance });

                    itemsNewArray = objectPath.NewInstance == null
                        ? (Array)emptyMethod.Invoke(null, Array.Empty<object>())
                        : (Array)toArrayMethod.Invoke(null, new[] { objectPath.NewInstance });
                }

                var maxCount = Math.Max(
                    itemsOldArray?.Length ?? 0, 
                    itemsNewArray?.Length ?? 0);

                var newItemsOldArray = new object[maxCount];
                var newItemsNewArray = new object[maxCount];

                Array.Copy(itemsOldArray ?? Array.Empty<object>(), newItemsOldArray, itemsOldArray?.Length ?? 0);
                Array.Copy(itemsNewArray ?? Array.Empty<object>(), newItemsNewArray, itemsNewArray?.Length ?? 0);

                var shallowChange = new Change(
                    objectPath.BasePropertyPath ?? string.Empty,
                    objectPath.OldInstance,
                    objectPath.NewInstance);
                var newParentChanges = new[]
                {
                    shallowChange
                };

                var parentChanges = objectPath.ParentChanges == null ?
                    newParentChanges : 
                    objectPath
                        .ParentChanges
                        .Union(newParentChanges)
                        .Distinct()
                        .ToArray();

                for (var i = 0; i < maxCount; i++)
                {
                    var itemOldInstance = newItemsOldArray[i];
                    var itemNewInstance = newItemsNewArray[i];

                    var itemType = GetTypeFromObjects(itemOldInstance, itemNewInstance);
                    if (itemType == null)
                        continue;

                    var arrayItemObjectPath = new ObjectPath()
                    {
                        BasePropertyPath = AddToPropertyPath(
                            objectPath.BasePropertyPath,
                            i.ToString()),
                        OldInstance = itemOldInstance,
                        NewInstance = itemNewInstance,
                        Properties = itemType.GetProperties(),
                        ParentChanges = parentChanges
                    };

                    contextPair.ObjectPathQueue.Enqueue(arrayItemObjectPath);
                }
            }
            else
            {
                EnqueueObjectPathQueues(
                    contextPair,
                    objectPath);
            }
        }

        private static void EnqueueObjectPathQueues(
            ContextPair contextPair,
            ObjectPath objectPath)
        {
            var oldInstance = objectPath.OldInstance;
            var newInstance = objectPath.NewInstance;

            var type = GetTypeFromObjects(oldInstance, newInstance);
            if (type == null)
                throw new InvalidOperationException("The type could not be determined.");

            if (IsSimpleType(type))
                throw new InvalidOperationException("This method can't be called with simple types.");

            var seenObjectsOld = contextPair.OldInstanceContext.SeenObjects;
            var seenObjectsNew = contextPair.NewInstanceContext.SeenObjects;

            if (seenObjectsOld.Contains(objectPath.OldInstance) || seenObjectsNew.Contains(objectPath.NewInstance))
                return;

            if (objectPath.OldInstance != null)
                seenObjectsOld.Add(objectPath.OldInstance);

            if (objectPath.NewInstance != null)
                seenObjectsNew.Add(objectPath.NewInstance);

            var objectPathQueue = contextPair.ObjectPathQueue;

            var properties = type.GetProperties();
            foreach (var property in properties)
            {
                try
                {
                    objectPathQueue.Enqueue(new ObjectPath()
                    {
                        OldInstance = oldInstance == null ? null : property.GetValue(oldInstance),
                        NewInstance = newInstance == null ? null : property.GetValue(newInstance),
                        Properties = property.PropertyType.GetProperties(),
                        BasePropertyPath = AddToPropertyPath(
                            objectPath.BasePropertyPath,
                            property.Name),
                        ParentChanges = objectPath.ParentChanges
                    });
                }
                catch (Exception ex)
                {
                    throw new Exception($"An error occured while comparing {AddToPropertyPath(objectPath.BasePropertyPath, property.Name)}.", ex);
                }
            }
        }

        private static string AddToPropertyPath(string? basePropertyPath, string propertyName)
        {
            return !string.IsNullOrEmpty(basePropertyPath) ?
                basePropertyPath + "." + propertyName :
                propertyName;
        }

        private static Type? GetTypeFromObjects(object? a, object? b)
        {
            var type = a?.GetType() ?? b?.GetType();
            return type;
        }

        private static Change GetShallowChange(string? parentPropertyPath, object? oldValue, object? newValue)
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

        private static bool IsSimpleType(Type? type)
        {
            if (type == null)
                return false;
            
            return type.IsPrimitive ||
                   type.IsValueType ||
                   type == typeof(string);
        }

        private static object? GetValueOfExpressionFor<T>(T obj, Expression<Func<T, object>>? expression = null)
        {
            if (expression == null)
                return obj;

            var compiledExpression = expression.Compile();
            return compiledExpression(obj);
        }

        private class ContextPair : IDisposable
        {
            public Context OldInstanceContext
            {
                get;
            }

            public Context NewInstanceContext
            {
                get;
            }

            public Queue<ObjectPath> ObjectPathQueue
            {
                get;
            }

            public ContextPair(object? a, object? b)
            {
                OldInstanceContext = new Context(a);
                NewInstanceContext = new Context(b);

                ObjectPathQueue = new Queue<ObjectPath>();
            }

            public void Dispose()
            {
                OldInstanceContext.Dispose();
                NewInstanceContext.Dispose();
            }
        }

        private class Context : IDisposable
        {
            public object? Instance
            {
                get;
            }

            public ICollection<object?> SeenObjects
            {
                get;
            }

            public Context(object? instance)
            {
                Instance = instance;

                SeenObjects = new HashSet<object?>();
            }

            public void Dispose()
            {
                SeenObjects.Clear();
            }
        }

        private class ObjectPath
        {
            public object? OldInstance
            {
                get; set;
            }

            public object? NewInstance
            {
                get; set;
            }

            public Change[]? ParentChanges
            {
                get; set;
            }

            public PropertyInfo[] Properties
            {
                get; set;
            }

            public string? BasePropertyPath
            {
                get; set;
            }
        }
    }
}
