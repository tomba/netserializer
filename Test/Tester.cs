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

			//NS.Serializer.GenerateDebugAssembly(types, new NS.ITypeSerializer[] { new TriDimArrayCustomSerializer() });

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

		static readonly ITestSpec[] s_testSpecs = new ITestSpec[]
		{
			new MessageTestSpec<U8Message>(100, 100000),
			new MessageTestSpec<S16Message>(100, 100000),
			new MessageTestSpec<S32Message>(100, 100000),
			new MessageTestSpec<S64Message>(100, 100000),
			new MessageTestSpec<DecimalMessage>(100, 50000),
			new MessageTestSpec<NullableDecimalMessage>(100, 100000),
			new MessageTestSpec<PrimitivesMessage>(100, 10000),
			new MessageTestSpec<DictionaryMessage>(10, 1000),
			new MessageTestSpec<ComplexMessage>(100, 10000),
			new MessageTestSpec<StringMessage>(100, 20000),
			new MessageTestSpec<StructMessage>(100, 20000),
			new MessageTestSpec<BoxedPrimitivesMessage>(100, 20000),
			new MessageTestSpec<ByteArrayMessage>(10000, 1),
			new MessageTestSpec<IntArrayMessage>(1000, 1),
			new MessageTestSpec<TriDimArrayCustomSerializersMessage>(10, 100),
		};

		public void Run()
		{
			foreach (var spec in s_testSpecs)
			{
				var test = spec.Create();

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
