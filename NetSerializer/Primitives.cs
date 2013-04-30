/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NetSerializer
{
	public static class Primitives
	{
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
					throw new EndOfStreamException();

				result |= (b & 0x7f) << offset;

				if ((b & 0x80) == 0)
					return (uint)result;
			}

			throw new InvalidDataException();
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
					throw new EndOfStreamException();

				result |= ((long)(b & 0x7f)) << offset;

				if ((b & 0x80) == 0)
					return (ulong)result;
			}

			throw new InvalidDataException();
		}

		static void WriteVarint64(Stream stream, ulong value)
		{
			for (; value >= 0x80u; value >>= 7)
				stream.WriteByte((byte)(value | 0x80u));

			stream.WriteByte((byte)value);
		}


		public static void WritePrimitive(Stream stream, bool value, ObjectList objList)
		{
			stream.WriteByte(value ? (byte)1 : (byte)0);
		}

		public static void ReadPrimitive(Stream stream, out bool value, ObjectList objList)
		{
			var b = stream.ReadByte();
			value = b != 0;
		}

		public static void WritePrimitive(Stream stream, byte value, ObjectList objList)
		{
			stream.WriteByte(value);
		}

		public static void ReadPrimitive(Stream stream, out byte value, ObjectList objList)
		{
			value = (byte)stream.ReadByte();
		}

		public static void WritePrimitive(Stream stream, sbyte value, ObjectList objList)
		{
			stream.WriteByte((byte)value);
		}

		public static void ReadPrimitive(Stream stream, out sbyte value, ObjectList objList)
		{
			value = (sbyte)stream.ReadByte();
		}

		public static void WritePrimitive(Stream stream, char value, ObjectList objList)
		{
			WriteVarint32(stream, value);
		}

		public static void ReadPrimitive(Stream stream, out char value, ObjectList objList)
		{
			value = (char)ReadVarint32(stream);
		}

		public static void WritePrimitive(Stream stream, ushort value, ObjectList objList)
		{
			WriteVarint32(stream, value);
		}

		public static void ReadPrimitive(Stream stream, out ushort value, ObjectList objList)
		{
			value = (ushort)ReadVarint32(stream);
		}

		public static void WritePrimitive(Stream stream, short value, ObjectList objList)
		{
			WriteVarint32(stream, EncodeZigZag32(value));
		}

		public static void ReadPrimitive(Stream stream, out short value, ObjectList objList)
		{
			value = (short)DecodeZigZag32(ReadVarint32(stream));
		}

		public static void WritePrimitive(Stream stream, uint value, ObjectList objList)
		{
			WriteVarint32(stream, value);
		}

		public static void ReadPrimitive(Stream stream, out uint value, ObjectList objList)
		{
			value = ReadVarint32(stream);
		}

		public static void WritePrimitive(Stream stream, int value, ObjectList objList)
		{
			WriteVarint32(stream, EncodeZigZag32(value));
		}

		public static void ReadPrimitive(Stream stream, out int value, ObjectList objList)
		{
			value = DecodeZigZag32(ReadVarint32(stream));
		}

		public static void WritePrimitive(Stream stream, ulong value, ObjectList objList)
		{
			WriteVarint64(stream, value);
		}

		public static void ReadPrimitive(Stream stream, out ulong value, ObjectList objList)
		{
			value = ReadVarint64(stream);
		}

		public static void WritePrimitive(Stream stream, long value, ObjectList objList)
		{
			WriteVarint64(stream, EncodeZigZag64(value));
		}

		public static void ReadPrimitive(Stream stream, out long value, ObjectList objList)
		{
			value = DecodeZigZag64(ReadVarint64(stream));
		}

#if !NO_UNSAFE
		public static unsafe void WritePrimitive(Stream stream, float value, ObjectList objList)
		{
			uint v = *(uint*)(&value);
			WriteVarint32(stream, v);
		}

		public static unsafe void ReadPrimitive(Stream stream, out float value, ObjectList objList)
		{
			uint v = ReadVarint32(stream);
			value = *(float*)(&v);
		}

		public static unsafe void WritePrimitive(Stream stream, double value, ObjectList objList)
		{
			ulong v = *(ulong*)(&value);
			WriteVarint64(stream, v);
		}

		public static unsafe void ReadPrimitive(Stream stream, out double value, ObjectList objList)
		{
			ulong v = ReadVarint64(stream);
			value = *(double*)(&v);
		}
#else
		public static void WritePrimitive(Stream stream, float value, ObjectList objList)
		{
			WritePrimitive(stream, (double)value);
		}

		public static void ReadPrimitive(Stream stream, out float value, ObjectList objList)
		{
			double v;
			ReadPrimitive(stream, out v);
			value = (float)v;
		}

		public static void WritePrimitive(Stream stream, double value, ObjectList objList)
		{
			ulong v = (ulong)BitConverter.DoubleToInt64Bits(value);
			WriteVarint64(stream, v);
		}

		public static void ReadPrimitive(Stream stream, out double value, ObjectList objList)
		{
			ulong v = ReadVarint64(stream);
			value = BitConverter.Int64BitsToDouble((long)v);
		}
#endif

		public static void WritePrimitive(Stream stream, DateTime value, ObjectList objList)
		{
			long v = value.ToBinary();
			WritePrimitive(stream, v, objList);
		}

		public static void ReadPrimitive(Stream stream, out DateTime value, ObjectList objList)
		{
			long v;
			ReadPrimitive(stream, out v, objList);
			value = DateTime.FromBinary(v);
		}

		public static void WritePrimitive(Stream stream, string value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(stream, (uint)0, objList);
				return;
			}

			if (objList != null)
				objList.Add(value);

#if USE_UTF8_STRING
			var encoding = new UTF8Encoding(false, true);
			int len = encoding.GetByteCount(value);

			WritePrimitive(stream, (uint)len + 1, objList);

			var buf = new byte[len];

			encoding.GetBytes(value, 0, value.Length, buf, 0);
			stream.Write(buf, 0, len);
#else
			WritePrimitive(stream, (uint)value.Length + 1, objList);

			foreach (var c in value)
				WritePrimitive(stream, c, objList);
#endif
		}

		public static void ReadPrimitive(Stream stream, out string value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(stream, out len, objList);

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

			len--;

#if USE_UTF8_STRING
			var encoding = new UTF8Encoding(false, true);
			var buf = new byte[len];
			int l = 0;

			while (l < len)
			{
				int r = stream.Read(buf, l, (int)len - l);
				if (r == 0)
					throw new EndOfStreamException();
				l += r;
			}
			value = encoding.GetString(buf);
#else
			var arr = new char[len];
			for (uint i = 0; i < len; ++i)
				ReadPrimitive(stream, out arr[i], objList);

			value = new string(arr);
#endif
			if (objList != null)
				objList.Add(value);

		}

		public static void WritePrimitive(Stream stream, byte[] value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			WritePrimitive(stream, (uint)value.Length + 1, objList);
			stream.Write(value, 0, value.Length);
		}

		static readonly byte[] s_emptyByteArray = new byte[0];

		public static void ReadPrimitive(Stream stream, out byte[] value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(stream, out len, objList);

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

			len--;

			value = new byte[len];
			int l = 0;

			while (l < len)
			{
				int r = stream.Read(value, l, (int)len - l);
				if (r == 0)
					throw new EndOfStreamException();
				l += r;
			}
			if (objList != null)
				objList.Add(value);
		}


		public static void WritePrimitive(Stream stream, int[] value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			WritePrimitive(stream, (uint)value.Length + 1, objList);
			for (uint i = 0; i < value.Length; ++i)
				WritePrimitive(stream, value[i], objList);
		}

		static readonly int[] s_emptyIntArray = new int[0];

		public static void ReadPrimitive(Stream stream, out int[] value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}
			else if (len == 1)
			{
				value = s_emptyIntArray;
				return;
			}

			len--;

			value = new int[len];
			for (uint i = 0; i < len; ++i)
				ReadPrimitive(stream, out value[i], objList);
			if (objList != null)
				objList.Add(value);
		}


		public static void WritePrimitive(Stream stream, TimeSpan value, ObjectList objList)
		{
			long v = value.Ticks;
			WritePrimitive(stream, v, objList);
		}

		public static void ReadPrimitive(Stream stream, out TimeSpan value, ObjectList objList)
		{
			long v;
			ReadPrimitive(stream, out v, objList);
			value = TimeSpan.FromTicks(v);
		}


		public static void WritePrimitive(Stream stream, DateTimeOffset value, ObjectList objList)
		{
			long v = value.DateTime.ToBinary();
			long o = value.Offset.Ticks;
			WritePrimitive(stream, v, objList);
			WritePrimitive(stream, o, objList);
		}

		public static void ReadPrimitive(Stream stream, out DateTimeOffset value, ObjectList objList)
		{
			long v,o;
			ReadPrimitive(stream, out v, objList);
			ReadPrimitive(stream, out o, objList);
			value = new DateTimeOffset(DateTime.FromBinary(v), TimeSpan.FromTicks(o));
		}


		public static void WritePrimitive(Stream stream, decimal value, ObjectList objList)
		{
			int[] v = Decimal.GetBits(value);
			WritePrimitive(stream, v, null);
		}

		public static void ReadPrimitive(Stream stream, out decimal value, ObjectList objList)
		{
			int[] v;
			ReadPrimitive(stream, out v, null);
			value = new Decimal(v);
		}

		public static void WritePrimitive(Stream stream, Guid value, ObjectList objList)
		{
			byte[] v = value.ToByteArray();
			WritePrimitive(stream, v, null);
		}

		public static void ReadPrimitive(Stream stream, out Guid value, ObjectList objList)
		{
			byte[] v;
			ReadPrimitive(stream, out v, objList);
			value = new Guid(v);
		}


		public static void WritePrimitive<T1>(Stream stream, Tuple<T1> value, ObjectList objList)
		{
			if (objList != null)
				objList.Add(value);

			NetSerializer.Serializer.Serialize(stream, value.Item1, objList);
		}

		public static void ReadPrimitive<T1>(Stream stream, out Tuple<T1> value, ObjectList objList)
		{
			T1 item1 = (T1)NetSerializer.Serializer.Deserialize(stream, objList);
			value = new Tuple<T1>(item1);
			if (objList != null)
				objList.Add(value);
		}


		public static void WritePrimitive<T1,T2>(Stream stream, Tuple<T1,T2> value, ObjectList objList)
		{
			if (objList != null)
				objList.Add(value);

			NetSerializer.Serializer.Serialize(stream, value.Item1, objList);
			NetSerializer.Serializer.Serialize(stream, value.Item2, objList);
		}

		public static void ReadPrimitive<T1,T2>(Stream stream, out Tuple<T1,T2> value, ObjectList objList)
		{
			T1 item1 = (T1)NetSerializer.Serializer.Deserialize(stream, objList);
			T2 item2 = (T2)NetSerializer.Serializer.Deserialize(stream, objList);
			value = new Tuple<T1,T2>(item1,item2);
			if (objList != null)
				objList.Add(value);
		}

		public static void WritePrimitive<T1,T2,T3>(Stream stream, Tuple<T1,T2,T3> value, ObjectList objList)
		{
			if (objList != null)
				objList.Add(value);

			NetSerializer.Serializer.Serialize(stream, value.Item1, objList);
			NetSerializer.Serializer.Serialize(stream, value.Item2, objList);
			NetSerializer.Serializer.Serialize(stream, value.Item3, objList);
		}

		public static void ReadPrimitive<T1,T2,T3>(Stream stream, out Tuple<T1,T2,T3> value, ObjectList objList)
		{
			T1 item1 = (T1)NetSerializer.Serializer.Deserialize(stream, objList);
			T2 item2 = (T2)NetSerializer.Serializer.Deserialize(stream, objList);
			T3 item3 = (T3)NetSerializer.Serializer.Deserialize(stream, objList);
			value = new Tuple<T1,T2,T3>(item1, item2, item3);
			if (objList != null)
				objList.Add(value);
		}


		public static void WritePrimitive<T1, T2, T3,T4>(Stream stream, Tuple<T1, T2, T3,T4> value, ObjectList objList)
		{
			if (objList != null)
				objList.Add(value);

			NetSerializer.Serializer.Serialize(stream, value.Item1, objList);
			NetSerializer.Serializer.Serialize(stream, value.Item2, objList);
			NetSerializer.Serializer.Serialize(stream, value.Item3, objList);
			NetSerializer.Serializer.Serialize(stream, value.Item4, objList);
		}

		public static void ReadPrimitive<T1, T2, T3,T4>(Stream stream, out Tuple<T1, T2, T3,T4> value, ObjectList objList)
		{
			T1 item1 = (T1)NetSerializer.Serializer.Deserialize(stream, objList);
			T2 item2 = (T2)NetSerializer.Serializer.Deserialize(stream, objList);
			T3 item3 = (T3)NetSerializer.Serializer.Deserialize(stream, objList);
			T4 item4 = (T4)NetSerializer.Serializer.Deserialize(stream, objList);
			value = new Tuple<T1, T2, T3,T4>(item1, item2, item3, item4);
			if (objList != null)
				objList.Add(value);
		}


		public static void WritePrimitive<T1, T2, T3, T4,T5>(Stream stream, Tuple<T1, T2, T3, T4,T5> value, ObjectList objList)
		{
			if (objList != null)
				objList.Add(value);

			NetSerializer.Serializer.Serialize(stream, value.Item1, objList);
			NetSerializer.Serializer.Serialize(stream, value.Item2, objList);
			NetSerializer.Serializer.Serialize(stream, value.Item3, objList);
			NetSerializer.Serializer.Serialize(stream, value.Item4, objList);
			NetSerializer.Serializer.Serialize(stream, value.Item5, objList);
		}

		public static void ReadPrimitive<T1, T2, T3, T4,T5>(Stream stream, out Tuple<T1, T2, T3, T4,T5> value, ObjectList objList)
		{
			T1 item1 = (T1)NetSerializer.Serializer.Deserialize(stream, objList);
			T2 item2 = (T2)NetSerializer.Serializer.Deserialize(stream, objList);
			T3 item3 = (T3)NetSerializer.Serializer.Deserialize(stream, objList);
			T4 item4 = (T4)NetSerializer.Serializer.Deserialize(stream, objList);
			T5 item5 = (T5)NetSerializer.Serializer.Deserialize(stream, objList);
			value = new Tuple<T1, T2, T3, T4,T5>(item1, item2, item3, item4, item5);
			if (objList != null)
				objList.Add(value);
		}


		public static void WritePrimitive<T1, T2, T3, T4, T5,T6>(Stream stream, Tuple<T1, T2, T3, T4, T5,T6> value, ObjectList objList)
		{
			if (objList != null)
				objList.Add(value);

			NetSerializer.Serializer.Serialize(stream, value.Item1, objList);
			NetSerializer.Serializer.Serialize(stream, value.Item2, objList);
			NetSerializer.Serializer.Serialize(stream, value.Item3, objList);
			NetSerializer.Serializer.Serialize(stream, value.Item4, objList);
			NetSerializer.Serializer.Serialize(stream, value.Item5, objList);
			NetSerializer.Serializer.Serialize(stream, value.Item6, objList);
		}

		public static void ReadPrimitive<T1, T2, T3, T4, T5,T6>(Stream stream, out Tuple<T1, T2, T3, T4, T5,T6> value, ObjectList objList)
		{
			T1 item1 = (T1)NetSerializer.Serializer.Deserialize(stream, objList);
			T2 item2 = (T2)NetSerializer.Serializer.Deserialize(stream, objList);
			T3 item3 = (T3)NetSerializer.Serializer.Deserialize(stream, objList);
			T4 item4 = (T4)NetSerializer.Serializer.Deserialize(stream, objList);
			T5 item5 = (T5)NetSerializer.Serializer.Deserialize(stream, objList);
			T6 item6 = (T6)NetSerializer.Serializer.Deserialize(stream, objList);
			value = new Tuple<T1, T2, T3, T4, T5,T6>(item1, item2, item3, item4, item5, item6);
			if (objList != null)
				objList.Add(value);
		}


		public static void WritePrimitive<T1, T2, T3, T4, T5, T6,T7>(Stream stream, Tuple<T1, T2, T3, T4, T5, T6,T7> value, ObjectList objList)
		{
			if (objList != null)
				objList.Add(value);

			NetSerializer.Serializer.Serialize(stream, value.Item1, objList);
			NetSerializer.Serializer.Serialize(stream, value.Item2, objList);
			NetSerializer.Serializer.Serialize(stream, value.Item3, objList);
			NetSerializer.Serializer.Serialize(stream, value.Item4, objList);
			NetSerializer.Serializer.Serialize(stream, value.Item5, objList);
			NetSerializer.Serializer.Serialize(stream, value.Item6, objList);
			NetSerializer.Serializer.Serialize(stream, value.Item7, objList);
		}

		public static void ReadPrimitive<T1, T2, T3, T4, T5, T6,T7>(Stream stream, out Tuple<T1, T2, T3, T4, T5, T6,T7> value, ObjectList objList)
		{
			T1 item1 = (T1)NetSerializer.Serializer.Deserialize(stream, objList);
			T2 item2 = (T2)NetSerializer.Serializer.Deserialize(stream, objList);
			T3 item3 = (T3)NetSerializer.Serializer.Deserialize(stream, objList);
			T4 item4 = (T4)NetSerializer.Serializer.Deserialize(stream, objList);
			T5 item5 = (T5)NetSerializer.Serializer.Deserialize(stream, objList);
			T6 item6 = (T6)NetSerializer.Serializer.Deserialize(stream, objList);
			T7 item7 = (T7)NetSerializer.Serializer.Deserialize(stream, objList);
			value = new Tuple<T1, T2, T3, T4, T5, T6,T7>(item1, item2, item3, item4, item5, item6, item7);
			if (objList != null)
				objList.Add(value);
		}


		public static void WritePrimitive<T1, T2, T3, T4, T5, T6, T7,TRest>(Stream stream, Tuple<T1, T2, T3, T4, T5, T6, T7,TRest> value, ObjectList objList)
		{
			if (objList != null)
				objList.Add(value);

			NetSerializer.Serializer.Serialize(stream, value.Item1, objList);
			NetSerializer.Serializer.Serialize(stream, value.Item2, objList);
			NetSerializer.Serializer.Serialize(stream, value.Item3, objList);
			NetSerializer.Serializer.Serialize(stream, value.Item4, objList);
			NetSerializer.Serializer.Serialize(stream, value.Item5, objList);
			NetSerializer.Serializer.Serialize(stream, value.Item6, objList);
			NetSerializer.Serializer.Serialize(stream, value.Item7, objList);
			NetSerializer.Serializer.Serialize(stream, value.Rest, objList);
		}

		public static void ReadPrimitive<T1, T2, T3, T4, T5, T6, T7, TRest>(Stream stream, out Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> value, ObjectList objList)
		{
			T1 item1 = (T1)NetSerializer.Serializer.Deserialize(stream, objList);
			T2 item2 = (T2)NetSerializer.Serializer.Deserialize(stream, objList);
			T3 item3 = (T3)NetSerializer.Serializer.Deserialize(stream, objList);
			T4 item4 = (T4)NetSerializer.Serializer.Deserialize(stream, objList);
			T5 item5 = (T5)NetSerializer.Serializer.Deserialize(stream, objList);
			T6 item6 = (T6)NetSerializer.Serializer.Deserialize(stream, objList);
			T7 item7 = (T7)NetSerializer.Serializer.Deserialize(stream, objList);
			TRest item8 = (TRest)NetSerializer.Serializer.Deserialize(stream, objList);
			value = new Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>(item1, item2, item3, item4, item5, item6, item7, item8);
			if (objList != null)
				objList.Add(value);
		}





		public static void WritePrimitive(Stream stream, ObjectRef value, ObjectList objList)
		{
			WritePrimitive(stream, value.obj_ref, objList);
		}

		public static void ReadPrimitive(Stream stream, out ObjectRef value, ObjectList objList)
		{
			ReadPrimitive(stream, out value.obj_ref, objList);
		}


		public static void WritePrimitive<TKey, TValue>(Stream stream, Dictionary<TKey, TValue> value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				NetSerializer.Serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive<TKey, TValue>(Stream stream, out Dictionary<TKey, TValue> value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new Dictionary<TKey, TValue>((int)len);
			for (int i = 0; i < len; i++)
			{
				var kvp = (KeyValuePair<TKey, TValue>)NetSerializer.Serializer.Deserialize(stream, objList);
				value.Add(kvp.Key, kvp.Value);
			}
			if (objList != null)
				objList.Add(value);
		}


		public static void WritePrimitive<TValue>(Stream stream, TValue? value, ObjectList objList) where TValue : struct
		{
			if (!value.HasValue)
			{
				WritePrimitive(stream, (byte)0, objList);
				return;
			}

			WritePrimitive(stream, (byte)1, objList);
			NetSerializer.Serializer.Serialize(stream, value.Value, objList);
		}

		public static void ReadPrimitive<TValue>(Stream stream, out TValue? value, ObjectList objList) where TValue : struct
		{
			byte ntype;
			ReadPrimitive(stream, out ntype, objList);

			if (ntype == 0)
			{
				value = new Nullable<TValue>();
				return;
			}

			TValue v = (TValue)NetSerializer.Serializer.Deserialize(stream, objList);
			value = new Nullable<TValue>(v);
		}

//??????????????????????????????????????
		public static void WritePrimitive<TValue>(Stream stream, List<TValue> value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				NetSerializer.Serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive<TValue>(Stream stream, out List<TValue> value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new List<TValue>((int)len);
			for (int i = 0; i < len; i++)
			{
				value.Add((TValue)NetSerializer.Serializer.Deserialize(stream, objList));
			}
			if (objList != null)
				objList.Add(value);
		}


/////////////////////////////////
		public static void WritePrimitive<TValue>(Stream stream, HashSet<TValue> value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				NetSerializer.Serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive<TValue>(Stream stream, out HashSet<TValue> value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new HashSet<TValue>();
			for (int i = 0; i < len; i++)
			{
				value.Add((TValue)NetSerializer.Serializer.Deserialize(stream, objList));
			}
			if (objList != null)
				objList.Add(value);
		}

/////////////////////////////////
		public static void WritePrimitive<TValue>(Stream stream, Queue<TValue> value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				NetSerializer.Serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive<TValue>(Stream stream, out Queue<TValue> value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new Queue<TValue>((int)len);
			for (int i = 0; i < len; i++)
			{
				value.Enqueue((TValue)NetSerializer.Serializer.Deserialize(stream, objList));
			}
			if (objList != null)
				objList.Add(value);
		}

/////////////////////////////////
		public static void WritePrimitive<TValue>(Stream stream, Stack<TValue> value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				NetSerializer.Serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive<TValue>(Stream stream, out Stack<TValue> value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new Stack<TValue>((int)len);
			for (int i = 0; i < len; i++)
			{
				value.Push((TValue)NetSerializer.Serializer.Deserialize(stream, objList));
			}
			if (objList != null)
				objList.Add(value);
		}

/////////////////////////////////
		public static void WritePrimitive<TKey, TValue>(Stream stream, SortedDictionary<TKey, TValue> value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				NetSerializer.Serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive<TKey, TValue>(Stream stream, out SortedDictionary<TKey, TValue> value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new SortedDictionary<TKey, TValue>();
			for (int i = 0; i < len; i++)
			{
				var kvp = (KeyValuePair<TKey, TValue>)NetSerializer.Serializer.Deserialize(stream, objList);
				value.Add(kvp.Key, kvp.Value);
			}
			if (objList != null)
				objList.Add(value);
		}

/////////////////////////////////
		public static void WritePrimitive<TKey,TValue>(Stream stream, SortedList<TKey,TValue> value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				NetSerializer.Serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive<TKey, TValue>(Stream stream, out SortedList<TKey, TValue> value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new SortedList<TKey, TValue>((int)len);
			for (int i = 0; i < len; i++)
			{
				var kvp = (KeyValuePair<TKey, TValue>)NetSerializer.Serializer.Deserialize(stream, objList);
				value.Add(kvp.Key, kvp.Value);
			}
			if (objList != null)
				objList.Add(value);
		}

/////////////////////////////////
		public static void WritePrimitive<TValue>(Stream stream, SortedSet<TValue> value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				NetSerializer.Serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive<TValue>(Stream stream, out SortedSet<TValue> value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new SortedSet<TValue>();
			for (int i = 0; i < len; i++)
			{
				value.Add((TValue)NetSerializer.Serializer.Deserialize(stream, objList));
			}
			if (objList != null)
				objList.Add(value);
		}

//////////////////////////////////////
		public static void WritePrimitive<TKey, TValue>(Stream stream, ConcurrentDictionary<TKey, TValue> value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				NetSerializer.Serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive<TKey, TValue>(Stream stream, out ConcurrentDictionary<TKey, TValue> value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new ConcurrentDictionary<TKey, TValue>(8, (int)len);
			for (int i = 0; i < len; i++)
			{
				var kvp = (KeyValuePair<TKey, TValue>)NetSerializer.Serializer.Deserialize(stream, objList);
				value.TryAdd(kvp.Key, kvp.Value);
			}
			if (objList != null)
				objList.Add(value);
		}

/////////////////////////////////////////////////////////
		public static void WritePrimitive<TValue>(Stream stream, ConcurrentBag<TValue> value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				NetSerializer.Serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive<TValue>(Stream stream, out ConcurrentBag<TValue> value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new ConcurrentBag<TValue>();
			for (int i = 0; i < len; i++)
			{
				value.Add((TValue)NetSerializer.Serializer.Deserialize(stream, objList));
			}
			if (objList != null)
				objList.Add(value);
		}


//////////!
		public static void WritePrimitive<TValue>(Stream stream, ConcurrentQueue<TValue> value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				NetSerializer.Serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive<TValue>(Stream stream, out ConcurrentQueue<TValue> value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new ConcurrentQueue<TValue>();
			for (int i = 0; i < len; i++)
			{
				value.Enqueue((TValue)NetSerializer.Serializer.Deserialize(stream, objList));
			}
			if (objList != null)
				objList.Add(value);
		}


/////////////!
		public static void WritePrimitive<TValue>(Stream stream, ConcurrentStack<TValue> value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				NetSerializer.Serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive<TValue>(Stream stream, out ConcurrentStack<TValue> value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new ConcurrentStack<TValue>();
			for (int i = 0; i < len; i++)
			{
				value.Push((TValue)NetSerializer.Serializer.Deserialize(stream, objList));
			}
			if (objList != null)
				objList.Add(value);
		}

		//BlockingCollection
		public static void WritePrimitive<TValue>(Stream stream, BlockingCollection<TValue> value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				NetSerializer.Serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive<TValue>(Stream stream, out BlockingCollection<TValue> value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new BlockingCollection<TValue>();
			for (int i = 0; i < len; i++)
			{
				value.Add((TValue)NetSerializer.Serializer.Deserialize(stream, objList));
			}
			if (objList != null)
				objList.Add(value);
		}


		//LinkedList
		public static void WritePrimitive<TValue>(Stream stream, LinkedList<TValue> value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				NetSerializer.Serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive<TValue>(Stream stream, out LinkedList<TValue> value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new LinkedList<TValue>();
			for (int i = 0; i < len; i++)
			{
				value.AddLast((TValue)NetSerializer.Serializer.Deserialize(stream, objList));
			}
			if (objList != null)
				objList.Add(value);
		}





/////////////////!
		public static void WritePrimitive(Stream stream, ArrayList value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				NetSerializer.Serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive(Stream stream, out ArrayList value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new ArrayList((int)len);
			for (int i = 0; i < len; i++)
			{
				value.Add(NetSerializer.Serializer.Deserialize(stream, objList));
			}
			if (objList != null)
				objList.Add(value);
		}

/////////////////!
		public static void WritePrimitive(Stream stream, Hashtable value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(stream, (uint)count + 1, objList);

			foreach (DictionaryEntry kvp in value)
			{
				NetSerializer.Serializer.Serialize(stream, kvp.Key, objList);
				NetSerializer.Serializer.Serialize(stream, kvp.Value, objList);
			}
		}

		public static void ReadPrimitive(Stream stream, out Hashtable value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new Hashtable((int)len);
			for (int i = 0; i < len; i++)
			{
				object _Key = NetSerializer.Serializer.Deserialize(stream, objList);
				object _Val = NetSerializer.Serializer.Deserialize(stream, objList);
				value.Add(_Key, _Val);
			}
			if (objList != null)
				objList.Add(value);
		}


/////////////////!
		public static void WritePrimitive(Stream stream, Queue value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				NetSerializer.Serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive(Stream stream, out Queue value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new Queue((int)len);
			for (int i = 0; i < len; i++)
			{
				value.Enqueue(NetSerializer.Serializer.Deserialize(stream, objList));
			}
			if (objList != null)
				objList.Add(value);
		}


/////////////////!
		public static void WritePrimitive(Stream stream, Stack value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				NetSerializer.Serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive(Stream stream, out Stack value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new Stack((int)len);
			for (int i = 0; i < len; i++)
			{
				value.Push(NetSerializer.Serializer.Deserialize(stream, objList));
			}
			if (objList != null)
				objList.Add(value);
		}


/////////////////!
		public static void WritePrimitive(Stream stream, SortedList value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(stream, (uint)count + 1, objList);

			foreach (DictionaryEntry kvp in value)
			{
				NetSerializer.Serializer.Serialize(stream, kvp.Key, objList);
				NetSerializer.Serializer.Serialize(stream, kvp.Value, objList);
			}
		}

		public static void ReadPrimitive(Stream stream, out SortedList value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new SortedList((int)len);
			for (int i = 0; i < len; i++)
			{
				object _Key = NetSerializer.Serializer.Deserialize(stream, objList);
				object _Val = NetSerializer.Serializer.Deserialize(stream, objList);
				value.Add(_Key, _Val);
			}
			if (objList != null)
				objList.Add(value);
		}

/////////////////!
		public static void WritePrimitive(Stream stream, BitArray value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(stream, (uint)count + 1, objList);

			int numints = (value.Count + 31) / 32;
			WritePrimitive(stream, (uint)numints, objList);

			int[] data = new int[numints];
			value.CopyTo(data, 0);
			foreach (var v in data)
				WritePrimitive(stream, v, objList);
		}

		public static void ReadPrimitive(Stream stream, out BitArray value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			uint numints;
			ReadPrimitive(stream, out numints, null);

			int[] data = new int[numints];
			for (uint i = 0; i < numints; i++)
				ReadPrimitive(stream, out data[i], null);

			value = new BitArray(data);
			value.Length = (int)len;

			if (objList != null)
				objList.Add(value);
		}





	}
}
