using System.Runtime.InteropServices.ComTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FluffySpoon.ChangeDetection.Tests
{
    [TestClass]
    public class ChangeDetectorTest
    {
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
