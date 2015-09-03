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
			var serializer = new NS.Serializer(types, new NS.ITypeSerializer[] { new TriDimArrayCustomSerializer() });
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

		readonly ITest[] m_tests = new ITest[]
		{
			new MessageTest<U8Message>(100, 100000),
			new MessageTest<S16Message>(100, 100000),
			new MessageTest<S32Message>(100, 100000),
			new MessageTest<S64Message>(100, 100000),
			new MessageTest<DecimalMessage>(100, 50000),
			new MessageTest<NullableDecimalMessage>(100, 100000),
			new MessageTest<PrimitivesMessage>(100, 10000),
			new MessageTest<DictionaryMessage>(10, 1000),
			new MessageTest<ComplexMessage>(100, 10000),
			new MessageTest<StringMessage>(100, 20000),
			new MessageTest<StructMessage>(100, 20000),
			new MessageTest<BoxedPrimitivesMessage>(100, 20000),
			new MessageTest<ByteArrayMessage>(10000, 1),
			new MessageTest<IntArrayMessage>(1000, 1),
			new MessageTest<TriDimArrayCustomSerializersMessage>(10, 100),
		};

		public void Run()
		{
			foreach (var test in m_tests)
			{
				test.Prepare();

				foreach (var specimen in m_specimens)
				{
					if (test.CanRun(specimen) == false)
						continue;

					test.Run(specimen);
				}

				test.Unprepare();
			}

			//Console.WriteLine("Press enter to quit");
			//Console.ReadLine();
		}
	}
}
