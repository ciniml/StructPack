using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace StructPack.Test
{
    [TestClass]
    public class PackTest
    {
        
        private struct StructWithAProperty<T>
        {
            public T Property { get; set; }
        }

        private void InnerTestAPublicProperty<T>(T expectedValue, bool isLittleEndian)
        { 
            var source = new StructWithAProperty<T>()
            {
                Property = expectedValue
            };
            var size = Packer<StructWithAProperty<T>>.GetRequiredSize(source, isLittleEndian);
            var expected = TypedBitConverter<T>.GetBytes(expectedValue, isLittleEndian);
            Assert.AreEqual(expected.Length, size);

            var actual = new byte[size];
            Packer<StructWithAProperty<T>>.Pack(source, actual, 0, isLittleEndian);
            Assert.IsTrue(expected.SequenceEqual(actual));
        }
        private void InnerTestAPublicProperty<T>()
        {
            InnerTestAPublicProperty<T>(NumericalTypeTraits<T>.Minimum, false);
            InnerTestAPublicProperty<T>(NumericalTypeTraits<T>.Minimum, true);
            InnerTestAPublicProperty<T>(NumericalTypeTraits<T>.Maximum, false);
            InnerTestAPublicProperty<T>(NumericalTypeTraits<T>.Maximum, true);
        }

        [TestMethod]
        public void TestAPublicProperty()
        {
            var method = (Action)InnerTestAPublicProperty<int>;
            var genericDefinition = method.Method.GetGenericMethodDefinition();
            foreach (var type in NumericalTypes.Types)
            {
                genericDefinition.MakeGenericMethod(type).Invoke(this, new object[0]);
            }
        }
    }
}
