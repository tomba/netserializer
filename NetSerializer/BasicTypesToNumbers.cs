#define NO_DRAWING
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

#if !NO_DRAWING  
using System.Drawing;
#endif

namespace NetSerializer
{
    public static class BasicTypesToNumbers
    {
        private static Dictionary<Type, Int16> typeToNumberDictionary;
        private static Dictionary<Int16, Type> numberToTypeDictionary;

        private static void initializeBasicTypes()
        {
            //All Types from: http://msdn.microsoft.com/en-us/library/br205768(v=vs.85).aspx
            var types = new[]
            {
                typeof (string), typeof (string[]),

                typeof(Uri), typeof(Uri[]),

                typeof (bool), typeof (byte), typeof (sbyte), typeof (char), typeof (decimal),
                typeof (double), typeof (float), typeof (int), typeof (uint), typeof (long), typeof (ulong),
                typeof (short), typeof (ushort), typeof (DateTime), typeof (TimeSpan), typeof (Guid), typeof(DateTimeOffset), 

                typeof (bool?), typeof (byte?), typeof (sbyte?), typeof (char?), typeof (decimal?),
                typeof (double?), typeof (float?), typeof (int?), typeof (uint?), typeof (long?), typeof (ulong?),
                typeof (short?), typeof (ushort?), typeof (DateTime?), typeof (TimeSpan?), typeof (Guid?), typeof(DateTimeOffset?),

                typeof (bool[]), typeof (byte[]), typeof (sbyte[]), typeof (char[]), typeof (decimal[]),
                typeof (double[]), typeof (float[]), typeof (int[]), typeof (uint[]), typeof (long[]), typeof (ulong[]),
                typeof (short[]), typeof (ushort[]), typeof (DateTime[]), typeof (TimeSpan[]), typeof (Guid[]), typeof(DateTimeOffset[]),

                typeof (bool?[]), typeof (byte?[]), typeof (sbyte?[]), typeof (char?[]), typeof (decimal?[]),
                typeof (double?[]), typeof (float?[]), typeof (int?[]), typeof (uint?[]), typeof (long?[]), typeof (ulong?[]),
                typeof (short?[]), typeof (ushort?[]), typeof (DateTime?[]), typeof (TimeSpan?[]), typeof (Guid?[]), typeof(DateTimeOffset?[]),

                typeof (List<string>), typeof (IEnumerable<string>),

                typeof(List<Uri>), typeof(IEnumerable<Uri>),


                typeof (List<bool>), typeof (List<byte>), typeof (List<sbyte>), typeof (List<char>), typeof (List<decimal>),
                typeof (List<double>), typeof (List<float>), typeof (List<int>), typeof (List<uint>), typeof (List<long>), typeof (List<ulong>),
                typeof (List<short>), typeof (List<ushort>), typeof (List<DateTime>), typeof (List<TimeSpan>), typeof (List<Guid>), typeof(List<DateTimeOffset>),

                typeof (IEnumerable<bool>), typeof (IEnumerable<byte>), typeof (IEnumerable<sbyte>), typeof (IEnumerable<char>), typeof (IEnumerable<decimal>),
                typeof (IEnumerable<double>), typeof (IEnumerable<float>), typeof (IEnumerable<int>), typeof (IEnumerable<uint>), typeof (IEnumerable<long>), typeof (IEnumerable<ulong>),
                typeof (IEnumerable<short>), typeof (IEnumerable<ushort>), typeof (IEnumerable<DateTime>), typeof (IEnumerable<TimeSpan>), typeof (IEnumerable<Guid>), typeof(IEnumerable<DateTimeOffset>),

#if !NO_DRAWING
                typeof(Size), typeof(Point), typeof(Rectangle),
#endif

                //typeof(BigInteger), typeof(BigInteger?), typeof(BigInteger[]), typeof(BigInteger?[]),
            };

            Int16 i = 0;
            numberToTypeDictionary = types.ToDictionary(x => i++);
            i = 0;
            typeToNumberDictionary = types.ToDictionary(x => x, y => i++);
        }

        internal static void Initialize(Type[] types)
        {
            initializeBasicTypes();

            foreach (var t in types)
            {
                if (!typeToNumberDictionary.ContainsKey(t))
                {
                    var att = t.GetCustomAttribute<TypeNumberAttribute>();
                    if (att != null)
                    {
                        numberToTypeDictionary.Add(att.Number, t);
                        typeToNumberDictionary.Add(t, att.Number);
                    }
                    else
                    {
                        /*if (!(typeof (INetserializerSerialisation).IsAssignableFrom(t)))
                            throw new ArgumentException("Type " + t.FullName +
                                                        " does not have Attribute TypeNumberAttribute set!");*/
                    }
                }
            }
        }

        internal static Int16 GetObjectId(object obj)
        {
            return typeToNumberDictionary[obj.GetType()];
        }

        internal static object WriteObjectToStream(Stream stream, object obj)
        {
            return null;
        }


        internal static object ReadObjectFromStream(Int16 objtypeid, Stream stream)
        {
            var tp = numberToTypeDictionary[objtypeid];

            return null;
        }

        /// <summary>
        /// Adds a Custom type to the List of Serializable Types...
        /// </summary>
        /// <param name="number"></param>
        /// <param name="type"></param>
        public static void AddUserTypeNumber(Int16 number, Type type)
        {
            numberToTypeDictionary.Add(number, type);
            typeToNumberDictionary.Add(type, number);
        }

        public static T GetCustomAttribute<T>(this Type type, bool inherit = false) where T : class
        {
            return type.GetCustomAttributes(typeof (T), inherit).FirstOrDefault() as T;
        }

    }
}
