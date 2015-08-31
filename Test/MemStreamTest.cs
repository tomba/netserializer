using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NS = NetSerializer;

namespace Test
{
	interface IMemStreamTest
	{
		string Framework { get; }
		void Warmup(MessageBase[] msgs);
		void Prepare(int numMessages);
		long Serialize(MessageBase[] msgs);
		MessageBase[] Deserialize();
	}

	class MemStreamTest : IMemStreamTest
	{
		NS.Serializer m_serializer;
		MessageBase[] m_received;
		MemoryStream m_stream;

		public MemStreamTest(NS.Serializer serializer)
		{
			m_serializer = serializer;
		}

		public string Framework { get { return "NetSerializer"; } }

		public void Warmup(MessageBase[] msgs)
		{
			using (var stream = new MemoryStream())
			{
				int n = msgs.Length > 10 ? 10 : msgs.Length;

				for (int i = 0; i < n; ++i)
					m_serializer.Serialize(stream, msgs[i]);
				stream.Position = 0;
				for (int i = 0; i < n; ++i)
					m_serializer.Deserialize(stream);
			}
		}

		public void Prepare(int numMessages)
		{
			m_received = new MessageBase[numMessages];
			m_stream = new MemoryStream();
		}

		public long Serialize(MessageBase[] msgs)
		{
			int numMessages = msgs.Length;

			m_stream.Position = 0;

			foreach (var msg in msgs)
				m_serializer.Serialize(m_stream, msg);

			m_stream.Flush();

			return m_stream.Position;
		}

		public MessageBase[] Deserialize()
		{
			int numMessages = m_received.Length;

			m_stream.Position = 0;

			for (int i = 0; i < numMessages; ++i)
				m_received[i] = (MessageBase)m_serializer.Deserialize(m_stream);

			return m_received;
		}
	}
}
