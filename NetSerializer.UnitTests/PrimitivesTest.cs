using System;
using System.IO;
using NUnit.Framework;

namespace NetSerializer.UnitTests
{
	[TestFixture]
	[TestOf(typeof(Primitives))]
	[Parallelizable(ParallelScope.All)]
	public class PrimitivesTest
	{
#if NET5_0
#if NO_UNSAFE
        [Ignore("Float and half tests are inacurrate due to rounding when NO_UNSAFE is enabled.")]
#endif
        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(123.4)]
        [TestCase(0.01)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        [TestCase(double.NaN)]
        public void TestHalf(double val)
        {
        	// Can't stick Half values in attributes so have to do this.
        	var half = (Half) val;

        	var stream = new MemoryStream();
        	Primitives.WritePrimitive(stream, half);

        	stream.Position = 0;

        	Primitives.ReadPrimitive(new ByteStream(stream), out Half read);
        	Assert.That(read, Is.EqualTo(half));
        }
#endif

#if NO_UNSAFE
		[Ignore("Float tests are inacurrate due to rounding when NO_UNSAFE is enabled.")]
#endif
		[Test]
		[TestCase(0)]
		[TestCase(1)]
		[TestCase(123.4f)]
		[TestCase(0.01f)]
		[TestCase(float.PositiveInfinity)]
		[TestCase(float.NegativeInfinity)]
		[TestCase(float.NaN)]
		public void TestSingle(float val)
		{
			var stream = new MemoryStream();
			Primitives.WritePrimitive(stream, val);

			stream.Position = 0;

			Primitives.ReadPrimitive(new ByteStream(stream), out float read);
			Assert.That(read, Is.EqualTo(val));
		}

		[Test]
		[TestCase(0)]
		[TestCase(1)]
		[TestCase(123.4)]
		[TestCase(0.01)]
		[TestCase(float.PositiveInfinity)]
		[TestCase(float.NegativeInfinity)]
		[TestCase(float.NaN)]
		public void TestDouble(double val)
		{
			var stream = new MemoryStream();
			Primitives.WritePrimitive(stream, val);

			stream.Position = 0;

			Primitives.ReadPrimitive(new ByteStream(stream), out double read);
			Assert.That(read, Is.EqualTo(val));
		}

		// Stream wrapper that only reads one byte at a time to test the reading code.
		private sealed class ByteStream : Stream
		{
			private readonly Stream _parent;

			public ByteStream(Stream parent)
			{
				_parent = parent;
			}

			public override void Flush()
			{
				_parent.Flush();
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				return _parent.Seek(offset, origin);
			}

			public override void SetLength(long value)
			{
				_parent.SetLength(value);
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				return _parent.Read(buffer, offset, 1);
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				_parent.Write(buffer, offset, count);
			}

			public override bool CanRead => _parent.CanRead;

			public override bool CanSeek => _parent.CanSeek;

			public override bool CanWrite => _parent.CanWrite;

			public override long Length => _parent.Length;

			public override long Position
			{
				get => _parent.Position;
				set => _parent.Position = value;
			}
		}
	}
}
