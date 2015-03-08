using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Test
{
	class MemStreamTest : IMemStreamTest
	{
		MessageBase[] m_received;
		MemoryStream m_stream;

		public string Framework { get { return "NetSerializer"; } }

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
				Program.Serializer.Serialize(m_stream, msg);

			m_stream.Flush();

			return m_stream.Position;
		}

		public MessageBase[] Deserialize()
		{
			int numMessages = m_received.Length;

			m_stream.Position = 0;

			for (int i = 0; i < numMessages; ++i)
				m_received[i] = (MessageBase)Program.Serializer.Deserialize(m_stream);

			return m_received;
		}
	}
}
