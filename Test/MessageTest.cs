using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Test
{
	class MessageTest<T> : ITest
	{
		Type m_messageType;
		int m_numMessages;
		int m_loops;

		Func<MyRandom, T> m_creator;
		Action<T, T> m_comparer;

		T[] m_messages;

		public MessageTest(int numMessages, int loops)
			: this(numMessages, loops, null, null)
		{
		}

		public MessageTest(int numMessages, int loops, Func<MyRandom, T> creator, Action<T, T> comparer)
		{
			m_messageType = typeof(T);
			m_numMessages = numMessages;
			m_loops = loops;

			if (creator == null)
			{
				var method = typeof(T).GetMethod("Create", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
				creator = (Func<MyRandom, T>)Delegate.CreateDelegate(typeof(Func<MyRandom, T>), method);
			}

			m_creator = creator;

			if (comparer == null)
			{
				var method = typeof(T).GetMethod("Compare", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
				if (method != null)
					comparer = (Action<T, T>)Delegate.CreateDelegate(typeof(Action<T, T>), method);
			}

			m_comparer = comparer;
		}

		public bool CanRun(ISerializerSpecimen specimen)
		{
			return specimen.CanRun(m_messageType);
		}

		public void Prepare()
		{
			if (Program.QuickRun)
			{
				m_numMessages = Math.Min(10, m_numMessages);
				m_loops = 1;
			}

			var r = new MyRandom(123);

			var msgs = new T[m_numMessages];
			for (int i = 0; i < msgs.Length; ++i)
				msgs[i] = m_creator(r);
			m_messages = msgs;

			Console.WriteLine("== {0} {1} x {2} ==", m_numMessages, m_messageType.Name, m_loops);
		}

		public void Unprepare()
		{
			m_messages = null;
		}

		public void Run(ISerializerSpecimen specimen)
		{
			MyRandom rand = new MyRandom(123);

			var arr = m_messages.Take(m_numMessages > 10 ? 10 : 1).ToArray();
			specimen.Warmup(arr);

			using (var test = new MemStreamTest<T>(specimen))
				Test(test, m_messages, m_loops);

			using (var test = new NetTest<T>(specimen))
				Test(test, m_messages, m_loops);
		}

		void Test(MemStreamTest<T> test, T[] msgs, int loops)
		{
			test.Prepare();

			/* Serialize part */
			{
				Console.Write("{0,-13} | {1,-21} | ", test.Specimen.Name, "MemStream Serialize");
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

				Console.WriteLine("{0,10} | {1,3} {2,3} {3,3} | {4,10} |", sw.ElapsedMilliseconds, c0, c1, c2, size);
			}

			/* Deserialize part */

			{
				var received = new T[msgs.Length];

				Console.Write("{0,-13} | {1,-21} | ", test.Specimen.Name, "MemStream Deserialize");
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

				Console.WriteLine("{0,10} | {1,3} {2,3} {3,3} | {4,10} |", sw.ElapsedMilliseconds, c0, c1, c2, "");

				if (Program.EnableResultCheck)
					CompareMessages(msgs, received);
			}
		}

		void Test(NetTest<T> test, T[] msgs, int loops)
		{
			test.Prepare(msgs.Length);

			Console.Write("{0,-13} | {1,-21} | ", test.Specimen.Name, "NetTest");
			Console.Out.Flush();

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			var c0 = GC.CollectionCount(0);
			var c1 = GC.CollectionCount(1);
			var c2 = GC.CollectionCount(2);

			var sw = Stopwatch.StartNew();

			var received = test.Test(msgs, loops);

			sw.Stop();

			c0 = GC.CollectionCount(0) - c0;
			c1 = GC.CollectionCount(1) - c1;
			c2 = GC.CollectionCount(2) - c2;

			Console.WriteLine("{0,10} | {1,3} {2,3} {3,3} | {4,10} |", sw.ElapsedMilliseconds, c0, c1, c2, "");

			if (Program.EnableResultCheck)
				CompareMessages(msgs, received);
		}

		void CompareMessages(T[] msgs1, T[] msgs2)
		{
			if (msgs1.Length != msgs2.Length)
				throw new Exception();

			for (int i = 0; i < msgs1.Length; ++i)
				m_comparer(msgs1[i], msgs2[i]);
		}
	}
}
