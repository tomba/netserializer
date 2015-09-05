using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using ProtoBuf;

namespace Test
{
	[Serializable]
	[ProtoContract]
	[ProtoInclude(1, typeof(U8Message))]
	[ProtoInclude(2, typeof(S16Message))]
	[ProtoInclude(3, typeof(S32Message))]
	[ProtoInclude(4, typeof(S64Message))]
	[ProtoInclude(5, typeof(PrimitivesMessage))]
	[ProtoInclude(6, typeof(ComplexMessage))]
	[ProtoInclude(7, typeof(ByteArrayMessage))]
	[ProtoInclude(8, typeof(IntArrayMessage))]
	[ProtoInclude(9, typeof(StringMessage))]
	[ProtoInclude(10, typeof(DictionaryMessage))]
	[ProtoInclude(11, typeof(StructMessage))]
	[ProtoInclude(12, typeof(DecimalMessage))]
	[ProtoInclude(13, typeof(NullableDecimalMessage))]
	[ProtoInclude(14, typeof(LargeStruct))]
	abstract class MessageBase
	{
		protected static void A(bool b)
		{
			if (!b)
				throw new Exception();
		}
	}

	[Serializable]
	[ProtoContract]
	struct LargeStruct
	{
		static void A(bool b)
		{
			if (!b)
				throw new Exception();
		}

		[ProtoMember(1)]
		ulong m_val1;
		[ProtoMember(2)]
		ulong m_val2;
		[ProtoMember(3)]
		ulong m_val3;
		[ProtoMember(4)]
		ulong m_val4;

		public LargeStruct(MyRandom r)
		{
			m_val1 = r.Next();
			m_val2 = r.Next();
			m_val3 = r.Next();
			m_val4 = r.Next();
		}

		public static LargeStruct Create(MyRandom r)
		{
			return new LargeStruct(r);
		}

		public static void Compare(LargeStruct a, LargeStruct b)
		{
			A(a.m_val1 == b.m_val1);
			A(a.m_val2 == b.m_val2);
			A(a.m_val3 == b.m_val3);
			A(a.m_val4 == b.m_val4);
		}
	}

	[Serializable]
	[ProtoContract]
	sealed class U8Message : MessageBase
	{
		[ProtoMember(1)]
		byte m_val;

		public U8Message()
		{
		}

		public U8Message(MyRandom r)
		{
			m_val = (byte)r.Next();
		}

		public static U8Message Create(MyRandom r)
		{
			return new U8Message(r);
		}

		public static void Compare(U8Message a, U8Message b)
		{
			A(a.m_val == b.m_val);
		}
	}

	[Serializable]
	[ProtoContract]
	sealed class S16Message : MessageBase
	{
		[ProtoMember(1)]
		short m_val;

		public S16Message()
		{
		}

		public S16Message(MyRandom r)
		{
			m_val = (short)r.Next();
		}

		public static S16Message Create(MyRandom r)
		{
			return new S16Message(r);
		}

		public static void Compare(S16Message a, S16Message b)
		{
			A(a.m_val == b.m_val);
		}
	}

	[Serializable]
	[ProtoContract]
	sealed class S32Message : MessageBase
	{
		[ProtoMember(1)]
		int m_val;

		public S32Message()
		{
		}

		public S32Message(MyRandom r)
		{
			m_val = (int)r.Next();
		}

		public static S32Message Create(MyRandom r)
		{
			return new S32Message(r);
		}

		public static void Compare(S32Message a, S32Message b)
		{
			A(a.m_val == b.m_val);
		}
	}

	[Serializable]
	[ProtoContract]
	sealed class DecimalMessage : MessageBase
	{
		[ProtoMember(1)]
		decimal m_val;

		public DecimalMessage()
		{
		}

		public DecimalMessage(MyRandom r)
		{
			int[] bits = new int[4];
			bits[0] = (int)r.Next();
			bits[1] = (int)r.Next();
			bits[2] = (int)r.Next();

			uint exp = ((uint)r.Next(29)) << 16;
			exp |= ((r.Next() & 1) == 0 ? 0u : 1u) << 31;
			bits[3] = (int)exp;

			m_val = new decimal(bits);
		}

		public static DecimalMessage Create(MyRandom r)
		{
			return new DecimalMessage(r);
		}

		public static void Compare(DecimalMessage a, DecimalMessage b)
		{
			A(a.m_val == b.m_val);
		}
	}

	[Serializable]
	[ProtoContract]
	sealed class NullableDecimalMessage : MessageBase
	{
		[ProtoMember(1)]
		decimal? m_val;

		public NullableDecimalMessage()
		{
		}

		public NullableDecimalMessage(MyRandom r)
		{
			if (r.Next(100) != 0)
				return;

			int[] bits = new int[4];
			bits[0] = (int)r.Next();
			bits[1] = (int)r.Next();
			bits[2] = (int)r.Next();

			uint exp = ((uint)r.Next(29)) << 16;
			exp |= ((r.Next() & 1) == 0 ? 0u : 1u) << 31;
			bits[3] = (int)exp;

			m_val = new decimal(bits);
		}

		public static NullableDecimalMessage Create(MyRandom r)
		{
			return new NullableDecimalMessage(r);
		}

		public static void Compare(NullableDecimalMessage a, NullableDecimalMessage b)
		{
			A(a.m_val == b.m_val);
		}
	}

	[Serializable]
	[ProtoContract]
	struct MyStruct1
	{
		[ProtoMember(1)]
		public byte m_byte;
		[ProtoMember(2)]
		public int m_int;
		[ProtoMember(3)]
		public long m_long;
	}

	[Serializable]
	[ProtoContract]
	struct MyStruct2
	{
		[ProtoMember(1)]
		public string m_string;
		[ProtoMember(2)]
		public int m_int;
	}

	[Serializable]
	[ProtoContract]
	sealed class StructMessage : MessageBase
	{
		[ProtoMember(1)]
		MyStruct1 m_struct1;

		[ProtoMember(2)]
		MyStruct2 m_struct2;

		public StructMessage()
		{
		}

		public StructMessage(MyRandom r)
		{
			m_struct1.m_byte = (byte)r.Next();
			m_struct1.m_int = (int)r.Next();
			m_struct1.m_long = (long)r.Next();

			m_struct2.m_string = new string((char)r.Next((int)'a', (int)'z'), r.Next(0, 20));
			m_struct2.m_int = (int)r.Next();
		}

		public static StructMessage Create(MyRandom r)
		{
			return new StructMessage(r);
		}

		public static void Compare(StructMessage a, StructMessage b)
		{
			A(a.m_struct1.Equals(b.m_struct1));
			A(a.m_struct2.Equals(b.m_struct2));
		}
	}

	[Serializable]
	[ProtoContract]
	sealed class S64Message : MessageBase
	{
		[ProtoMember(1)]
		long m_val;

		public S64Message()
		{
		}

		public S64Message(MyRandom r)
		{
			m_val = (long)r.Next();
		}

		public static S64Message Create(MyRandom r)
		{
			return new S64Message(r);
		}

		public static void Compare(S64Message a, S64Message b)
		{
			A(a.m_val == b.m_val);
		}
	}

	enum MyEnum
	{
		Zero = 0,
		One,
		Two,
		Three,
		Four,
		Five,
	}

	[Serializable]
	[ProtoContract]
	sealed class PrimitivesMessage : MessageBase
	{
		[ProtoMember(1)]
		bool m_bool;

		[ProtoMember(2)]
		byte m_byte;
		[ProtoMember(3)]
		sbyte m_sbyte;
		[ProtoMember(4)]
		char m_char;
		[ProtoMember(5)]
		ushort m_ushort;
		[ProtoMember(6)]
		short m_short;
		[ProtoMember(7)]
		uint m_uint;
		[ProtoMember(8)]
		int m_int;
		[ProtoMember(9)]
		ulong m_ulong;
		[ProtoMember(10)]
		long m_long;

		[ProtoMember(11)]
		float m_single;
		[ProtoMember(12)]
		double m_double;

		[ProtoMember(13)]
		MyEnum m_enum;

		[ProtoMember(14)]
		DateTime m_date;

		public PrimitivesMessage()
		{
		}

		public PrimitivesMessage(MyRandom r)
		{
			m_bool = (r.Next() & 1) == 1;
			m_byte = (byte)r.Next();
			m_sbyte = (sbyte)r.Next();
			m_char = (char)r.Next();
			m_ushort = (ushort)r.Next();
			m_short = (short)r.Next();
			m_uint = (uint)r.Next();
			m_int = (int)r.Next();
			m_ulong = (ulong)r.Next();
			m_long = (long)r.Next();

			m_int = (int)r.Next();

			m_single = (float)r.NextDouble();
			m_double = r.NextDouble();

			m_enum = (MyEnum)r.Next(0, 6);

			m_date = DateTime.Now;
		}

		public static PrimitivesMessage Create(MyRandom r)
		{
			return new PrimitivesMessage(r);
		}

		public static void Compare(PrimitivesMessage a, PrimitivesMessage b)
		{
			A(a.m_bool == b.m_bool);

			A(a.m_byte == b.m_byte);
			A(a.m_sbyte == b.m_sbyte);
			A(a.m_char == b.m_char);
			A(a.m_ushort == b.m_ushort);
			A(a.m_short == b.m_short);
			A(a.m_uint == b.m_uint);
			A(a.m_int == b.m_int);
			A(a.m_ulong == b.m_ulong);
			A(a.m_long == b.m_long);

			A(a.m_single == b.m_single);
			A(a.m_double == b.m_double);

			A(a.m_enum == b.m_enum);

			A(a.m_date == b.m_date);
		}
	}

	[Serializable]
	sealed class BoxedPrimitivesMessage : MessageBase
	{
		object m_bool;

		object m_byte;
		object m_int;
		object m_long;

		object m_enum;

		public BoxedPrimitivesMessage()
		{
		}

		public BoxedPrimitivesMessage(MyRandom r)
		{
			m_bool = (r.Next() & 1) == 1;
			m_byte = (byte)r.Next();
			m_int = (int)r.Next();
			m_long = (long)r.Next();

			m_int = (int)r.Next();

			m_enum = (MyEnum)r.Next(0, 6);
		}

		public static BoxedPrimitivesMessage Create(MyRandom r)
		{
			return new BoxedPrimitivesMessage(r);
		}

		public static void Compare(BoxedPrimitivesMessage a, BoxedPrimitivesMessage b)
		{
			A((bool)a.m_bool == (bool)b.m_bool);

			A((byte)a.m_byte == (byte)b.m_byte);
			A((int)a.m_int == (int)b.m_int);
			A((long)a.m_long == (long)b.m_long);

			A((MyEnum)a.m_enum == (MyEnum)b.m_enum);
		}
	}

	[Serializable]
	[ProtoContract]
	sealed class ByteArrayMessage : MessageBase
	{
		[ProtoMember(1)]
		byte[] m_byteArr;

		public ByteArrayMessage()
		{
		}

		public ByteArrayMessage(MyRandom r)
		{
			int len = r.Next(100000);

			if (len == 0)
			{
				m_byteArr = null;
			}
			else
			{
				m_byteArr = new byte[len - 1];
				for (int i = 0; i < m_byteArr.Length; ++i)
					m_byteArr[i] = (byte)i;
			}
		}

		public static ByteArrayMessage Create(MyRandom r)
		{
			return new ByteArrayMessage(r);
		}

		public static void Compare(ByteArrayMessage a, ByteArrayMessage b)
		{
			if (a.m_byteArr == null)
			{
				A(a.m_byteArr == b.m_byteArr);
			}
			else
			{
				for (int i = 0; i < a.m_byteArr.Length; ++i)
					A(a.m_byteArr[i] == b.m_byteArr[i]);
			}
		}
	}

	[Serializable]
	[ProtoContract]
	sealed class IntArrayMessage : MessageBase
	{
		[ProtoMember(1)]
		int[] m_intArr;

		public IntArrayMessage()
		{
		}

		public IntArrayMessage(MyRandom r)
		{
			int len = r.Next(100000);

			if (len == 0)
			{
				m_intArr = null;
			}
			else
			{
				m_intArr = new int[len - 1];
				for (int i = 0; i < m_intArr.Length; ++i)
					m_intArr[i] = (int)r.Next();
			}
		}

		public static IntArrayMessage Create(MyRandom r)
		{
			return new IntArrayMessage(r);
		}

		public static void Compare(IntArrayMessage a, IntArrayMessage b)
		{
			if (a.m_intArr == null)
			{
				A(a.m_intArr == b.m_intArr);
			}
			else
			{
				for (int i = 0; i < a.m_intArr.Length; ++i)
					A(a.m_intArr[i] == b.m_intArr[i]);
			}
		}
	}

	[Serializable]
	[ProtoContract]
	sealed class StringMessage : MessageBase
	{
		[ProtoMember(1)]
		string m_string;

		public StringMessage()
		{
		}

		public StringMessage(MyRandom r)
		{
			int len = r.Next(100);

			if (len == 0)
				m_string = null;
			else
				//m_string = new string((char)r.Next(0xD7FF), len - 1);
				m_string = new string((char)r.Next((int)'a', (int)'z'), len - 1);
		}

		public static StringMessage Create(MyRandom r)
		{
			return new StringMessage(r);
		}

		public static void Compare(StringMessage a, StringMessage b)
		{
			A(a.m_string == b.m_string);
		}
	}


	[Serializable]
	[ProtoContract]
	[ProtoInclude(1, typeof(SimpleClass))]
	abstract class SimpleClassBase
	{
		[ProtoMember(2)]
		int m_val;

		protected SimpleClassBase()
		{
		}

		protected SimpleClassBase(MyRandom r)
		{
			m_val = (int)r.Next();
		}

		public void Compare(SimpleClassBase other)
		{
			if (m_val != other.m_val)
				throw new Exception();
		}
	}

	[Serializable]
	[ProtoContract]
	sealed class SimpleClass : SimpleClassBase
	{
		[ProtoMember(1)]
		long m_val;

		public SimpleClass()
		{
		}

		public SimpleClass(MyRandom r)
			: base(r)
		{
			m_val = (long)r.Next();
		}

		public static SimpleClass Create(MyRandom r)
		{
			return new SimpleClass(r);
		}

		public void Compare(SimpleClass other)
		{
			if (m_val != other.m_val)
				throw new Exception();

			base.Compare(other);
		}
	}

	[ProtoContract]
	[ProtoInclude(1, typeof(SimpleClass2))]
	interface IMyTest
	{
	}

	[Serializable]
	[ProtoContract]
	sealed class SimpleClass2 : IMyTest
	{
		[ProtoMember(1)]
		long m_val;

		public SimpleClass2()
		{
		}

		public SimpleClass2(MyRandom r)
		{
			m_val = (long)r.Next();
		}

		public static SimpleClass2 Create(MyRandom r)
		{
			return new SimpleClass2(r);
		}

		public void Compare(SimpleClass2 other)
		{
			if (m_val != other.m_val)
				throw new Exception();
		}
	}

	[Serializable]
	[ProtoContract]
	sealed class DictionaryMessage : MessageBase
	{
		[ProtoMember(1)]
		Dictionary<int, int> m_intMap;

		[ProtoMember(2)]
		Dictionary<string, SimpleClass2> m_obMap;

		public DictionaryMessage()
		{
		}

		public DictionaryMessage(MyRandom r)
		{
			var len = r.Next(0, 1000);
			if (len > 0)
			{
				m_intMap = new Dictionary<int, int>(len);
				for (int i = 0; i < len; ++i)
					m_intMap[(int)r.Next()] = (int)r.Next();
			}

			len = r.Next(0, 1000);
			if (len > 0)
			{
				m_obMap = new Dictionary<string, SimpleClass2>();
				for (int i = 0; i < len; ++i)
				{
					var str = i.ToString();
					m_obMap[str] = new SimpleClass2(r);
				}
			}
		}

		public static DictionaryMessage Create(MyRandom r)
		{
			return new DictionaryMessage(r);
		}

		public static void Compare(DictionaryMessage a, DictionaryMessage b)
		{
			if (a.m_intMap == null)
				A(a.m_intMap == b.m_intMap);
			else
			{
				A(a.m_intMap.Count == b.m_intMap.Count);
				foreach (var kvp in a.m_intMap)
					A(kvp.Value == b.m_intMap[kvp.Key]);
			}

			if (a.m_obMap == null)
				A(a.m_obMap == b.m_obMap);
			else
			{
				A(a.m_obMap.Count == b.m_obMap.Count);
				foreach (var kvp in a.m_obMap)
					kvp.Value.Compare(b.m_obMap[kvp.Key]);
			}
		}
	}

	[Serializable]
	[ProtoContract]
	sealed class ComplexMessage : MessageBase
	{
		[ProtoMember(1)]
		S16Message m_msg;

		[ProtoMember(2)]
		SimpleClass m_sealedClass;

		[ProtoMember(3)]
		SimpleClassBase m_abstractMsg;

		[ProtoMember(4)]
		IMyTest m_ifaceMsg;

		public ComplexMessage()
		{
		}

		public ComplexMessage(MyRandom r)
		{
			if (r.Next(100) == 0)
				m_msg = null;
			else
				m_msg = new S16Message(r);

			if (r.Next(100) == 0)
				m_sealedClass = null;
			else
				m_sealedClass = new SimpleClass(r);

			if (r.Next(100) == 0)
				m_abstractMsg = null;
			else
				m_abstractMsg = new SimpleClass(r);

			if (r.Next(100) == 0)
				m_ifaceMsg = null;
			else
				m_ifaceMsg = new SimpleClass2(r);
		}

		public static ComplexMessage Create(MyRandom r)
		{
			return new ComplexMessage(r);
		}

		public static void Compare(ComplexMessage a, ComplexMessage b)
		{
			if (a.m_msg == null)
				A(a.m_msg == b.m_msg);
			else
				S16Message.Compare(a.m_msg, b.m_msg);

			if (a.m_sealedClass == null)
				A(a.m_sealedClass == b.m_sealedClass);
			else
				a.m_sealedClass.Compare(b.m_sealedClass);

			if (a.m_abstractMsg == null)
				A(a.m_abstractMsg == b.m_abstractMsg);
			else
				((SimpleClass)a.m_abstractMsg).Compare((SimpleClass)b.m_abstractMsg);

			if (a.m_ifaceMsg == null)
				A(a.m_ifaceMsg == b.m_ifaceMsg);
			else
				((SimpleClass2)a.m_ifaceMsg).Compare((SimpleClass2)b.m_ifaceMsg);
		}
	}

	[Serializable]
	sealed class TriDimArrayCustomSerializersMessage : MessageBase
	{
		int[,,] m_int3Arr;

		public TriDimArrayCustomSerializersMessage()
		{
		}

		public TriDimArrayCustomSerializersMessage(MyRandom r)
		{
			int lx = r.Next(100) + 1;
			int ly = r.Next(70) + 1;
			int lz = r.Next(40) + 1;

			m_int3Arr = new int[lz, ly, lx];

			for (int z = 0; z < lz; ++z)
				for (int y = 0; y < ly; ++y)
					for (int x = 0; x < lx; ++x)
						m_int3Arr[z, y, x] = (int)r.Next();
		}

		public static TriDimArrayCustomSerializersMessage Create(MyRandom r)
		{
			return new TriDimArrayCustomSerializersMessage(r);
		}

		public static void Compare(TriDimArrayCustomSerializersMessage a, TriDimArrayCustomSerializersMessage b)
		{
			int lz = a.m_int3Arr.GetLength(0);
			int ly = a.m_int3Arr.GetLength(1);
			int lx = a.m_int3Arr.GetLength(2);

			for (int z = 0; z < lz; ++z)
				for (int y = 0; y < ly; ++y)
					for (int x = 0; x < lx; ++x)
						A(a.m_int3Arr[z, y, x] == b.m_int3Arr[z, y, x]);
		}
	}
}
