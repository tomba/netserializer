﻿/*
 * Copyright 2015 Tomi Valkeinen
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Diagnostics;
#if NETCOREAPP
using System.Runtime.CompilerServices;
using System.Text.Unicode;
using System.Buffers.Binary;
using System.Buffers;
#endif

namespace NetSerializer
{
	public static class Primitives
	{
		private const int StringByteBufferLength = 256;
		private const int StringCharBufferLength = 128;

		public static MethodInfo GetWritePrimitive(Type type)
		{
			return typeof(Primitives).GetMethod("WritePrimitive",
				BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null,
				new Type[] { typeof(Stream), type }, null);
		}

		public static MethodInfo GetReaderPrimitive(Type type)
		{
			return typeof(Primitives).GetMethod("ReadPrimitive",
				BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null,
				new Type[] { typeof(Stream), type.MakeByRefType() }, null);
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

#if !NO_UNSAFE
		public static unsafe void WritePrimitive(Stream stream, float value)
		{
			uint v = *(uint*)(&value);
			WriteUInt32(stream, v);
		}

		public static unsafe void ReadPrimitive(Stream stream, out float value)
		{
			uint v = ReadUInt32(stream);
			value = *(float*)(&v);
		}

		public static unsafe void WritePrimitive(Stream stream, double value)
		{
			ulong v = *(ulong*)(&value);
			WriteUInt64(stream, v);
		}

		public static unsafe void ReadPrimitive(Stream stream, out double value)
		{
			ulong v = ReadUInt64(stream);
			value = *(double*)(&v);
		}

#if NET5_0
		public static void WritePrimitive(Stream stream, Half value)
		{
			ushort v = Unsafe.As<Half, ushort>(ref value);
			WriteUInt16(stream, v);
		}

		public static void ReadPrimitive(Stream stream, out Half value)
		{
			var v = ReadUInt16(stream);
			value = Unsafe.As<ushort, Half>(ref v);
		}
#endif

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
			WriteUInt64(stream, v);
		}

		public static void ReadPrimitive(Stream stream, out double value)
		{
			ulong v = ReadUInt64(stream);
			value = BitConverter.Int64BitsToDouble((long)v);
		}

#if NET5_0
		public static void WritePrimitive(Stream stream, Half value)
		{
			WritePrimitive(stream, (double)value);
		}

		public static void ReadPrimitive(Stream stream, out Half value)
		{
			double v;
			ReadPrimitive(stream, out v);
			value = (Half)v;
		}
#endif

#endif

		private static void WriteUInt16(Stream stream, ushort value)
		{
			stream.WriteByte((byte) value);
			stream.WriteByte((byte) (value >> 8));
		}

		private static ushort ReadUInt16(Stream stream)
		{
			ushort a = 0;

			for (var i = 0; i < 16; i += 8)
			{
				var val = stream.ReadByte();
				if (val == -1)
					throw new EndOfStreamException();

				a |= (ushort) (val << i);
			}

			return a;
		}

		// 32 and 64 bit variants use stackalloc when everything is available since it's faster.

#if !NETCOREAPP
		private static void WriteUInt32(Stream stream, uint value)
		{
			stream.WriteByte((byte) value);
			stream.WriteByte((byte) (value >> 8));
			stream.WriteByte((byte) (value >> 16));
			stream.WriteByte((byte) (value >> 24));
		}

		private static void WriteUInt64(Stream stream, ulong value)
		{
			stream.WriteByte((byte) value);
			stream.WriteByte((byte) (value >> 8));
			stream.WriteByte((byte) (value >> 16));
			stream.WriteByte((byte) (value >> 24));
			stream.WriteByte((byte) (value >> 32));
			stream.WriteByte((byte) (value >> 40));
			stream.WriteByte((byte) (value >> 48));
			stream.WriteByte((byte) (value >> 56));
		}

		private static uint ReadUInt32(Stream stream)
		{
			uint a = 0;

			for (var i = 0; i < 32; i += 8)
			{
				var val = stream.ReadByte();
				if (val < 0)
					throw new EndOfStreamException();

				a |= (uint)val << i;
			}

			return a;
		}

		private static ulong ReadUInt64(Stream stream)
		{
			ulong a = 0;

			for (var i = 0; i < 64; i += 8)
			{
				var val = stream.ReadByte();
				if (val < 0)
					throw new EndOfStreamException();

				a |= (ulong)val << i;
			}

			return a;
		}
#else
		private static void WriteUInt32(Stream stream, uint value)
		{
			Span<byte> buf = stackalloc byte[4];
			BinaryPrimitives.WriteUInt32LittleEndian(buf, value);

			stream.Write(buf);
		}

		private static void WriteUInt64(Stream stream, ulong value)
		{
			Span<byte> buf = stackalloc byte[8];
			BinaryPrimitives.WriteUInt64LittleEndian(buf, value);

			stream.Write(buf);
		}

		private static uint ReadUInt32(Stream stream)
		{
			Span<byte> buf = stackalloc byte[4];
			var wSpan = buf;

			while (true)
			{
				var read = stream.Read(wSpan);
				if (read == 0)
					throw new EndOfStreamException();
				if (read == wSpan.Length)
					break;
				wSpan = wSpan[read..];
			}

			return BinaryPrimitives.ReadUInt32LittleEndian(buf);
		}

		private static ulong ReadUInt64(Stream stream)
		{
			Span<byte> buf = stackalloc byte[8];
			var wSpan = buf;

			while (true)
			{
				var read = stream.Read(wSpan);
				if (read == 0)
					throw new EndOfStreamException();
				if (read == wSpan.Length)
					break;
				wSpan = wSpan[read..];
			}

			return BinaryPrimitives.ReadUInt64LittleEndian(buf);
		}
#endif

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

		[ThreadStatic]
		static int[] s_decimalBitsArray;

		public static void WritePrimitive(Stream stream, decimal value)
		{
			int[] bits = Decimal.GetBits(value);

			ulong low = (uint)bits[0];
			ulong mid = ((ulong)(uint)bits[1]) << 32;
			ulong lowmid = low | mid;

			uint high = (uint)bits[2];

			uint scale = ((uint)bits[3] >> 15) & 0x01fe;
			uint sign = ((uint)bits[3]) >> 31;
			uint scaleSign = scale | sign;

			WritePrimitive(stream, lowmid);
			WritePrimitive(stream, high);
			WritePrimitive(stream, scaleSign);
		}

		public static void ReadPrimitive(Stream stream, out decimal value)
		{
			ulong lowmid;
			uint high, scaleSign;

			ReadPrimitive(stream, out lowmid);
			ReadPrimitive(stream, out high);
			ReadPrimitive(stream, out scaleSign);

			int scale = (int)((scaleSign & ~1) << 15);
			int sign = (int)((scaleSign & 1) << 31);

			var arr = s_decimalBitsArray;
			if (arr == null)
				arr = s_decimalBitsArray = new int[4];

			arr[0] = (int)lowmid;
			arr[1] = (int)(lowmid >> 32);
			arr[2] = (int)high;
			arr[3] = scale | sign;

			value = new Decimal(arr);
		}

#if NETCOREAPP
		public static void WritePrimitive(Stream stream, string value)
		{
			if (value == null)
			{
				WritePrimitive(stream, (uint)0);
				return;
			}

			if (value.Length == 0)
			{
				WritePrimitive(stream, (uint)1);
				return;
			}

			Span<byte> buf = stackalloc byte[StringByteBufferLength];

			var totalChars = value.Length;
			var totalBytes = Encoding.UTF8.GetByteCount(value);

			WritePrimitive(stream, (uint)totalBytes + 1);
			WritePrimitive(stream, (uint)totalChars);

			var totalRead = 0;
			ReadOnlySpan<char> span = value;
			for (;;)
			{
				var finalChunk = totalRead + totalChars >= totalChars;
				Utf8.FromUtf16(span, buf, out var read, out var wrote, isFinalBlock: finalChunk);
				stream.Write(buf.Slice(0, wrote));
				totalRead += read;
				if (read >= totalChars)
					break;

				span = span[read..];
				totalChars -= read;
			}
		}

		// We cache the delegate in a static here to avoid a delegate allocation on every call to ReadPrimitive.
		private static readonly SpanAction<char, (int, Stream)> _stringSpanRead = StringSpanRead;

		public static void ReadPrimitive(Stream stream, out string value)
		{
			ReadPrimitive(stream, out uint totalBytes);

			if (totalBytes == 0)
			{
				value = null;
				return;
			}

			if (totalBytes == 1)
			{
				value = string.Empty;
				return;
			}

			totalBytes -= 1;

			ReadPrimitive(stream, out uint totalChars);

			value = string.Create((int) totalChars, ((int) totalBytes, stream), _stringSpanRead);
		}

		private static void StringSpanRead(Span<char> span, (int totalBytes, Stream stream) tuple)
		{
			Span<byte> buf = stackalloc byte[StringByteBufferLength];

			var (totalBytes, stream) = tuple;

			var totalBytesRead = 0;
			var totalCharsRead = 0;
			var writeBufStart = 0;

			while (totalBytesRead < totalBytes)
			{
				var bytesLeft = totalBytes - totalBytesRead;
				var bytesReadLeft = Math.Min(buf.Length, bytesLeft);
				var writeSlice = buf.Slice(writeBufStart, bytesReadLeft - writeBufStart);
				var bytesInBuffer = stream.Read(writeSlice);
				if (bytesInBuffer == 0)
					throw new EndOfStreamException();

				var readFromStream = bytesInBuffer + writeBufStart;
				var final = readFromStream == bytesLeft;
				var status = Utf8.ToUtf16(buf[..readFromStream], span[totalCharsRead..], out var bytesRead, out var charsRead, isFinalBlock: final);

				totalBytesRead += bytesRead;
				totalCharsRead += charsRead;
				writeBufStart = 0;

				if (status == OperationStatus.DestinationTooSmall)
				{
					// Malformed data?
					throw new InvalidDataException();
				}

				if (status == OperationStatus.NeedMoreData)
				{
					// We got cut short in the middle of a multi-byte UTF-8 sequence.
					// So we need to move it to the bottom of the span, then read the next bit *past* that.
					// This copy should be fine because we're only ever gonna be copying up to 4 bytes
					// from the end of the buffer to the start.
					// So no chance of overlap.
					buf[bytesRead..].CopyTo(buf);
					writeBufStart = bytesReadLeft - bytesRead;
					continue;
				}

				Debug.Assert(status == OperationStatus.Done);
			}
		}
#elif NO_UNSAFE
		public static void WritePrimitive(Stream stream, string value)
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

			var encoding = new UTF8Encoding(false, true);

			int len = encoding.GetByteCount(value);

			WritePrimitive(stream, (uint)len + 1);
			WritePrimitive(stream, (uint)value.Length);

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

			uint totalChars;
			ReadPrimitive(stream, out totalChars);

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

			Encoder m_encoder;
			Decoder m_decoder;

			byte[] m_byteBuffer;
			char[] m_charBuffer;

			public UTF8Encoding Encoding { get; private set; }
			public Encoder Encoder { get { if (m_encoder == null) m_encoder = this.Encoding.GetEncoder(); return m_encoder; } }
			public Decoder Decoder { get { if (m_decoder == null) m_decoder = this.Encoding.GetDecoder(); return m_decoder; } }

			public byte[] ByteBuffer { get { if (m_byteBuffer == null) m_byteBuffer = new byte[StringByteBufferLength]; return m_byteBuffer; } }
			public char[] CharBuffer { get { if (m_charBuffer == null) m_charBuffer = new char[StringCharBufferLength]; return m_charBuffer; } }
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
			if (totalChars <= StringCharBufferLength)
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
	}
}
