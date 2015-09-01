using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.IO;
using NS = NetSerializer;

namespace Test
{
	interface INetTest
	{
		string Framework { get; }
		void Prepare(int numMessages, int loops);
		MessageBase[] Test(MessageBase[] msgs);
	}

	class NetTest : INetTest
	{
		NS.Serializer m_serializer;

		int m_loops;
		MessageBase[] m_sent;
		MessageBase[] m_received;

		Thread m_server;
		Thread m_client;

		ManualResetEvent m_ev;

		TcpListener m_listener;
		int m_port;

		public NetTest(NS.Serializer serializer)
		{
			m_serializer = serializer;
		}

		public string Framework { get { return "NetSerializer"; } }

		public void Prepare(int numMessages, int loops)
		{
			m_received = new MessageBase[numMessages];
			m_loops = loops;

			m_ev = new ManualResetEvent(false);

			m_listener = new TcpListener(IPAddress.Loopback, 0);
			m_listener.Start();
			m_port = ((IPEndPoint)m_listener.LocalEndpoint).Port;

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

			m_listener.Stop();

			return m_received;
		}

		void ServerMain()
		{
			var c = m_listener.AcceptTcpClient();

			using (var stream = c.GetStream())
			using (var bufStream = new BufferedStream(stream))
			{
				for (int l = 0; l < m_loops; ++l)
					for (int i = 0; i < m_received.Length; ++i)
						m_received[i] = (MessageBase)m_serializer.Deserialize(bufStream);
			}
		}

		void ClientMain()
		{
			var c = new TcpClient();
			c.Connect(IPAddress.Loopback, m_port);

			using (var netStream = c.GetStream())
			using (var bufStream = new BufferedStream(netStream))
			{
				m_ev.WaitOne();

				for (int l = 0; l < m_loops; ++l)
					for (int i = 0; i < m_sent.Length; ++i)
						m_serializer.Serialize(bufStream, m_sent[i]);
			}

			c.Close();
		}
	}
}
