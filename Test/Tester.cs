using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NS = NetSerializer;

namespace Test
{
	class Tester
	{
		public static NS.Serializer CreateSerializer()
		{
			var types = typeof(MessageBase).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(MessageBase)))
				.Concat(new Type[] { typeof(SimpleClass), typeof(SimpleClass2) })
				.ToArray();

			var sw = Stopwatch.StartNew();
			var serializer = new NS.Serializer(types, new NS.ITypeSerializer[] { new CustomSerializers() });
			sw.Stop();

			Console.WriteLine("Serializer.Initialize() in {0} ms", sw.ElapsedMilliseconds);

			return serializer;
		}

		List<ISerializerSpecimen> m_specimens = new List<ISerializerSpecimen>();

		public Tester()
			: this(CreateSerializer())
		{
		}

		public Tester(NS.Serializer serializer)
		{
			m_specimens.Add(new NetSerializerSpecimen(serializer));

			if (Program.RunProtoBufTests)
				m_specimens.Add(new ProtobufSpecimen());
		}

		class TestSpec
		{
			public TestSpec(Type type, int num, int loops)
			{
				this.MessageType = type;
				this.NumMessages = num;
				this.Loops = loops;
			}

			public Type MessageType;
			public int NumMessages;
			public int Loops;
		}

		readonly TestSpec[] m_tests = new[]
		{
			new TestSpec(typeof(U8Message),  100, 100000),
			new TestSpec(typeof(S16Message), 100, 100000),
			new TestSpec(typeof(S32Message), 100, 100000),
			new TestSpec(typeof(S64Message), 100, 100000),

			new TestSpec(typeof(DecimalMessage), 100, 50000),
			new TestSpec(typeof(NullableDecimalMessage), 100, 100000),

			new TestSpec(typeof(PrimitivesMessage), 100, 10000),
			new TestSpec(typeof(DictionaryMessage), 10, 1000),
			new TestSpec(typeof(ComplexMessage), 100, 10000),
			new TestSpec(typeof(StringMessage), 100, 20000),
			new TestSpec(typeof(StructMessage), 100, 20000),
			new TestSpec(typeof(BoxedPrimitivesMessage), 100, 20000),

			new TestSpec(typeof(ByteArrayMessage), 10000, 1),
			new TestSpec(typeof(IntArrayMessage), 1000, 1),

			new TestSpec(typeof(CustomSerializersMessage), 10, 100),
		};

		public void Run()
		{
			foreach (var test in m_tests)
			{
				RunTests(test.MessageType, test.NumMessages, test.Loops);
			}

			//Console.WriteLine("Press enter to quit");
			//Console.ReadLine();
		}

		void RunTests(Type msgType, int numMessages, int loops)
		{
			if (Program.QuickRun)
			{
				numMessages = Math.Min(10, numMessages);
				loops = 1;
			}

			Console.WriteLine("== {0} {1} x {2} ==", numMessages, msgType.Name, loops);

			MyRandom rand = new MyRandom(123);

			var msgs = MessageBase.CreateMessages(rand, msgType, numMessages);

			foreach (var specimen in m_specimens)
			{
				if (specimen.CanRun(msgType) == false)
					continue;

				Test(new MemStreamTest(specimen), msgs, loops);
				Test(new NetTest(specimen), msgs, loops);
			}
		}

		static void Test(MemStreamTest test, MessageBase[] msgs, int loops)
		{
			test.Warmup(msgs);

			test.Prepare();

			/* Serialize part */
			{
				Console.Out.Flush();

				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();

				var c0 = GC.CollectionCount(0);
				var c1 = GC.CollectionCount(1);
				var c2 = GC.CollectionCount(2);

				var sw = Stopwatch.StartNew();

				long size = 0;
				for (int l = 0; l < loops; ++l)
					size = test.Serialize(msgs);

				sw.Stop();

				c0 = GC.CollectionCount(0) - c0;
				c1 = GC.CollectionCount(1) - c1;
				c2 = GC.CollectionCount(2) - c2;

				Console.WriteLine("{0,-13} | {1,-21} | {2,10} | {3,3} {4,3} {5,3} | {6,10} |",
					test.Specimen.Name, "MemStream Serialize", sw.ElapsedMilliseconds, c0, c1, c2, size);
			}

			/* Deerialize part */

			{
				MessageBase[] received = new MessageBase[msgs.Length];

				Console.Out.Flush();

				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();

				var c0 = GC.CollectionCount(0);
				var c1 = GC.CollectionCount(1);
				var c2 = GC.CollectionCount(2);

				var sw = Stopwatch.StartNew();

				for (int l = 0; l < loops; ++l)
					test.Deserialize(received);

				sw.Stop();

				c0 = GC.CollectionCount(0) - c0;
				c1 = GC.CollectionCount(1) - c1;
				c2 = GC.CollectionCount(2) - c2;

				Console.WriteLine("{0,-13} | {1,-21} | {2,10} | {3,3} {4,3} {5,3} | {6,10} |",
					test.Specimen.Name, "MemStream Deserialize", sw.ElapsedMilliseconds, c0, c1, c2, "");

				if (Program.EnableResultCheck)
				{
					for (int i = 0; i < msgs.Length; ++i)
					{
						var msg1 = msgs[i];
						var msg2 = received[i];

						msg1.Compare(msg2);
					}
				}
			}
		}

		static void Test(NetTest test, MessageBase[] msgs, int loops)
		{
			test.Prepare(msgs.Length, loops);

			Console.Out.Flush();

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			var c0 = GC.CollectionCount(0);
			var c1 = GC.CollectionCount(1);
			var c2 = GC.CollectionCount(2);

			var sw = Stopwatch.StartNew();

			var received = test.Test(msgs);

			sw.Stop();

			c0 = GC.CollectionCount(0) - c0;
			c1 = GC.CollectionCount(1) - c1;
			c2 = GC.CollectionCount(2) - c2;

			Console.WriteLine("{0,-13} | {1,-21} | {2,10} | {3,3} {4,3} {5,3} | {6,10} |",
				test.Specimen.Name, "NetTest", sw.ElapsedMilliseconds, c0, c1, c2, "");

			if (Program.EnableResultCheck)
			{
				for (int i = 0; i < msgs.Length; ++i)
				{
					var msg1 = msgs[i];
					var msg2 = received[i];

					msg1.Compare(msg2);
				}
			}
		}
	}
}
