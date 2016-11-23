using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace StructPack
{
    internal class Packer
    {
        private struct ConvertExpressionParts
        {
            public Expression[] Prologue { get; }
            public Expression[] ValueExpressions { get; }
            public int Size { get; }
            public ParameterExpression[] Variables { get; }
            public ConvertExpressionParts(Expression[] prologue, Expression[] valueExpressions, int size, ParameterExpression[] variables)
            {
                this.Prologue = prologue;
                this.ValueExpressions = valueExpressions;
                this.Size = size;
                this.Variables = variables;
            }

            public void Deconstruct(out Expression[] prologue, out Expression[] valueExpressions, out int size, out ParameterExpression[] variables)
            {
                prologue = this.Prologue;
                valueExpressions = this.ValueExpressions;
                size = this.Size;
                variables = this.Variables;
            }
        }
        private delegate ConvertExpressionParts MakeConvertExpressionDelegate(Expression value, bool isLittleEndian);

        private static IReadOnlyDictionary<Type, MakeConvertExpressionDelegate> makeConvertExpressions = new ReadOnlyDictionary<Type, MakeConvertExpressionDelegate>(
            new Dictionary<Type, MakeConvertExpressionDelegate>{
                {typeof(Byte), MakeConvertExpression_Int8 },
                {typeof(SByte), MakeConvertExpression_Int8 },
                {typeof(UInt16), MakeConvertExpression_Int16 },
                {typeof(Int16), MakeConvertExpression_Int16 },
                {typeof(UInt32), MakeConvertExpression_Int32 },
                {typeof(Int32), MakeConvertExpression_Int32 },
                {typeof(UInt64), MakeConvertExpression_Int64 },
                {typeof(Int64), MakeConvertExpression_Int64 },
                {typeof(Single), MakeConvertExpression_Float },
                {typeof(Double), MakeConvertExpression_Float },
                {typeof(Char), MakeConvertExpression_Int16 },
            });

        internal static (Expression expression, int size) MakeConvertExpression(Type type, Expression value, Expression buffer, Expression index, bool isLittleEndian)
        {
            (var prologue, var valueExpressions, var size, var variables) = makeConvertExpressions[type](value, isLittleEndian);
            var expressions = new List<Expression>();
            if (prologue != null)
            {
                expressions.AddRange(prologue);
            }
            foreach ((var valueExpression, var expressionIndex) in valueExpressions.Select((expression, index_) => (expression, index_)))
            {
                var assignExpression = Expression.Assign(
                    Expression.ArrayAccess(buffer, Expression.Add(index, Expression.Constant(expressionIndex))),
                    valueExpression);
                expressions.Add(assignExpression);
            }
            expressions.Add(Expression.AddAssign(index, Expression.Constant(size)));

            return (Expression.Block(variables ?? new ParameterExpression[0], expressions.ToArray()), size);
        }

        private static ConvertExpressionParts MakeConvertExpression_Int8(Expression value, bool isLittleEndian)
        {
            var valueExpressions = new[]
            {
                Expression.Convert(value, typeof(Byte))
            };
            return new ConvertExpressionParts(null, valueExpressions, 1, null);
        }

        private static ConvertExpressionParts MakeConvertExpression_Int16(Expression value, bool isLittleEndian)
        {
            var valueExpressions = new[]
            {
                Expression.Convert(Expression.RightShift(Expression.Convert(value, typeof(UInt32)), Expression.Constant(8)), typeof(byte)),
                Expression.Convert(Expression.And(Expression.Convert(value, typeof(UInt32)), Expression.Constant(0xffu)), typeof(byte)),
            };
            if (isLittleEndian)
            {
                valueExpressions = valueExpressions.Reverse().ToArray();
            }
            return new ConvertExpressionParts(null, valueExpressions, 2, null);
        }

        private static ConvertExpressionParts MakeConvertExpression_Int32(Expression value, bool isLittleEndian)
        {
            var valueExpressions = new[]
            {
                Expression.Convert(Expression.RightShift(value, Expression.Constant(24)), typeof(byte)),
                Expression.Convert(Expression.And(Expression.Convert(Expression.RightShift(value, Expression.Constant(16)), typeof(UInt32)), Expression.Constant(0xffu)), typeof(byte)),
                Expression.Convert(Expression.And(Expression.Convert(Expression.RightShift(value, Expression.Constant(8)), typeof(UInt32)), Expression.Constant(0xffu)), typeof(byte)),
                Expression.Convert(Expression.And(Expression.Convert(value, typeof(UInt32)), Expression.Constant(0xffu)), typeof(byte)),
            };
            if (isLittleEndian)
            {
                valueExpressions = valueExpressions.Reverse().ToArray();
            }
            return new ConvertExpressionParts(null, valueExpressions, 4, null);
        }

        private static ConvertExpressionParts MakeConvertExpression_Int64(Expression value, bool isLittleEndian)
        {
            var valueExpressions = new[]
            {
                Expression.Convert(Expression.RightShift(value, Expression.Constant(56)), typeof(byte)),
                Expression.Convert(Expression.And(Expression.Convert(Expression.RightShift(value, Expression.Constant(48)), typeof(UInt64)), Expression.Constant(0xfful)), typeof(byte)),
                Expression.Convert(Expression.And(Expression.Convert(Expression.RightShift(value, Expression.Constant(40)), typeof(UInt64)), Expression.Constant(0xfful)), typeof(byte)),
                Expression.Convert(Expression.And(Expression.Convert(Expression.RightShift(value, Expression.Constant(32)), typeof(UInt64)), Expression.Constant(0xfful)), typeof(byte)),
                Expression.Convert(Expression.And(Expression.Convert(Expression.RightShift(value, Expression.Constant(24)), typeof(UInt64)), Expression.Constant(0xfful)), typeof(byte)),
                Expression.Convert(Expression.And(Expression.Convert(Expression.RightShift(value, Expression.Constant(16)), typeof(UInt64)), Expression.Constant(0xfful)), typeof(byte)),
                Expression.Convert(Expression.And(Expression.Convert(Expression.RightShift(value, Expression.Constant(8)), typeof(UInt64)), Expression.Constant(0xfful)), typeof(byte)),
                Expression.Convert(Expression.And(Expression.Convert(value, typeof(UInt64)), Expression.Constant(0xfful)), typeof(byte)),
            };
            if (isLittleEndian)
            {
                valueExpressions = valueExpressions.Reverse().ToArray();
            }
            return new ConvertExpressionParts(null, valueExpressions, 8, null);
        }

        private static ConvertExpressionParts MakeConvertExpression_Float(Expression value, bool isLittleEndian)
        {
            var converterFloat = (Func<float, byte[]>)BitConverter.GetBytes;
            var converterDouble = (Func<double, byte[]>)BitConverter.GetBytes;
            var converter = value.Type == typeof(float) ? converterFloat.Method : converterDouble.Method;

            var convertedBytes = Expression.Variable(typeof(byte[]), "convertedBytes");

            var prologue = new Expression[]
            {
                convertedBytes,
                Expression.Assign(convertedBytes, Expression.Call(null, converter, value)),
            };
            var length = value.Type == typeof(float) ? 4 : 8;
            var valueExpressions = Enumerable.Range(0, length)
                .Select(index => Expression.ArrayIndex(convertedBytes, Expression.Constant(index)))
                .ToArray();
            if (isLittleEndian != BitConverter.IsLittleEndian)
            {
                valueExpressions = valueExpressions.Reverse().ToArray();
            }
            return new ConvertExpressionParts(prologue, valueExpressions, length, new[] { convertedBytes });
        }
    }

    public class Packer<T>
    {
        private static readonly Lazy<(Action<T, byte[], int>, int)> serializerLe = new Lazy<(Action<T, byte[], int>, int)>(() => MakeSerializer(true));
        private static readonly Lazy<(Action<T, byte[], int>, int)> serializerBe = new Lazy<(Action<T, byte[], int>, int)>(() => MakeSerializer(false));

        private static (Action<T, byte[], int> serializer, int totalSize) MakeSerializer(bool isLittleEndian)
        {
            var sourceParameter = Expression.Parameter(typeof(T), "source");
            var bufferParameter = Expression.Parameter(typeof(byte[]), "buffer");
            var offsetParameter = Expression.Parameter(typeof(int), "offset");

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            int totalSize = 0;
            var expressions = new List<Expression>();
            foreach (var property in properties)
            {
                (var expression, var size) = Packer.MakeConvertExpression(
                    property.PropertyType,
                    Expression.Property(sourceParameter, property.GetMethod),
                    bufferParameter,
                    offsetParameter,
                    isLittleEndian);
                expressions.Add(expression);
                totalSize += size;
            }

            var lambda = Expression.Lambda<Action<T, byte[], int>>(
                Expression.Block(expressions),
                sourceParameter, bufferParameter, offsetParameter);

            return (lambda.Compile(), totalSize);
        }

        public static int GetRequiredSize(T source, bool isLittleEndian)
        {
            return isLittleEndian ? serializerLe.Value.Item2 : serializerBe.Value.Item2;
        }

        public static void Pack(T source, byte[] buffer, int offset, bool isLittleEndian)
        {
            var serializer = isLittleEndian ? serializerLe.Value.Item1 : serializerBe.Value.Item1;
            serializer(source, buffer, offset);
        }
    }
}
