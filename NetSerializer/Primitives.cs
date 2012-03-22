using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace NetSerializer
{
	public static class Primitives
	{
		internal static MethodInfo GetWritePrimitive(Type type)
		{
			if (type.IsEnum)
				type = type.GetEnumUnderlyingType();

			var mi = typeof(Primitives).GetMethod("WritePrimitive", BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null,
				new Type[] { typeof(Stream), type }, null);

			return mi;
		}

		internal static MethodInfo GetReadPrimitive(Type type)
		{
			if (!type.IsByRef)
				throw new Exception();

			if (type.GetElementType().IsEnum)
				type = type.GetElementType().GetEnumUnderlyingType().MakeByRefType();

			MethodInfo mi = typeof(Primitives).GetMethod("ReadPrimitive", BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null,
				new Type[] { typeof(Stream), type }, null);

			return mi;
		}

		static uint EncodeZigZag32(int n)
		{
			return (uint)((n << 1) ^ (n >> 31));
		}

		static ulong EncodeZigZag64(long n)
		{
			return (ulong)((n << 1) ^ (n >> 63));
		}

		static int DecodeZigZag32(uint n)
		{
			return (int)(n >> 1) ^ -(int)(n & 1);
		}

		static long DecodeZigZag64(ulong n)
		{
			return (long)(n >> 1) ^ -(long)(n & 1);
		}

		static uint ReadVarint32(Stream stream)
		{
			int result = 0;
			int offset = 0;

			for (; offset < 32; offset += 7)
			{
				int b = stream.ReadByte();
				if (b == -1)
					throw new Exception();

				result |= (b & 0x7f) << offset;

				if ((b & 0x80) == 0)
					return (uint)result;
			}

			throw new Exception();
		}

		static void WriteVarint32(Stream stream, uint value)
		{
			for (; value >= 0x80u; value >>= 7)
				stream.WriteByte((byte)(value | 0x80u));

			stream.WriteByte((byte)value);
		}

		static ulong ReadVarint64(Stream stream)
		{
			long result = 0;
			int offset = 0;

			for (; offset < 64; offset += 7)
			{
				int b = stream.ReadByte();
				if (b == -1)
					throw new Exception();

				result |= ((long)(b & 0x7f)) << offset;

				if ((b & 0x80) == 0)
					return (ulong)result;
			}

			throw new Exception();
		}

		static void WriteVarint64(Stream stream, ulong value)
		{
			for (; value >= 0x80u; value >>= 7)
				stream.WriteByte((byte)(value | 0x80u));

			stream.WriteByte((byte)value);
		}


		public static void WritePrimitive(Stream stream, bool value)
		{
			stream.WriteByte(value ? (byte)1 : (byte)0);
		}

		public static void ReadPrimitive(Stream stream, out bool value)
		{
			var b = stream.ReadByte();
			value = b != 0;
		}

		public static void WritePrimitive(Stream stream, byte value)
		{
			stream.WriteByte(value);
		}

		public static void ReadPrimitive(Stream stream, out byte value)
		{
			value = (byte)stream.ReadByte();
		}

		public static void WritePrimitive(Stream stream, sbyte value)
		{
			stream.WriteByte((byte)value);
		}

		public static void ReadPrimitive(Stream stream, out sbyte value)
		{
			value = (sbyte)stream.ReadByte();
		}

		public static void WritePrimitive(Stream stream, char value)
		{
			WriteVarint32(stream, value);
		}

		public static void ReadPrimitive(Stream stream, out char value)
		{
			value = (char)ReadVarint32(stream);
		}

		public static void WritePrimitive(Stream stream, ushort value)
		{
			WriteVarint32(stream, value);
		}

		public static void ReadPrimitive(Stream stream, out ushort value)
		{
			value = (ushort)ReadVarint32(stream);
		}

		public static void WritePrimitive(Stream stream, short value)
		{
			WriteVarint32(stream, EncodeZigZag32(value));
		}

		public static void ReadPrimitive(Stream stream, out short value)
		{
			value = (short)DecodeZigZag32(ReadVarint32(stream));
		}

		public static void WritePrimitive(Stream stream, uint value)
		{
			WriteVarint32(stream, value);
		}

		public static void ReadPrimitive(Stream stream, out uint value)
		{
			value = ReadVarint32(stream);
		}

		public static void WritePrimitive(Stream stream, int value)
		{
			WriteVarint32(stream, EncodeZigZag32(value));
		}

		public static void ReadPrimitive(Stream stream, out int value)
		{
			value = DecodeZigZag32(ReadVarint32(stream));
		}

		public static void WritePrimitive(Stream stream, ulong value)
		{
			WriteVarint64(stream, value);
		}

		public static void ReadPrimitive(Stream stream, out ulong value)
		{
			value = ReadVarint64(stream);
		}

		public static void WritePrimitive(Stream stream, long value)
		{
			WriteVarint64(stream, EncodeZigZag64(value));
		}

		public static void ReadPrimitive(Stream stream, out long value)
		{
			value = DecodeZigZag64(ReadVarint64(stream));
		}

		public static unsafe void WritePrimitive(Stream stream, float value)
		{
			uint v = *(uint*)(&value);
			WriteVarint32(stream, v);
		}

		public static unsafe void ReadPrimitive(Stream stream, out float value)
		{
			uint v = ReadVarint32(stream);
			value = *(float*)(&v);
		}

		public static unsafe void WritePrimitive(Stream stream, double value)
		{
			ulong v = *(ulong*)(&value);
			WriteVarint64(stream, v);
		}

		public static unsafe void ReadPrimitive(Stream stream, out double value)
		{
			ulong v = ReadVarint64(stream);
			value = *(double*)(&v);
		}

		public static unsafe void WritePrimitive(Stream stream, DateTime value)
		{
			long v = value.ToBinary();
			WritePrimitive(stream, v);
		}

		public static unsafe void ReadPrimitive(Stream stream, out DateTime value)
		{
			long v;
			ReadPrimitive(stream, out v);
			value = DateTime.FromBinary(v);
		}

		public static void WritePrimitive(Stream stream, string value)
		{
			if (value == null)
			{
				WritePrimitive(stream, (uint)0);
				return;
			}

#if GC_NICE_VERSION
			WritePrimitive(stream, (uint)value.Length + 1);

			foreach (char c in value)
				WritePrimitive(stream, c);
#else

			var encoding = new UTF8Encoding(false, true);

			int len = encoding.GetByteCount(value);

			WritePrimitive(stream, (uint)len + 1);

			var buf = new byte[len];

			encoding.GetBytes(value, 0, value.Length, buf, 0);

			stream.Write(buf, 0, len);
#endif
		}

		public static void ReadPrimitive(Stream stream, out string value)
		{
			uint len;
			ReadPrimitive(stream, out len);

			if (len == 0)
			{
				value = null;
				return;
			}
			else if (len == 1)
			{
				value = string.Empty;
				return;
			}

			len -= 1;

#if GC_NICE_VERSION
			var arr = new char[len];
			for (uint i = 0; i < len; ++i)
				ReadPrimitive(stream, out arr[i]);

			value = new string(arr);
#else

			var encoding = new UTF8Encoding(false, true);

			var buf = new byte[len];

			int l = 0;

			while (l < len)
			{
				int r = stream.Read(buf, l, (int)len - l);
				if (r == 0)
					throw new Exception();
				l += r;
			}

			value = encoding.GetString(buf);
#endif
		}

		public static void WritePrimitive(Stream stream, byte[] value)
		{
			if (value == null)
			{
				WritePrimitive(stream, (uint)0);
				return;
			}

			WritePrimitive(stream, (uint)value.Length + 1);

			stream.Write(value, 0, value.Length);
		}

		static readonly byte[] s_emptyByteArray = new byte[0];

		public static void ReadPrimitive(Stream stream, out byte[] value)
		{
			uint len;
			ReadPrimitive(stream, out len);

			if (len == 0)
			{
				value = null;
				return;
			}
			else if (len == 1)
			{
				value = s_emptyByteArray;
				return;
			}

			len -= 1;

			value = new byte[len];
			int l = 0;

			while (l < len)
			{
				int r = stream.Read(value, l, (int)len - l);
				if (r == 0)
					throw new Exception();
				l += r;
			}
		}
	}
}
