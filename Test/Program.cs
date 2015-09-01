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
		internal static bool EnableResultCheck = false;

		static int NumThreads = 1;
		static bool ShareSerializer = false;

		static NS.Serializer s_sharedSerializer;

		static void Main(string[] args)
		{
			if (ParseArgs(args) == false)
				return;

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

		static bool ParseArgs(string[] args)
		{
			bool show_help = false;

			var p = new Mono.Options.OptionSet() {
				{ "q|quick", "quick run", _ => QuickRun = true },
				{ "p|protobuf", "run protobuf tests", _ => RunProtoBufTests = true },
				{ "v|verify", "verify results", _ => EnableResultCheck = true },
				{ "threads=", "number of threads", (int v) => NumThreads = v },
				{ "share", "share serializer between threads", _ => ShareSerializer = true },
				{ "h|help",  "show help", _ => show_help = true },
			};

			List<string> extra;

			try
			{
				extra = p.Parse(args);
			}
			catch (Mono.Options.OptionException e)
			{
				Console.WriteLine(e.Message);
				return false;
			}

			if (show_help || extra.Count > 0)
			{
				p.WriteOptionDescriptions(Console.Out);
				return false;
			}

			return true;
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
