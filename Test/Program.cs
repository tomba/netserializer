using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using NS = NetSerializer;

namespace Test
{
	static class Program
	{
		internal static bool RunProtoBufTests = false;
		internal static bool QuickRun = false;

		const int NumThreads = 1;
		static bool ShareSerializer = false;

		static NS.Serializer s_sharedSerializer;

		static void Main(string[] args)
		{
			if (ShareSerializer)
				s_sharedSerializer = Tester.CreateSerializer();

			List<Thread> threads = new List<Thread>();

			for (int i = 0; i < NumThreads; ++i)
			{
				var thread = new Thread(Test);
				threads.Add(thread);
			}

			foreach (var thread in threads)
				thread.Start();

			foreach (var thread in threads)
				thread.Join();
		}

		static void Test()
		{
			Tester tester;

			if (ShareSerializer)
				tester = new Tester(s_sharedSerializer);
			else
				tester = new Tester();

			tester.Run();
		}
	}
}
