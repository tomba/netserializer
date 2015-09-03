using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.IO;

namespace Test
{
	class NetTest<T>
	{
		public ISerializerSpecimen Specimen { get; private set; }

		int m_loops;
		T[] m_sent;
		T[] m_received;

		Thread m_server;
		Thread m_client;

		ManualResetEvent m_ev;

		TcpListener m_listener;
		int m_port;

		public NetTest(ISerializerSpecimen specimen)
		{
			this.Specimen = specimen;
		}

		public void Prepare(int numMessages)
		{
			m_received = new T[numMessages];

			m_ev = new ManualResetEvent(false);

			m_listener = new TcpListener(IPAddress.Loopback, 0);
			m_listener.Start();
			m_port = ((IPEndPoint)m_listener.LocalEndpoint).Port;

			m_server = new Thread(ServerMain);
			m_server.Start();

			m_client = new Thread(ClientMain);
			m_client.Start();
		}

		public T[] Test(T[] msgs, int loops)
		{
			m_sent = msgs;
			m_loops = loops;

			Thread.MemoryBarrier();

			m_ev.Set();

			m_client.Join();
			m_server.Join();

			m_listener.Stop();

			return m_received;
		}

		void ServerMain()
		{
			var c = m_listener.AcceptTcpClient();

			m_ev.WaitOne();

			using (var stream = c.GetStream())
			using (var bufStream = new BufferedStream(stream))
			{
				for (int l = 0; l < m_loops; ++l)
					this.Specimen.Deserialize(bufStream, m_received);
			}
		}

		void ClientMain()
		{
			var c = new TcpClient();
			c.Connect(IPAddress.Loopback, m_port);

			m_ev.WaitOne();

			using (var netStream = c.GetStream())
			using (var bufStream = new BufferedStream(netStream))
			{
				for (int l = 0; l < m_loops; ++l)
					this.Specimen.Serialize(bufStream, m_sent);
			}

			c.Close();
		}
	}
}
