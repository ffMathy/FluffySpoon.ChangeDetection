using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FluffySpoon.ChangeDetection.Tests
{
    [TestClass]
    public class ChangeDetectorTest
    {
        [DebuggerStepThrough]
        private void AssertHasNoChange<T>(T oldObject, T newObject, Expression<Func<T, object>> expression)
        {
            var changes = ChangeDetector.GetChanges(oldObject, newObject, expression);
            Assert.AreEqual(0, changes.ToArray().Length);
        }

        [DebuggerStepThrough]
        private void AssertHasNoChange(object oldObject, object newObject)
        {
            var changes = ChangeDetector.GetChanges(oldObject, newObject);
            Assert.AreEqual(0, changes.ToArray().Length);
        }

        [DebuggerStepThrough]
        private void AssertHasChange(object oldObject, object newObject, Change expectedChange)
        {
            var changes = ChangeDetector.GetChanges(oldObject, newObject);
            Assert.AreEqual(expectedChange, changes.SingleOrDefault());
        }

        [DebuggerStepThrough]
        private void AssertHasChange<T>(T oldObject, T newObject, Expression<Func<T, object>> expression, Change expectedChange)
        {
            var changes = ChangeDetector.GetChanges(oldObject, newObject, expression);
            Assert.AreEqual(expectedChange, changes.SingleOrDefault());
        }

        [DebuggerStepThrough]
        private void AssertHasChange<T>(T oldObject, T newObject, Change expectedChange)
        {
            AssertHasChange<T>(oldObject, newObject, null, expectedChange);
        }

        [TestMethod]
        public void HasChangedRecursively_SameDates_ReturnsFalse()
        {
            Assert.IsFalse(ChangeDetector.HasChanges(
                new DateTime(2000, 1, 1), 
                new DateTime(2000, 1, 1)));
        }

        [TestMethod]
        public void HasChangedRecursively_DifferentDates_ReturnsTrue()
        {
            Assert.IsTrue(ChangeDetector.HasChanges(
                new DateTime(2000, 1, 2), 
                new DateTime(2000, 1, 1)));
        }

        [TestMethod]
        public void HasChangedRecursively_DifferentStrings_ReturnsTrue()
        {
            Assert.IsTrue(ChangeDetector.HasChanges("foo", "bar"));
        }

        [TestMethod]
        public void HasChangedRecursively_SameStrings_ReturnsFalse()
        {
            Assert.IsFalse(ChangeDetector.HasChanges("foo", "foo"));
        }

        [TestMethod]
        public void HasChangedRecursively_NullAndString_ReturnsTrue()
        {
            Assert.IsTrue(ChangeDetector.HasChanges(null, "foo"));
        }

        [TestMethod]
        public void HasChangedRecursively_Nulls_ReturnsFalse()
        {
            Assert.IsFalse(ChangeDetector.HasChanges(null, null));
        }

        [TestMethod]
        public void HasChangedRecursively_DifferentShallowObjects_ReturnsTrue()
        {
            Assert.IsTrue(ChangeDetector.HasChanges(new ComplexObject()
            {
                StringValue = "foo"
            },
                new ComplexObject()
                {
                    StringValue = "bar"
                }));
        }

        [TestMethod]
        public void HasChangedRecursively_SameSameShallowObjects_ReturnsFalse()
        {
            Assert.IsFalse(ChangeDetector.HasChanges(new ComplexObject()
            {
                StringValue = "foo"
            },
                new ComplexObject()
                {
                    StringValue = "foo"
                }));
        }

        [TestMethod]
        public void HasChangedRecursively_DifferentShallowObjectStrings_ReturnsTrue()
        {
            Assert.IsTrue(ChangeDetector.HasChanges(new ComplexObject()
            {
                StringValue = "foo"
            },
                new ComplexObject()
                {
                    StringValue = "bar"
                }, x => x.StringValue));
        }

        [TestMethod]
        public void HasChangedRecursively_SameSameShallowObjectStrings_ReturnsFalse()
        {
            Assert.IsFalse(ChangeDetector.HasChanges(new ComplexObject()
            {
                StringValue = "foo"
            },
                new ComplexObject()
                {
                    StringValue = "foo"
                }, x => x.StringValue));
        }

        [TestMethod]
        public void HasChangedRecursively_DifferentDeepObjects_ReturnsTrue()
        {
            Assert.IsTrue(ChangeDetector.HasChanges(new DeepComplexObject()
            {
                ComplexObject = new ComplexObject()
                {
                    StringValue = "foo"
                }
            },
                new DeepComplexObject()
                {
                    ComplexObject = new ComplexObject()
                    {
                        StringValue = "bar"
                    }
                }));
        }

        [TestMethod]
        public void HasChangedRecursively_SameArrays_ReturnsFalse()
        {
            Assert.IsFalse(ChangeDetector.HasChanges(
                new [] { "foo" },
                new [] { "foo" }));
        }

        [TestMethod]
        public void HasChangedRecursively_DifferentArrays_ReturnsTrue()
        {
            Assert.IsTrue(ChangeDetector.HasChanges(
                new [] { "foo" },
                new [] { "bar" }));
        }

        [TestMethod]
        public void HasChangedRecursively_SameSameDeepObjects_ReturnsFalse()
        {
            Assert.IsFalse(ChangeDetector.HasChanges(new DeepComplexObject()
            {
                ComplexObject = new ComplexObject()
                {
                    StringValue = "foo"
                }
            },
                new DeepComplexObject()
                {
                    ComplexObject = new ComplexObject()
                    {
                        StringValue = "foo"
                    }
                }));
        }

        [TestMethod]
        public void GetChangesRecursively_DifferentStrings_ReturnsTrue()
        {
            AssertHasChange("foo", "bar", new Change(string.Empty, "foo", "bar"));
        }

        [TestMethod]
        public void GetChangesRecursively_SameStrings_ReturnsFalse()
        {
            AssertHasNoChange("foo", "foo");
        }

        [TestMethod]
        public void GetChangesRecursively_NullAndString_ReturnsTrue()
        {
            AssertHasChange(null, "foo", new Change(string.Empty, null, "foo"));
        }

        [TestMethod]
        public void GetChangesRecursively_Nulls_ReturnsFalse()
        {
            AssertHasNoChange(null, null);
        }

        [TestMethod]
        public void GetChangesRecursively_DifferentShallowObjects_HasChange()
        {
            AssertHasChange(
                new ComplexObject()
                {
                    StringValue = "foo"
                },
                new ComplexObject()
                {
                    StringValue = "bar"
                },
                new Change("StringValue", "foo", "bar"));
        }

        [TestMethod]
        public void GetChangesRecursively_SameSameShallowObjects_HasNoChanges()
        {
            AssertHasNoChange(
                new ComplexObject()
                {
                    StringValue = "foo"
                },
                new ComplexObject()
                {
                    StringValue = "foo"
                });
        }

        [TestMethod]
        public void GetChangesRecursively_DifferentShallowObjectStrings_HasChange()
        {
            AssertHasChange(
                new ComplexObject()
                {
                    StringValue = "foo"
                },
                new ComplexObject()
                {
                    StringValue = "bar"
                },
                x => x.StringValue,
                new Change("StringValue", "foo", "bar"));
        }

        [TestMethod]
        public void GetChangesRecursively_SameSameShallowObjectStrings_HasNoChanges()
        {
            AssertHasNoChange(
                new ComplexObject()
                {
                    StringValue = "foo"
                },
                new ComplexObject()
                {
                    StringValue = "foo"
                },
                x => x.StringValue);
        }

        [TestMethod]
        public void GetChangesRecursively_DifferentSubObjectCollectionsInDeepObjects_HasChange()
        {
            var changes = ChangeDetector
                .GetChanges(
                    new DeepComplexObject()
                    {
                        ComplexObject = new ComplexObject()
                        {
                            SubObjects = new List<ComplexObject>()
                        }
                    },
                    new DeepComplexObject()
                    {
                        ComplexObject = new ComplexObject()
                        {
                            SubObjects = new List<ComplexObject>()
                            {
                                new ComplexObject()
                            }
                        }
                    });

            Assert.AreEqual(4, changes.Count);

            Assert.IsTrue(changes.HasChangeFor(x => x.ComplexObject.SubObjects));
        }

        [TestMethod]
        public void GetChangesRecursively_DifferentDeepObjects_HasChange()
        {
            AssertHasChange(
                new DeepComplexObject()
                {
                    ComplexObject = new ComplexObject()
                    {
                        StringValue = "foo"
                    }
                },
                new DeepComplexObject()
                {
                    ComplexObject = new ComplexObject()
                    {
                        StringValue = "bar"
                    }
                },
                new Change("ComplexObject.StringValue", "foo", "bar"));
        }

        [TestMethod]
        public void GetChangesRecursively_DifferentDeepVeryDifferentObjects_HasProperChanges()
        {
            var changes = ChangeDetector
                .GetChanges(
                    new ComplexObject()
                    {
                        StringValue = "foo",
                        MyIntValue = 28,
                        SubObject = new ComplexObject()
                        {
                            StringValue = "bar",
                            MyIntValue = 123
                        }
                    },
                    new ComplexObject()
                    {
                        StringValue = "fuz",
                        MyIntValue = 1337,
                        SubObject = new ComplexObject()
                        {
                            StringValue = "baz",
                            MyIntValue = 123
                        }
                    })
                .OrderBy(x => x.PropertyPath)
                .ToArray();

            Assert.AreEqual(3, changes.Length);

            Assert.AreEqual(new Change("MyIntValue", 28, 1337), changes[0]);
            Assert.AreEqual(new Change("StringValue", "foo", "fuz"), changes[1]);
            Assert.AreEqual(new Change("SubObject.StringValue", "bar", "baz"), changes[2]);
        }

        [TestMethod]
        public void GetChangesRecursively_NullSubObjectToComplexObject_HasProperChanges()
        {
            var oldObject = new ComplexObject()
            {
                StringValue = "foo",
                MyIntValue = 28,
                SubObject = null
            };

            var newObject = new ComplexObject()
            {
                StringValue = "fuz",
                MyIntValue = 1337,
                SubObject = new ComplexObject()
                {
                    StringValue = "baz",
                    MyIntValue = 123
                }
            };

            var changes = ChangeDetector
                .GetChanges(
                    oldObject,
                    newObject)
                .OrderBy(x => x.PropertyPath)
                .ToArray();

            Assert.AreEqual(6, changes.Length);

            Assert.AreEqual(new Change("MyIntValue", 28, 1337), changes[0]);
            Assert.AreEqual(new Change("StringValue", "foo", "fuz"), changes[1]);
            Assert.AreEqual(new Change("SubObject.MyIntValue", null, 123), changes[2]);
            Assert.AreEqual(new Change("SubObject.StringsHashSet", null, newObject.SubObject.StringsHashSet), changes[3]);
            Assert.AreEqual(new Change("SubObject.StringsHashSet.0", null, "foo"), changes[4]);
            Assert.AreEqual(new Change("SubObject.StringValue", null, "baz"), changes[5]);
        }

        [TestMethod]
        public void GetChangesRecursively_ComplexObjectToNullSubObject_HasProperChanges()
        {
            var oldObject = new ComplexObject()
            {
                StringValue = "foo",
                MyIntValue = 28,
                SubObject = new ComplexObject()
                {
                    StringValue = "baz",
                    MyIntValue = 123
                }
            };

            var newObject = new ComplexObject()
            {
                StringValue = "fuz",
                MyIntValue = 1337,
                SubObject = null
            };

            var changeCollection = ChangeDetector
                .GetChanges(
                    oldObject,
                    newObject);

            var changes = changeCollection
                .OrderBy(x => x.PropertyPath)
                .ToArray();

            Assert.AreEqual(6, changes.Length);

            Assert.AreEqual(new Change("MyIntValue", 28, 1337), changes[0]);
            Assert.AreEqual(new Change("StringValue", "foo", "fuz"), changes[1]);
            Assert.AreEqual(new Change("SubObject.MyIntValue", 123, null), changes[2]);
            Assert.AreEqual(new Change("SubObject.StringsHashSet", oldObject.SubObject.StringsHashSet, null), changes[3]);
            Assert.AreEqual(new Change("SubObject.StringsHashSet.0", "foo", null), changes[4]);
            Assert.AreEqual(new Change("SubObject.StringValue", "baz", null), changes[5]);

            Assert.IsTrue(changeCollection.HasChangeFor(x => x.SubObject.StringsHashSet));
        }

        [TestMethod]
        public void GetChangesRecursively_StringArray_HasProperChanges()
        {
            var oldObject = new [] { "foo", "bar", "baz", "fuz" };
            var newObject = new [] { "foo", "lol", "hi", "fuz" };

            var changes = ChangeDetector
                .GetChanges(
                    oldObject,
                    newObject)
                .OrderBy(x => x.PropertyPath)
                .ToArray();

            Assert.AreEqual(3, changes.Length);
            
            Assert.AreEqual(new Change("", oldObject, newObject), changes[0]);
            Assert.AreEqual(new Change("1", "bar", "lol"), changes[1]);
            Assert.AreEqual(new Change("2", "baz", "hi"), changes[2]);
        }

        [TestMethod]
        public void GetChangesRecursively_StringList_HasProperChanges()
        {
            var oldObject = new List<string>() { "foo", "bar", "baz", "fuz" };
            var newObject = new List<string>() { "foo", "lol", "hi", "fuz" };

            var changes = ChangeDetector
                .GetChanges(
                    oldObject,
                    newObject)
                .OrderBy(x => x.PropertyPath)
                .ToArray();

            Assert.AreEqual(3, changes.Length);
            
            Assert.AreEqual(new Change("", oldObject, newObject), changes[0]);
            Assert.AreEqual(new Change("1", "bar", "lol"), changes[1]);
            Assert.AreEqual(new Change("2", "baz", "hi"), changes[2]);
        }

        [TestMethod]
        public void GetChangesRecursively_StringHashSet_HasProperChanges()
        {
            var oldObject = new HashSet<string>(new [] { "foo", "bar", "baz", "fuz" });
            var newObject = new HashSet<string>(new [] { "foo", "lol", "hi", "fuz" });

            var changes = ChangeDetector
                .GetChanges(
                    oldObject,
                    newObject)
                .OrderBy(x => x.PropertyPath)
                .ToArray();

            Assert.AreEqual(3, changes.Length);
            
            Assert.AreEqual(new Change("", oldObject, newObject), changes[0]);
            Assert.AreEqual(new Change("1", "bar", "lol"), changes[1]);
            Assert.AreEqual(new Change("2", "baz", "hi"), changes[2]);
        }

        [TestMethod]
        public void GetChangesRecursively_SameSameDeepObjects_HasNoChanges()
        {
            AssertHasNoChange(
                new DeepComplexObject()
                {
                    ComplexObject = new ComplexObject()
                    {
                        StringValue = "foo"
                    }
                },
                new DeepComplexObject()
                {
                    ComplexObject = new ComplexObject()
                    {
                        StringValue = "foo"
                    }
                });
        }

        [TestMethod]
        public void HasChangesRecursively_RecursiveObjects_ReturnsFalse()
        {
            var recursiveObject1 = new RecursiveObject();
            recursiveObject1.Reference = recursiveObject1;

            var recursiveObject2 = new RecursiveObject();
            recursiveObject2.Reference = recursiveObject2;

            Assert.IsFalse(ChangeDetector.HasChanges(
                recursiveObject1,
                recursiveObject2));
        }

        private class RecursiveObject
        {
            public RecursiveObject Reference
            {
                get; set;
            }
        }

        private class ComplexObject
        {
            public string StringValue
            {
                get; set;
            }

            public int MyIntValue
            {
                get; set;
            }

            public HashSet<string> StringsHashSet { get; }

            public HashSet<SimpleEnum> EnumHashSet { get; }
            public HashSet<SimpleStruct> StructHashSet { get; }

            public Dictionary<string, string> StringsDictionary { get; set; }

            public List<ComplexObject> SubObjects { get; set; }

            public ComplexObject SubObject
            {
                get; set;
            }

            public ComplexObject()
            {
                StringsHashSet = new HashSet<string>(new [] {"foo"});
                EnumHashSet = new HashSet<SimpleEnum>();
                StructHashSet = new HashSet<SimpleStruct>();
                StringsDictionary = new Dictionary<string, string>();
                SubObjects = new List<ComplexObject>();
            }
        }

        private class DeepComplexObject
        {
            public string StringValue
            {
                get; set;
            }

            public ComplexObject ComplexObject
            {
                get; set;
            }
        }

        private enum SimpleEnum
        {
        }

        private struct SimpleStruct
        {
            
        }
    }
}
