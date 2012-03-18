using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using NetSerializer;
using System.Diagnostics;
using System.IO;

namespace Test
{
	class NetTest : ITest
	{
		MessageBase[] m_sent;
		MessageBase[] m_received;

		Thread m_server;
		Thread m_client;

		ManualResetEvent m_ev;

		public void Prepare(int numMessages)
		{
			m_received = new MessageBase[numMessages];

			m_ev = new ManualResetEvent(false);

			m_server = new Thread(ServerMain);
			m_server.Start();

			Thread.Sleep(100);

			m_client = new Thread(ClientMain);
			m_client.Start();
		}

		public MessageBase[] Test(MessageBase[] msgs)
		{
			m_sent = msgs;

			m_ev.Set();

			m_client.Join();
			m_server.Join();

			return m_received;
		}

		public long Size { get { return 0; } }

		void ServerMain()
		{
			var listener = new TcpListener(IPAddress.Loopback, 9999);
			listener.Start();
			var c = listener.AcceptTcpClient();

			using (var stream = c.GetStream())
			using (var bufStream = new BufferedStream(stream))
			{
				for (int i = 0; i < m_received.Length; ++i)
					m_received[i] = (MessageBase)Serializer.Deserialize(bufStream);
			}

			listener.Stop();
		}

		void ClientMain()
		{
			var c = new TcpClient();
			c.Connect(IPAddress.Loopback, 9999);

			using (var netStream = c.GetStream())
			using (var bufStream = new BufferedStream(netStream))
			{
				m_ev.WaitOne();

				for (int i = 0; i < m_sent.Length; ++i)
					Serializer.Serialize(bufStream, m_sent[i]);
			}

			c.Close();
		}
	}
}
