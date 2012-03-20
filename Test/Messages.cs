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
	[ProtoInclude(1, typeof(U8NullMessage))]
	[ProtoInclude(2, typeof(S16Message))]
	[ProtoInclude(3, typeof(S32Message))]
	[ProtoInclude(4, typeof(U32Message))]
	[ProtoInclude(5, typeof(S64Message))]
	[ProtoInclude(6, typeof(PrimitivesMessage))]
	[ProtoInclude(7, typeof(ComplexMessage))]
	[ProtoInclude(8, typeof(ByteArrayMessage))]
	[ProtoInclude(9, typeof(IntArrayMessage))]
	[ProtoInclude(10, typeof(StringMessage))]
	abstract class MessageBase
	{
		public abstract void Compare(MessageBase msg);

		protected static Random s_rand = new Random(123);

		public static void ResetSeed()
		{
			s_rand = new Random(123);
		}

		protected static void A(bool b)
		{
			if (!b)
				throw new Exception();
		}

		public static MessageBase[] CreateMessages(Type type, int numMessages)
		{
			var arr = new MessageBase[numMessages];

			for (int i = 0; i < numMessages; ++i)
				arr[i] = (MessageBase)Activator.CreateInstance(type, s_rand);

			return arr;
		}

		static byte[] r64buf = new byte[8];
		protected static long GetRandomInt64(Random random)
		{
			// XXX produces quite big numbers
			random.NextBytes(r64buf);
			return BitConverter.ToInt64(r64buf, 0);
		}
	}

	[Serializable]
	[ProtoContract]
	sealed class U8NullMessage : MessageBase
	{
		[ProtoMember(1)]
		byte m_val;

		public U8NullMessage()
		{
		}

		public U8NullMessage(Random r)
		{
			m_val = 0;
		}

		public override void Compare(MessageBase msg)
		{
			var m = (U8NullMessage)msg;
			A(m_val == m.m_val);
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

		public S16Message(Random r)
		{
			m_val = (short)r.Next();
		}

		public override void Compare(MessageBase msg)
		{
			var m = (S16Message)msg;
			A(m_val == m.m_val);
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

		public S32Message(Random r)
		{
			m_val = (int)r.Next();
		}

		public override void Compare(MessageBase msg)
		{
			var m = (S32Message)msg;
			A(m_val == m.m_val);
		}
	}

	[Serializable]
	[ProtoContract]
	sealed class U32Message : MessageBase
	{
		[ProtoMember(1)]
		uint m_val;

		public U32Message()
		{
		}

		public U32Message(Random r)
		{
			m_val = (uint)r.Next();
		}

		public override void Compare(MessageBase msg)
		{
			var m = (U32Message)msg;
			A(m_val == m.m_val);
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

		public S64Message(Random r)
		{
			m_val = GetRandomInt64(r);
		}

		public override void Compare(MessageBase msg)
		{
			var m = (S64Message)msg;
			A(m_val == m.m_val);
		}
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

		public PrimitivesMessage()
		{
		}

		public PrimitivesMessage(Random r)
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

			m_int = r.Next();

			m_single = (float)r.NextDouble();
			m_double = r.NextDouble();
		}

		public override void Compare(MessageBase msg)
		{
			var m = (PrimitivesMessage)msg;

			A(m_bool == m.m_bool);

			A(m_byte == m.m_byte);
			A(m_sbyte == m.m_sbyte);
			A(m_char == m.m_char);
			A(m_ushort == m.m_ushort);
			A(m_short == m.m_short);
			A(m_uint == m.m_uint);
			A(m_int == m.m_int);
			A(m_ulong == m.m_ulong);
			A(m_long == m.m_long);

			A(m_single == m.m_single);
			A(m_double == m.m_double);
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

		public ByteArrayMessage(Random r)
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

		public override void Compare(MessageBase msg)
		{
			var m = (ByteArrayMessage)msg;

			if (m_byteArr == null)
			{
				A(m_byteArr == m.m_byteArr);
			}
			else
			{
				for (int i = 0; i < m_byteArr.Length; ++i)
					A(m_byteArr[i] == m.m_byteArr[i]);
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

		public IntArrayMessage(Random r)
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
					m_intArr[i] = r.Next();
			}
		}

		public override void Compare(MessageBase msg)
		{
			var m = (IntArrayMessage)msg;

			if (m_intArr == null)
			{
				A(m_intArr == m.m_intArr);
			}
			else
			{
				for (int i = 0; i < m_intArr.Length; ++i)
					A(m_intArr[i] == m.m_intArr[i]);
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

		public StringMessage(Random r)
		{
			int len = r.Next(1000);

			if (len == 0)
				m_string = null;
			else
				m_string = new string((char)r.Next((int)'a', (int)'z'), len - 1);
		}

		public override void Compare(MessageBase msg)
		{
			var m = (StringMessage)msg;

			A(m_string == m.m_string);
		}
	}


	[Serializable]
	[ProtoContract]
	[ProtoInclude(1, typeof(SimpleClass))]
	abstract class SimpleClassBase
	{
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

		public SimpleClass(Random r)
		{
			m_val = (long)r.Next();
		}

		public void Compare(SimpleClass other)
		{
			if (m_val != other.m_val)
				throw new Exception();
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

		public SimpleClass2(Random r)
		{
			m_val = (long)r.Next();
		}

		public void Compare(SimpleClass2 other)
		{
			if (m_val != other.m_val)
				throw new Exception();
		}
	}

	[Serializable]
	[ProtoContract]
	sealed class ComplexMessage : MessageBase
	{
		[ProtoMember(2)]
		S16Message m_msg;

		[ProtoMember(3)]
		int[] m_intArr;

		[ProtoMember(4)]
		SimpleClass m_sealedClass;

		[ProtoMember(5)]
		SimpleClassBase m_abstractMsg;

		[ProtoMember(6)]
		IMyTest m_ifaceMsg;

		public ComplexMessage()
		{
		}

		public ComplexMessage(Random r)
		{
			if (r.Next(100) == 0)
				m_msg = null;
			else
				m_msg = new S16Message(r);

			if (r.Next(100) == 0)
				m_intArr = null;
			else
				m_intArr = new int[r.Next(1, 100)];

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

		public override void Compare(MessageBase msg)
		{
			var m = (ComplexMessage)msg;

			if (m_msg == null)
				A(m_msg == m.m_msg);
			else
				m_msg.Compare(m.m_msg);

			if (m_intArr == null)
			{
				A(m_intArr == m.m_intArr);
			}
			else
			{
				for (int i = 0; i < m_intArr.Length; ++i)
					A(m_intArr[i] == m.m_intArr[i]);
			}

			if (m_sealedClass == null)
				A(m_sealedClass == m.m_sealedClass);
			else
				m_sealedClass.Compare(m.m_sealedClass);

			if (m_abstractMsg == null)
				A(m_abstractMsg == m.m_abstractMsg);
			else
				((SimpleClass)m_abstractMsg).Compare((SimpleClass)m.m_abstractMsg);

			if (m_ifaceMsg == null)
				A(m_ifaceMsg == m.m_ifaceMsg);
			else
				((SimpleClass2)m_ifaceMsg).Compare((SimpleClass2)m.m_ifaceMsg);
		}
	}
}
