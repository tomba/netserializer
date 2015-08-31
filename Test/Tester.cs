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
		NS.Serializer m_serializer;

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

		public Tester()
		{
			m_serializer = CreateSerializer();
		}

		public Tester(NS.Serializer serializer)
		{
			m_serializer = serializer;
		}

		public void Run()
		{
			MyRandom rand = new MyRandom(123);

			RunTests(rand, typeof(U8Message), 6000000);
			RunTests(rand, typeof(S16Message), 6000000);
			RunTests(rand, typeof(S32Message), 6000000);
			RunTests(rand, typeof(S64Message), 5000000);

			RunTests(rand, typeof(DecimalMessage), 3000000);
			RunTests(rand, typeof(NullableDecimalMessage), 3000000);

			RunTests(rand, typeof(PrimitivesMessage), 1000000);
			RunTests(rand, typeof(DictionaryMessage), 5000);

			RunTests(rand, typeof(ComplexMessage), 1000000);

			RunTests(rand, typeof(StringMessage), 600000);

			RunTests(rand, typeof(StructMessage), 2000000);

			RunTests(rand, typeof(BoxedPrimitivesMessage), 2000000);

			RunTests(rand, typeof(ByteArrayMessage), 5000);
			RunTests(rand, typeof(IntArrayMessage), 800);

			RunTests(rand, typeof(CustomSerializersMessage), 800);

			//Console.WriteLine("Press enter to quit");
			//Console.ReadLine();
		}

		void RunTests(MyRandom rand, Type msgType, int numMessages)
		{
			if (Program.QuickRun)
				numMessages = 50;

			Console.WriteLine("== {0} {1} ==", numMessages, msgType.Name);

			bool protobufCompatible = msgType.GetCustomAttributes(typeof(ProtoBuf.ProtoContractAttribute), false).Any();

			var msgs = MessageBase.CreateMessages(rand, msgType, numMessages);

			Test(new MemStreamTest(m_serializer), msgs);
			Test(new NetTest(m_serializer), msgs);

			if (Program.RunProtoBufTests && protobufCompatible)
			{
				Test(new PBMemStreamTest(), msgs);
				Test(new PBNetTest(), msgs);
			}
		}

		static void Test(IMemStreamTest test, MessageBase[] msgs)
		{
			test.Warmup(msgs);

			test.Prepare(msgs.Length);

			/* Serialize part */
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();

				var c0 = GC.CollectionCount(0);
				var c1 = GC.CollectionCount(1);
				var c2 = GC.CollectionCount(2);

				var sw = Stopwatch.StartNew();

				long size = test.Serialize(msgs);

				sw.Stop();

				c0 = GC.CollectionCount(0) - c0;
				c1 = GC.CollectionCount(1) - c1;
				c2 = GC.CollectionCount(2) - c2;

				Console.WriteLine("{0,-13} | {1,-21} | {2,10} | {3,3} {4,3} {5,3} | {6,10} |",
					test.Framework, "MemStream Serialize", sw.ElapsedMilliseconds, c0, c1, c2, size);
			}

			/* Deerialize part */

			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();

				var c0 = GC.CollectionCount(0);
				var c1 = GC.CollectionCount(1);
				var c2 = GC.CollectionCount(2);

				var sw = Stopwatch.StartNew();

				var received = test.Deserialize();

				sw.Stop();

				c0 = GC.CollectionCount(0) - c0;
				c1 = GC.CollectionCount(1) - c1;
				c2 = GC.CollectionCount(2) - c2;

				Console.WriteLine("{0,-13} | {1,-21} | {2,10} | {3,3} {4,3} {5,3} | {6,10} |",
					test.Framework, "MemStream Deserialize", sw.ElapsedMilliseconds, c0, c1, c2, "");

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

		static void Test(INetTest test, MessageBase[] msgs)
		{
			test.Prepare(msgs.Length);

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
				test.Framework, "NetTest", sw.ElapsedMilliseconds, c0, c1, c2, "");

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
