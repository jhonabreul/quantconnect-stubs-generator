using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuantConnectStubsGenerator.Model;

namespace QuantConnectStubsGenerator.Tests.Model
{
    [TestFixture]
    public class ClassTests
    {
        [Test]
        public void GetUsedTypesShouldReturnAllTypesInTheClassAndItsInnerClasses()
        {
            var parentCls = new Class(new PythonType("ParentClass", "QuantConnect"));
            var childCls = new Class(new PythonType("ChildClass", "QuantConnect"))
            {
                ParentClass = parentCls,
                MetaClass = new PythonType("ABCMeta", "abc")
            };

            parentCls.InnerClasses.Add(childCls);

            childCls.Type.TypeParameters.Add(new PythonType("ChildClass.T", "QuantConnect")
            {
                IsNamedTypeParameter = true
            });

            childCls.InheritsFrom.Add(new PythonType("Any", "typing"));

            var usedTypes = parentCls.GetUsedTypes().ToList();

            Assert.AreEqual(7, usedTypes.Count);
            AssertTypeExists(usedTypes, "QuantConnect", "ParentClass");
            AssertTypeExists(usedTypes, "QuantConnect", "ChildClass");
            AssertTypeExists(usedTypes, "QuantConnect", "ChildClass.T");
            AssertTypeExists(usedTypes, "typing", "Generic");
            AssertTypeExists(usedTypes, "typing", "TypeVar");
            AssertTypeExists(usedTypes, "typing", "Any");
            AssertTypeExists(usedTypes, "abc", "ABCMeta");
        }

        [Test]
        public void GetUsedTypesShouldReturnAllTypesInTheClassAndItsNonStaticProperties()
        {
            var cls = new Class(new PythonType("MyClass", "QuantConnect"));

            cls.Properties.Add(new Property("Property1")
            {
                Type = new PythonType("MyProperty", "QuantConnect"),
                Abstract = true
            });

            var usedTypes = cls.GetUsedTypes().ToList();

            Assert.AreEqual(3, usedTypes.Count);
            AssertTypeExists(usedTypes, "QuantConnect", "MyClass");
            AssertTypeExists(usedTypes, "QuantConnect", "MyProperty");
            AssertTypeExists(usedTypes, "abc", "abstractmethod");
        }

        [Test]
        public void GetUsedTypesShouldReturnAllTypesInTheClassAndItsStaticProperties()
        {
            var cls = new Class(new PythonType("MyClass", "QuantConnect"));

            cls.Properties.Add(new Property("Property1")
            {
                Type = new PythonType("MyProperty", "QuantConnect"),
                Abstract = true,
                Static = true
            });

            var usedTypes = cls.GetUsedTypes().ToList();

            Assert.AreEqual(2, usedTypes.Count);
            AssertTypeExists(usedTypes, "QuantConnect", "MyClass");
            AssertTypeExists(usedTypes, "QuantConnect", "MyProperty");
        }

        [Test]
        public void GetUsedTypesShouldReturnAllTypesInTheClassAndItsMethods()
        {
            var cls = new Class(new PythonType("MyClass", "QuantConnect"));

            cls.Methods.Add(new Method("Method1", new PythonType("ReturnType", "QuantConnect"))
            {
                Overload = true,
                Static = true,
                Parameters =
                {
                    new Parameter("parameter1", new PythonType("Parameter1", "QuantConnect")),
                    new Parameter("parameter2", new PythonType("Parameter2", "QuantConnect")),
                    new Parameter("parameter3", new PythonType("Parameter3", "QuantConnect")),
                }
            });

            var usedTypes = cls.GetUsedTypes().ToList();

            Assert.AreEqual(6, usedTypes.Count);
            AssertTypeExists(usedTypes, "QuantConnect", "MyClass");
            AssertTypeExists(usedTypes, "QuantConnect", "ReturnType");
            AssertTypeExists(usedTypes, "QuantConnect", "Parameter1");
            AssertTypeExists(usedTypes, "QuantConnect", "Parameter2");
            AssertTypeExists(usedTypes, "QuantConnect", "Parameter3");
            AssertTypeExists(usedTypes, "typing", "overload");
        }

        private void AssertTypeExists(IEnumerable<PythonType> types, string ns, string name)
        {
            Assert.IsTrue(types.Any(t => t.Namespace == ns && t.Name == name));
        }
    }
}
