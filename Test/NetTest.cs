using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using NetSerializer;
using System.Diagnostics;

namespace Test
{
	class NetTest
	{
		static MessageBase[] s_sent;
		static MessageBase[] s_received;

		public static void Test()
		{
			int numMessages = 1000;

			s_sent = MessageBase.CreateMessages(numMessages).ToArray();
			s_received = new MessageBase[numMessages];

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			var c0 = GC.CollectionCount(0);
			var c1 = GC.CollectionCount(1);
			var c2 = GC.CollectionCount(2);

			var sw = Stopwatch.StartNew();

			var server = new Thread(ServerMain);
			server.Start();

			Thread.Sleep(100);

			var client = new Thread(ClientMain);
			client.Start();

			client.Join();
			server.Join();

			sw.Stop();

			c0 = GC.CollectionCount(0) - c0;
			c1 = GC.CollectionCount(1) - c1;
			c2 = GC.CollectionCount(2) - c2;

			Console.WriteLine("NetTest took {0} ms. GC {1}, {2}, {3}", sw.ElapsedMilliseconds, c0, c1, c2);

			for (int i = 0; i < numMessages; ++i)
			{
				var msg1 = s_sent[i];
				var msg2 = s_received[i];

				msg1.Compare(msg2);
			}
		}

		static void ServerMain()
		{
			var listener = new TcpListener(IPAddress.Loopback, 9999);
			listener.Start();
			var c = listener.AcceptTcpClient();

			var stream = c.GetStream();

			for (int i = 0; i < s_received.Length; ++i)
				s_received[i] = (MessageBase)Serializer.Deserialize(stream);

			listener.Stop();
		}

		static void ClientMain()
		{
			var c = new TcpClient();
			c.Connect(IPAddress.Loopback, 9999);

			var stream = c.GetStream();

			for (int i = 0; i < s_sent.Length; ++i)
				Serializer.Serialize(stream, s_sent[i]);

			stream.Close();
			c.Close();
		}
	}
}
