using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetSerializer;
using System.IO;
using System.Diagnostics;

namespace Test
{
	interface ITest
	{
		void Prepare(int numMessages);
		MessageBase[] Test(MessageBase[] msgs);
		void Cleanup();
	}

	class Program
	{
		static void Main(string[] args)
		{
			System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;

			var types = typeof(MessageBase).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(MessageBase))).ToArray();

			Serializer.Initialize(types);

			int numMessages = 1111111;

			var msgs = MessageBase.CreateMessages(numMessages).ToArray();
			//var msgs = MessageBase.CreateSimpleMessages(numMessages).ToArray();

			Test(new MemStreamTest(), msgs);
			Test(new PBMemStreamTest(), msgs);

			//Console.WriteLine("Press enter to quit");
			//Console.ReadLine();
		}

		static void Test(ITest test, MessageBase[] msgs)
		{
			Console.WriteLine(test.GetType().Name);

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

			Console.WriteLine("Time {0} ms. GC {1}, {2}, {3}", sw.ElapsedMilliseconds, c0, c1, c2);

			test.Cleanup();

			for (int i = 0; i < msgs.Length; ++i)
			{
				var msg1 = msgs[i];
				var msg2 = received[i];

				msg1.Compare(msg2);
			}
		}

	}
}
