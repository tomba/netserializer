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
		long Size { get; }
	}

	class Program
	{
		static void Main(string[] args)
		{
			System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;

			var types = typeof(MessageBase).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(MessageBase))).ToArray();

			Serializer.Initialize(types);

			MessageBase[] msgs;

			msgs = MessageBase.CreateSimpleMessages(2000000).ToArray();
			RunTests("SimpleMessages", msgs);

			msgs = MessageBase.CreateMessages(300000).ToArray();
			RunTests("Messages", msgs);

			msgs = MessageBase.CreateLongMessages(1000).ToArray();
			RunTests("LongMessages", msgs);

			//Console.WriteLine("Press enter to quit");
			//Console.ReadLine();
		}

		static void RunTests(string name, MessageBase[] msgs)
		{
			Console.WriteLine("== {0}, {1} ==", name, msgs.Length);

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			Test(new MemStreamTest(), msgs);
			Test(new PBMemStreamTest(), msgs);

			Test(new NetTest(), msgs);
			Test(new PBNetTest(), msgs);
		}

		static void Test(ITest test, MessageBase[] msgs)
		{
			Console.Write("{0,-20}", test.GetType().Name);

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

			Console.WriteLine(" | {0,10} | {1,3} {2,3} {3,3} | {4,10} |", sw.ElapsedMilliseconds, c0, c1, c2, test.Size);

			for (int i = 0; i < msgs.Length; ++i)
			{
				var msg1 = msgs[i];
				var msg2 = received[i];

				msg1.Compare(msg2);
			}
		}

	}
}
