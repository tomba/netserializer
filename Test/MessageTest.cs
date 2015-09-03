using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Test
{
	class MessageTest<T> : ITest
	{
		public Type MessageType { get; private set; }
		public int NumMessages { get; private set; }
		public int Loops { get; private set; }

		Func<MyRandom, T> m_creator;
		Action<T, T> m_comparer;

		public T[] Messages { get; protected set; }

		public MessageTest(int numMessages, int loops)
			: this(numMessages, loops, null, null)
		{
		}

		public MessageTest(int numMessages, int loops, Func<MyRandom, T> creator, Action<T, T> comparer)
		{
			this.MessageType = typeof(T);
			this.NumMessages = numMessages;
			this.Loops = loops;

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
			return specimen.CanRun(this.MessageType);
		}

		public void Prepare()
		{
			if (Program.QuickRun)
			{
				this.NumMessages = Math.Min(10, this.NumMessages);
				this.Loops = 1;
			}

			var r = new MyRandom(123);

			var msgs = new T[this.NumMessages];
			for (int i = 0; i < msgs.Length; ++i)
				msgs[i] = m_creator(r);
			this.Messages = msgs;

			Console.WriteLine("== {0} {1} x {2} ==", this.NumMessages, this.MessageType.Name, this.Loops);
		}

		public void Unprepare()
		{
			this.Messages = null;
		}

		public void Run(ISerializerSpecimen specimen)
		{
			MyRandom rand = new MyRandom(123);

			var arr = this.Messages.Take(this.NumMessages > 10 ? 10 : 1).ToArray();
			specimen.Warmup(arr);

			Test(new MemStreamTest<T>(specimen), this.Messages, this.Loops);
			Test(new NetTest<T>(specimen), this.Messages, this.Loops);
		}

		void Test(MemStreamTest<T> test, T[] msgs, int loops)
		{
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

			/* Deserialize part */

			{
				var received = new T[msgs.Length];

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
					CompareMessages(msgs, received);
			}
		}

		void Test(NetTest<T> test, T[] msgs, int loops)
		{
			test.Prepare(msgs.Length);

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

			Console.WriteLine("{0,-13} | {1,-21} | {2,10} | {3,3} {4,3} {5,3} | {6,10} |",
				test.Specimen.Name, "NetTest", sw.ElapsedMilliseconds, c0, c1, c2, "");

			if (Program.EnableResultCheck)
				CompareMessages(msgs, received);
		}

		void CompareMessages(T[] msgs1, T[] msgs2)
		{
			if (m_comparer == null)
				return;

			if (msgs1.Length != msgs2.Length)
				throw new Exception();

			for (int i = 0; i < msgs1.Length; ++i)
				m_comparer(msgs1[i], msgs2[i]);
		}
	}
}
