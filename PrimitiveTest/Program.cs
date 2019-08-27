using System;
using System.IO;
using System.Diagnostics;

namespace PrimitiveTest
{
	class Program
	{
		static void Main(string[] args)
		{
			var stream = new MemoryStream();

			double v1 = 1.0;
			double v2 = 0.00000024312;
			double v3 = 38423423434.434;
			double v4 = .0;

			int loops = 10000000;

			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();

				var sw = Stopwatch.StartNew();

				for (int i = 0; i < loops; ++i)
				{
					NetSerializer.Primitives.WritePrimitive(stream, v1);
					NetSerializer.Primitives.WritePrimitive(stream, v2);
					NetSerializer.Primitives.WritePrimitive(stream, v3);
					NetSerializer.Primitives.WritePrimitive(stream, v4);
				}

				sw.Stop();

				Console.WriteLine("Writing {0} ms", sw.ElapsedMilliseconds);
			}

			long size = stream.Position;

			Console.WriteLine("Size {0}", size);

			stream.Position = 0;

			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();

				var sw = Stopwatch.StartNew();

				for (int i = 0; i < loops; ++i)
				{
					NetSerializer.Primitives.ReadPrimitive(stream, out v1);
					NetSerializer.Primitives.ReadPrimitive(stream, out v2);
					NetSerializer.Primitives.ReadPrimitive(stream, out v3);
					NetSerializer.Primitives.ReadPrimitive(stream, out v4);
				}

				sw.Stop();

				Console.WriteLine("Reading {0} ms", sw.ElapsedMilliseconds);
			}

			//Console.WriteLine("done");
			//Console.ReadLine();
		}
	}
}
