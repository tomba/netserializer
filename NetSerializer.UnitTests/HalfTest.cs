#if NET5_0
using System;
using System.IO;
using NUnit.Framework;

namespace NetSerializer.UnitTests
{
	[TestFixture]
	[TestOf(typeof(Primitives))]
	[Parallelizable(ParallelScope.All)]
	public class HalfTest
	{
		[Test]
		public void Test()
		{
			var serializer = new Serializer(new[] {typeof(SerializationType)});

			var stream = new MemoryStream();
			var obj = new SerializationType
			{
				R = (Half) 0,
				G = (Half) 12.34,
				B = (Half) 0.1,
				A = Half.PositiveInfinity,
			};

			serializer.Serialize(stream, obj);

			stream.Position = 0;

			var read = (SerializationType) serializer.Deserialize(stream);

			Assert.That(read.R, Is.EqualTo(obj.R));
			Assert.That(read.G, Is.EqualTo(obj.G));
			Assert.That(read.B, Is.EqualTo(obj.B));
			Assert.That(read.A, Is.EqualTo(obj.A));
		}

		[Serializable]
		private class SerializationType
		{
			public Half R;
			public Half G;
			public Half B;
			public Half A;
		}
	}
}
#endif
