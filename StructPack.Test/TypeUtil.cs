using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StructPack.Test
{
    static class NumericalTypes
    {
        public static IReadOnlyList<Type> Types { get; }

        static NumericalTypes()
        {
            Types = new[]
            {
                typeof(byte),
                typeof(sbyte),
                typeof(ushort),
                typeof(short),
                typeof(uint),
                typeof(int),
                typeof(ulong),
                typeof(long),
                typeof(float),
                typeof(double),
                typeof(char),
            };
        }
    }

    static class NumericalTypeTraits<T>
    {
        public static T Maximum { get; }
        public static T Minimum { get; }
        static NumericalTypeTraits()
        {
            var t = (object)default(T);
            switch(t)
            {
                case byte n:
                    Minimum = (T)(object)byte.MinValue;
                    Maximum = (T)(object)byte.MaxValue;
                    break;
                case sbyte n:
                    Minimum = (T)(object)sbyte.MinValue;
                    Maximum = (T)(object)sbyte.MaxValue;
                    break;

                case ushort n:
                    Minimum = (T)(object)ushort.MinValue;
                    Maximum = (T)(object)ushort.MaxValue;
                    break;
                case short n:
                    Minimum = (T)(object)short.MinValue;
                    Maximum = (T)(object)short.MaxValue;
                    break;

                case uint n:
                    Minimum = (T)(object)uint.MinValue;
                    Maximum = (T)(object)uint.MaxValue;
                    break;
                case int n:
                    Minimum = (T)(object)int.MinValue;
                    Maximum = (T)(object)int.MaxValue;
                    break;

                case ulong n:
                    Minimum = (T)(object)ulong.MinValue;
                    Maximum = (T)(object)ulong.MaxValue;
                    break;
                case long n:
                    Minimum = (T)(object)long.MinValue;
                    Maximum = (T)(object)long.MaxValue;
                    break;

                case float n:
                    Minimum = (T)(object)float.MinValue;
                    Maximum = (T)(object)float.MaxValue;
                    break;
                case double n:
                    Minimum = (T)(object)double.MinValue;
                    Maximum = (T)(object)double.MaxValue;
                    break;

                case char n:
                    Minimum = (T)(object)char.MinValue;
                    Maximum = (T)(object)char.MaxValue;
                    break;


                default:
                    throw new ArgumentException();
            }
        }
    }

    static class TypedBitConverter<T>
    {
        static byte[] InnerGetBytes(T value)
        {
            switch((object)value)
            {
                case byte typed: return new byte[] { typed };
                case sbyte typed: return new byte[] { (byte)typed };
                case ushort typed: return BitConverter.GetBytes(typed);
                case short typed: return BitConverter.GetBytes(typed);
                case uint typed: return BitConverter.GetBytes(typed);
                case int typed: return BitConverter.GetBytes(typed);
                case ulong typed: return BitConverter.GetBytes(typed);
                case long typed: return BitConverter.GetBytes(typed);
                case float typed: return BitConverter.GetBytes(typed);
                case double typed: return BitConverter.GetBytes(typed);
                case char typed: return BitConverter.GetBytes(typed);
                default: throw new ArgumentException();
            }
        }

        public static byte[] GetBytes(T value, bool isLittleEndian)
        {
            var bytes = InnerGetBytes(value);
            return BitConverter.IsLittleEndian == isLittleEndian ? bytes : bytes.Reverse().ToArray();
        }
        
    }
}
