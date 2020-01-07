using System;
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
            var changes = ChangeDetector.GetChangesRecursively(oldObject, newObject, expression);
            Assert.AreEqual(0, changes.ToArray().Length);
        }

        [DebuggerStepThrough]
        private void AssertHasNoChange(object oldObject, object newObject)
        {
            var changes = ChangeDetector.GetChangesRecursively(oldObject, newObject);
            Assert.AreEqual(0, changes.ToArray().Length);
        }

        [DebuggerStepThrough]
        private void AssertHasChange(object oldObject, object newObject, Change expectedChange)
        {
            var changes = ChangeDetector.GetChangesRecursively(oldObject, newObject);
            Assert.AreEqual(expectedChange, changes.SingleOrDefault());
        }

        [DebuggerStepThrough]
        private void AssertHasChange<T>(T oldObject, T newObject, Expression<Func<T, object>> expression, Change expectedChange)
        {
            var changes = ChangeDetector.GetChangesRecursively(oldObject, newObject, expression);
            Assert.AreEqual(expectedChange, changes.SingleOrDefault());
        }

        [DebuggerStepThrough]
        private void AssertHasChange<T>(T oldObject, T newObject, Change expectedChange)
        {
            AssertHasChange<T>(oldObject, newObject, null, expectedChange);
        }

        [TestMethod]
        public void HasChangedRecursively_DifferentStrings_ReturnsTrue()
        {
            Assert.IsTrue(ChangeDetector.HasChangedRecursively("foo", "bar"));
        }

        [TestMethod]
        public void HasChangedRecursively_SameStrings_ReturnsFalse()
        {
            Assert.IsFalse(ChangeDetector.HasChangedRecursively("foo", "foo"));
        }

        [TestMethod]
        public void HasChangedRecursively_NullAndString_ReturnsTrue()
        {
            Assert.IsTrue(ChangeDetector.HasChangedRecursively(null, "foo"));
        }

        [TestMethod]
        public void HasChangedRecursively_Nulls_ReturnsFalse()
        {
            Assert.IsFalse(ChangeDetector.HasChangedRecursively(null, null));
        }

        [TestMethod]
        public void HasChangedRecursively_DifferentShallowObjects_ReturnsTrue()
        {
            Assert.IsTrue(ChangeDetector.HasChangedRecursively(new ComplexObject()
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
            Assert.IsFalse(ChangeDetector.HasChangedRecursively(new ComplexObject()
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
            Assert.IsTrue(ChangeDetector.HasChangedRecursively(new ComplexObject()
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
            Assert.IsFalse(ChangeDetector.HasChangedRecursively(new ComplexObject()
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
            Assert.IsTrue(ChangeDetector.HasChangedRecursively(new DeepComplexObject()
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
        public void HasChangedRecursively_SameSameDeepObjects_ReturnsFalse()
        {
            Assert.IsFalse(ChangeDetector.HasChangedRecursively(new DeepComplexObject()
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
        public void GetChangesRecursively_DifferentShallowObjects_ReturnsTrue()
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
        public void GetChangesRecursively_SameSameShallowObjects_ReturnsFalse()
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
        public void GetChangesRecursively_DifferentShallowObjectStrings_ReturnsTrue()
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
        public void GetChangesRecursively_SameSameShallowObjectStrings_ReturnsFalse()
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
        public void GetChangesRecursively_DifferentDeepObjects_ReturnsTrue()
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
        public void GetChangesRecursively_SameSameDeepObjects_ReturnsFalse()
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
        public void HasChangedRecursively_RecursiveObjects_ReturnsFalse()
        {
            var recursiveObject1 = new RecursiveObject();
            recursiveObject1.Reference = recursiveObject1;

            var recursiveObject2 = new RecursiveObject();
            recursiveObject2.Reference = recursiveObject2;

            Assert.IsFalse(ChangeDetector.HasChangedRecursively(
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
    }
}
