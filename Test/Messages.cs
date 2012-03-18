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
	[ProtoInclude(1, typeof(Message))]
	[ProtoInclude(2, typeof(LongMessage))]
	[ProtoInclude(3, typeof(SimpleMessage))]
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

		public static MessageBase[] CreateMessages(int numMessages)
		{
			var arr = new MessageBase[numMessages];

			for (int i = 0; i < numMessages; ++i)
				arr[i] = new Message(s_rand);

			return arr;
		}

		public static MessageBase[] CreateSimpleMessages(int numMessages)
		{
			var arr = new MessageBase[numMessages];

			for (int i = 0; i < numMessages; ++i)
				arr[i] = new SimpleMessage(s_rand);

			return arr;
		}

		public static MessageBase[] CreateLongMessages(int numMessages)
		{
			var arr = new MessageBase[numMessages];

			for (int i = 0; i < numMessages; ++i)
				arr[i] = new LongMessage(s_rand);

			return arr;
		}
	}

	[Serializable]
	[ProtoContract]
	sealed class SimpleMessage : MessageBase
	{
		[ProtoMember(1)]
		short m_val;

		public SimpleMessage()
		{
		}

		public SimpleMessage(Random r)
		{
			m_val = (short)r.Next();
		}

		public override void Compare(MessageBase msg)
		{
			var m = (SimpleMessage)msg;
			A(m_val == m.m_val);
		}
	}

	[Serializable]
	[ProtoContract]
	sealed class LongMessage : MessageBase
	{
		[ProtoMember(1)]
		byte[] m_byteArr;
		[ProtoMember(2)]
		int[] m_intArr;

		public LongMessage()
		{
		}

		public LongMessage(Random r)
		{
			m_byteArr = new byte[r.Next(10000, 100000)];
			r.NextBytes(m_byteArr);

			m_intArr = new int[r.Next(10000, 100000)];
			for (int i = 0; i < m_intArr.Length; ++i)
				m_intArr[i] = r.Next();
		}

		public override void Compare(MessageBase msg)
		{
			var m = (LongMessage)msg;

			for (int i = 0; i < m_byteArr.Length; ++i)
				A(m_byteArr[i] == m.m_byteArr[i]);

			for (int i = 0; i < m_intArr.Length; ++i)
				A(m_intArr[i] == m.m_intArr[i]);
		}
	}

	[Serializable]
	[ProtoContract]
	sealed class SimpleSealedClass
	{
		[ProtoMember(1)]
		long m_val;

		public SimpleSealedClass()
		{
		}

		public SimpleSealedClass(Random r)
		{
			m_val = (long)r.Next();
		}

		public void Compare(SimpleSealedClass msg)
		{
			var m = (SimpleSealedClass)msg;
			if (m_val != m.m_val)
				throw new Exception();
		}
	}

	[Serializable]
	[ProtoContract]
	sealed class Message : MessageBase
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
		string m_string;

		[ProtoMember(14)]
		SimpleMessage m_msg;

		[ProtoMember(15)]
		int[] m_intArr;

		[ProtoMember(16)]
		SimpleSealedClass m_sealedClass;

		public Message()
		{
		}

		public Message(Random r)
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

			m_string = new string((char)r.Next((int)'a', (int)'z'), r.Next(2, 100));

			m_msg = new SimpleMessage(r);

			m_intArr = new int[r.Next(1, 100)];

			m_sealedClass = new SimpleSealedClass(r);
		}

		public override void Compare(MessageBase msg)
		{
			var m = (Message)msg;

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

			A(m_string == m.m_string);

			m_msg.Compare(m.m_msg);

			for (int i = 0; i < m_intArr.Length; ++i)
				A(m_intArr[i] == m.m_intArr[i]);

			m_sealedClass.Compare(m.m_sealedClass);
		}
	}
}
