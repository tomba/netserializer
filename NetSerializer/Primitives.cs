﻿/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
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


		public static void WritePrimitive(Stream stream, bool value)
		{
			stream.WriteByte(value ? (byte)1 : (byte)0);
		}

		public static void ReadPrimitive(Stream stream, out bool value)
		{
			var b = stream.ReadByte();
			value = b != 0;
		}

        public static void WritePrimitive(Stream stream, bool? value)
        {
            stream.WriteByte(value == null ? (byte) 0 : value.Value ? (byte) 2 : (byte) 1);
        }

		public static void ReadPrimitive(Stream stream, out bool? value)
		{
			var b = stream.ReadByte();
		    value = b == 0 ? (bool?) null : b == 2;
		}        

		public static void WritePrimitive(Stream stream, byte value)
		{
			stream.WriteByte(value);
		}

		public static void ReadPrimitive(Stream stream, out byte value)
		{
			value = (byte)stream.ReadByte();
		}

        public static void WritePrimitive(Stream stream, byte? value)
		{
            if (value == null)
            {
                WritePrimitive(stream, (byte)0);
                return;
            }

            WritePrimitive(stream, (byte)1);
            WritePrimitive(stream, value.Value);            
		}

        public static void ReadPrimitive(Stream stream, out byte? value)
        {
            value = null;
            if (stream.ReadByte() == 0)
                return;

            byte rValue;
            ReadPrimitive(stream, out rValue);
            value = rValue;
        }

		public static void WritePrimitive(Stream stream, sbyte value)
		{
			stream.WriteByte((byte)value);
		}

		public static void ReadPrimitive(Stream stream, out sbyte value)
		{
			value = (sbyte)stream.ReadByte();
		}

        public static void WritePrimitive(Stream stream, sbyte? value)
        {
            if (value == null)
            {
                WritePrimitive(stream, (byte)0);
                return;
            }

            WritePrimitive(stream, (byte)1);
            WritePrimitive(stream, value.Value);            
        }

        public static void ReadPrimitive(Stream stream, out sbyte? value)
        {
            value = null;
            if (stream.ReadByte()==0)
                return;

            sbyte rValue;
            ReadPrimitive(stream, out rValue);
            value = rValue;
        }

		public static void WritePrimitive(Stream stream, char value)
		{
			WriteVarint32(stream, value);
		}

		public static void ReadPrimitive(Stream stream, out char value)
		{
			value = (char)ReadVarint32(stream);
		}

        public static void WritePrimitive(Stream stream, char? value)
        {
            if (value == null)
            {
                WritePrimitive(stream, (byte)0);
                return;
            }

            WritePrimitive(stream, (byte)1);
            WritePrimitive(stream, value.Value);
        }

        public static void ReadPrimitive(Stream stream, out char? value)
        {
            value = null;
            if (stream.ReadByte() == 0)
                return;

            char rValue;
            ReadPrimitive(stream, out rValue);
            value = rValue;
        }

		public static void WritePrimitive(Stream stream, ushort value)
		{
			WriteVarint32(stream, value);
		}

		public static void ReadPrimitive(Stream stream, out ushort value)
		{
			value = (ushort)ReadVarint32(stream);
		}

        public static void WritePrimitive(Stream stream, ushort? value)
        {
            if (value == null)
            {
                WritePrimitive(stream, (byte)0);
                return;
            }

            WritePrimitive(stream, (byte)1);
            WritePrimitive(stream, value.Value);
        }

        public static void ReadPrimitive(Stream stream, out ushort? value)
        {
            value = null;
            if (stream.ReadByte() == 0)
                return;

            ushort rValue;
            ReadPrimitive(stream, out rValue);
            value = rValue;
        }

		public static void WritePrimitive(Stream stream, short value)
		{
			WriteVarint32(stream, EncodeZigZag32(value));
		}

		public static void ReadPrimitive(Stream stream, out short value)
		{
			value = (short)DecodeZigZag32(ReadVarint32(stream));
		}

        public static void WritePrimitive(Stream stream, short? value)
        {
            if (value == null)
            {
                WritePrimitive(stream, (byte)0);
                return;
            }

            WritePrimitive(stream, (byte)1);
            WritePrimitive(stream, value.Value);
        }

        public static void ReadPrimitive(Stream stream, out short? value)
        {
            value = null;
            if (stream.ReadByte() == 0)
                return;

            short rValue;
            ReadPrimitive(stream, out rValue);
            value = rValue;
        }

		public static void WritePrimitive(Stream stream, uint value)
		{
			WriteVarint32(stream, value);
		}

		public static void ReadPrimitive(Stream stream, out uint value)
		{
			value = ReadVarint32(stream);
		}

        public static void WritePrimitive(Stream stream, uint? value)
        {
            if (value == null)
            {
                WritePrimitive(stream, (byte)0);
                return;
            }

            WritePrimitive(stream, (byte)1);
            WritePrimitive(stream, value.Value);
        }

        public static void ReadPrimitive(Stream stream, out uint? value)
        {
            value = null;
            if (stream.ReadByte() == 0)
                return;

            uint rValue;
            ReadPrimitive(stream, out rValue);
            value = rValue;
        }

		public static void WritePrimitive(Stream stream, int value)
		{
			WriteVarint32(stream, EncodeZigZag32(value));
		}

		public static void ReadPrimitive(Stream stream, out int value)
		{
			value = DecodeZigZag32(ReadVarint32(stream));
		}

        public static void WritePrimitive(Stream stream, int? value)
        {
            if (value == null)
            {
                WritePrimitive(stream, (byte)0);
                return;
            }

            WritePrimitive(stream, (byte)1);
            WritePrimitive(stream, value.Value);
        }

        public static void ReadPrimitive(Stream stream, out int? value)
        {
            value = null;
            if (stream.ReadByte() == 0)
                return;

            int rValue;
            ReadPrimitive(stream, out rValue);
            value = rValue;
        }

		public static void WritePrimitive(Stream stream, ulong value)
		{
			WriteVarint64(stream, value);
		}

		public static void ReadPrimitive(Stream stream, out ulong value)
		{
			value = ReadVarint64(stream);
		}

        public static void WritePrimitive(Stream stream, ulong? value)
        {
            if (value == null)
            {
                WritePrimitive(stream, (byte)0);
                return;
            }

            WritePrimitive(stream, (byte)1);
            WritePrimitive(stream, value.Value);
        }

        public static void ReadPrimitive(Stream stream, out ulong? value)
        {
            value = null;
            if (stream.ReadByte() == 0)
                return;

            ulong rValue;
            ReadPrimitive(stream, out rValue);
            value = rValue;
        }

		public static void WritePrimitive(Stream stream, long value)
		{
			WriteVarint64(stream, EncodeZigZag64(value));
		}

		public static void ReadPrimitive(Stream stream, out long value)
		{
			value = DecodeZigZag64(ReadVarint64(stream));
		}

        public static void WritePrimitive(Stream stream, long? value)
        {
            if (value == null)
            {
                WritePrimitive(stream, (byte)0);
                return;
            }

            WritePrimitive(stream, (byte)1);
            WritePrimitive(stream, value.Value);
        }

        public static void ReadPrimitive(Stream stream, out long? value)
        {
            value = null;
            if (stream.ReadByte() == 0)
                return;

            long rValue;
            ReadPrimitive(stream, out rValue);
            value = rValue;
        }

        public static void WritePrimitive(Stream stream, decimal value)
        {
            var bt = Decimal.GetBits(value);
            var ba1 = BitConverter.GetBytes(bt[0]);
            var ba2 = BitConverter.GetBytes(bt[1]);
            var ba3 = BitConverter.GetBytes(bt[2]);
            var ba4 = BitConverter.GetBytes(bt[3]);
            WritePrimitive(stream, new[] { ba1[0], ba1[1], ba1[2], ba1[3], ba2[0], ba2[1], ba2[2], ba2[3], ba3[0], ba3[1], ba3[2], ba3[3], ba4[0], ba4[1], ba4[2], ba4[3] });            
        }

        public static void ReadPrimitive(Stream stream, out decimal value)
        {
            byte[] bt;
            ReadPrimitive(stream, out bt);
            var i1 = BitConverter.ToInt32(bt, 0);
            var i2 = BitConverter.ToInt32(bt, 4);
            var i3 = BitConverter.ToInt32(bt, 8);
            var i4 = BitConverter.ToInt32(bt, 12);

            value = new decimal(new int[] {i1, i2, i3, i4});
        }

        public static void WritePrimitive(Stream stream, decimal? value)
        {
            if (value == null)
            {
                WritePrimitive(stream, (byte)0);
                return;
            }

            WritePrimitive(stream, (byte)1);
            WritePrimitive(stream, value.Value);
        }

        public static void ReadPrimitive(Stream stream, out decimal? value)
        {
            value = null;
            if (stream.ReadByte() == 0)
                return;

            decimal rValue;
            ReadPrimitive(stream, out rValue);
            value = rValue;
        }

#if !NO_UNSAFE
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
#else
		public static void WritePrimitive(Stream stream, float value)
		{
			WritePrimitive(stream, (double)value);
		}

		public static void ReadPrimitive(Stream stream, out float value)
		{
			double v;
			ReadPrimitive(stream, out v);
			value = (float)v;
		}

		public static void WritePrimitive(Stream stream, double value)
		{
			ulong v = (ulong)BitConverter.DoubleToInt64Bits(value);
			WriteVarint64(stream, v);
		}

		public static void ReadPrimitive(Stream stream, out double value)
		{
			ulong v = ReadVarint64(stream);
			value = BitConverter.Int64BitsToDouble((long)v);
		}
#endif

        public static void WritePrimitive(Stream stream, float? value)
        {
            if (value == null)
            {
                WritePrimitive(stream, (byte)0);
                return;
            }

            WritePrimitive(stream, (byte)1);
            WritePrimitive(stream, value.Value);
        }

        public static void ReadPrimitive(Stream stream, out float? value)
        {
            value = null;
            if (stream.ReadByte() == 0)
                return;

            float rValue;
            ReadPrimitive(stream, out rValue);
            value = rValue;
        }

        public static void WritePrimitive(Stream stream, double? value)
        {
            if (value == null)
            {
                WritePrimitive(stream, (byte)0);
                return;
            }

            WritePrimitive(stream, (byte)1);
            WritePrimitive(stream, value.Value);
        }

        public static void ReadPrimitive(Stream stream, out double? value)
        {
            value = null;
            if (stream.ReadByte() == 0)
                return;

            double rValue;
            ReadPrimitive(stream, out rValue);
            value = rValue;
        }

		public static void WritePrimitive(Stream stream, DateTime value)
		{
			long v = value.ToBinary();
			WritePrimitive(stream, v);
		}

		public static void ReadPrimitive(Stream stream, out DateTime value)
		{
			long v;
			ReadPrimitive(stream, out v);
			value = DateTime.FromBinary(v);
		}

        public static void WritePrimitive(Stream stream, DateTime? value)
        {
            if (value == null)
            {
                WritePrimitive(stream, (byte)0);
                return;
            }

            WritePrimitive(stream, (byte)1);
            WritePrimitive(stream, value.Value);
        }

        public static void ReadPrimitive(Stream stream, out DateTime? value)
        {
            value = null;
            if (stream.ReadByte() == 0)
                return;

            DateTime rValue;
            ReadPrimitive(stream, out rValue);
            value = rValue;
        }

        public static void WritePrimitive(Stream stream, TimeSpan value)
        {
            long v = value.Ticks;
            WritePrimitive(stream, v);
        }

        public static void ReadPrimitive(Stream stream, out TimeSpan value)
        {
            long v;
            ReadPrimitive(stream, out v);
            value = new TimeSpan(v);
        }

        public static void WritePrimitive(Stream stream, TimeSpan? value)
        {
            if (value == null)
            {
                WritePrimitive(stream, (byte)0);
                return;
            }

            WritePrimitive(stream, (byte)1);
            WritePrimitive(stream, value.Value);
        }

        public static void ReadPrimitive(Stream stream, out TimeSpan? value)
        {
            value = null;
            if (stream.ReadByte() == 0)
                return;

            TimeSpan rValue;
            ReadPrimitive(stream, out rValue);
            value = rValue;
        }

        public static void WritePrimitive(Stream stream, DateTimeOffset value)
        {
            var v1 = value.DateTime;
            var v2 = value.Offset;
            WritePrimitive(stream, v1);
            WritePrimitive(stream, v2);
        }

        public static void ReadPrimitive(Stream stream, out DateTimeOffset value)
        {
            DateTime v1;
            TimeSpan v2;
            ReadPrimitive(stream, out v1);
            ReadPrimitive(stream, out v2);
            value = new DateTimeOffset(v1, v2);
        }

        public static void WritePrimitive(Stream stream, DateTimeOffset? value)
        {
            if (value == null)
            {
                WritePrimitive(stream, (byte)0);
                return;
            }

            WritePrimitive(stream, (byte)1);
            WritePrimitive(stream, value.Value);
        }

        public static void ReadPrimitive(Stream stream, out DateTimeOffset? value)
        {
            value = null;
            if (stream.ReadByte() == 0)
                return;

            DateTimeOffset rValue;
            ReadPrimitive(stream, out rValue);
            value = rValue;
        }


        public static void WritePrimitive(Stream stream, Guid value)
        {
            var v = value.ToByteArray();
            WritePrimitive(stream, v);
        }

        public static void ReadPrimitive(Stream stream, out Guid value)
        {
            byte[] v;
            ReadPrimitive(stream, out v);
            value = new Guid(v);
        }

        public static void WritePrimitive(Stream stream, Guid? value)
        {
            if (value == null)
            {
                WritePrimitive(stream, (byte)0);
                return;
            }

            WritePrimitive(stream, (byte)1);
            WritePrimitive(stream, value.Value);
        }

        public static void ReadPrimitive(Stream stream, out Guid? value)
        {
            value = null;
            if (stream.ReadByte() == 0)
                return;

            Guid rValue;
            ReadPrimitive(stream, out rValue);
            value = rValue;
        }

        public static void WritePrimitive(Stream stream, Uri value)
        {
            var v = value.OriginalString;
            WritePrimitive(stream, v);
        }

        public static void ReadPrimitive(Stream stream, out Uri value)
        {
            string v;
            ReadPrimitive(stream, out v);
            value = new Uri(v, UriKind.RelativeOrAbsolute);
        }
        
#if NO_UNSAFE
		public static void WritePrimitive(Stream stream, string value)
		{
			if (value == null)
			{
				WritePrimitive(stream, (uint)0);
				return;
			}

			var encoding = new UTF8Encoding(false, true);

			int len = encoding.GetByteCount(value);

			WritePrimitive(stream, (uint)len + 1);

			var buf = new byte[len];

			encoding.GetBytes(value, 0, value.Length, buf, 0);

			stream.Write(buf, 0, len);
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
		}
#else
		sealed class StringHelper
		{
			public StringHelper()
			{
				this.Encoding = new UTF8Encoding(false, true);
			}

			public const int BYTEBUFFERLEN = 256;
			public const int CHARBUFFERLEN = 128;

			Encoder m_encoder;
			Decoder m_decoder;

			byte[] m_byteBuffer;
			char[] m_charBuffer;

			public UTF8Encoding Encoding { get; private set; }
			public Encoder Encoder { get { if (m_encoder == null) m_encoder = this.Encoding.GetEncoder(); return m_encoder; } }
			public Decoder Decoder { get { if (m_decoder == null) m_decoder = this.Encoding.GetDecoder(); return m_decoder; } }

			public byte[] ByteBuffer { get { if (m_byteBuffer == null) m_byteBuffer = new byte[BYTEBUFFERLEN]; return m_byteBuffer; } }
			public char[] CharBuffer { get { if (m_charBuffer == null) m_charBuffer = new char[CHARBUFFERLEN]; return m_charBuffer; } }
		}

		[ThreadStatic]
		static StringHelper s_stringHelper;

		public unsafe static void WritePrimitive(Stream stream, string value)
		{
			if (value == null)
			{
				WritePrimitive(stream, (uint)0);
				return;
			}
			else if (value.Length == 0)
			{
				WritePrimitive(stream, (uint)1);
				return;
			}

			var helper = s_stringHelper;
			if (helper == null)
				s_stringHelper = helper = new StringHelper();

			var encoder = helper.Encoder;
			var buf = helper.ByteBuffer;

			int totalChars = value.Length;
			int totalBytes;

			fixed (char* ptr = value)
				totalBytes = encoder.GetByteCount(ptr, totalChars, true);

			WritePrimitive(stream, (uint)totalBytes + 1);
			WritePrimitive(stream, (uint)totalChars);

			int p = 0;
			bool completed = false;

			while (completed == false)
			{
				int charsConverted;
				int bytesConverted;

				fixed (char* src = value)
				fixed (byte* dst = buf)
				{
					encoder.Convert(src + p, totalChars - p, dst, buf.Length, true,
						out charsConverted, out bytesConverted, out completed);
				}

				stream.Write(buf, 0, bytesConverted);

				p += charsConverted;
			}
		}

		public static void ReadPrimitive(Stream stream, out string value)
		{
			uint totalBytes;
			ReadPrimitive(stream, out totalBytes);

			if (totalBytes == 0)
			{
				value = null;
				return;
			}
			else if (totalBytes == 1)
			{
				value = string.Empty;
				return;
			}

			totalBytes -= 1;

			uint totalChars;
			ReadPrimitive(stream, out totalChars);

			var helper = s_stringHelper;
			if (helper == null)
				s_stringHelper = helper = new StringHelper();

			var decoder = helper.Decoder;
			var buf = helper.ByteBuffer;
			char[] chars;
			if (totalChars <= StringHelper.CHARBUFFERLEN)
				chars = helper.CharBuffer;
			else
				chars = new char[totalChars];

			int streamBytesLeft = (int)totalBytes;

			int cp = 0;

			while (streamBytesLeft > 0)
			{
				int bytesInBuffer = stream.Read(buf, 0, Math.Min(buf.Length, streamBytesLeft));
				if (bytesInBuffer == 0)
					throw new EndOfStreamException();

				streamBytesLeft -= bytesInBuffer;
				bool flush = streamBytesLeft == 0 ? true : false;

				bool completed = false;

				int p = 0;

				while (completed == false)
				{
					int charsConverted;
					int bytesConverted;

					decoder.Convert(buf, p, bytesInBuffer - p,
						chars, cp, (int)totalChars - cp,
						flush,
						out bytesConverted, out charsConverted, out completed);

					p += bytesConverted;
					cp += charsConverted;
				}
			}

			value = new string(chars, 0, (int)totalChars);
		}
#endif

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
					throw new EndOfStreamException();
				l += r;
			}
		}

        public static void WritePrimitive<TKey, TValue>(Stream stream, Dictionary<TKey, TValue> value)
		{
			var kvpArray = new KeyValuePair<TKey, TValue>[value.Count];

			int i = 0;
			foreach (var kvp in value)
				kvpArray[i++] = kvp;

			NetSerializer.Serializer.SerializeInternal(stream, kvpArray);
		}

		public static void ReadPrimitive<TKey, TValue>(Stream stream, out Dictionary<TKey, TValue> value)
		{
			var kvpArray = (KeyValuePair<TKey, TValue>[])NetSerializer.Serializer.DeserializeInternal(stream);

			value = new Dictionary<TKey, TValue>(kvpArray.Length);

			foreach (var kvp in kvpArray)
				value.Add(kvp.Key, kvp.Value);
		}
	}
}
