using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NetSerializer;

namespace Test
{
	class MemStreamTest : ITest
	{
		MessageBase[] m_received;
		MemoryStream m_stream;

		public void Prepare(int numMessages)
		{
			m_received = new MessageBase[numMessages];
			m_stream = new MemoryStream();
		}

		public MessageBase[] Test(MessageBase[] msgs)
		{
			int numMessages = msgs.Length;

			m_stream.Position = 0;

			foreach (var msg in msgs)
				Serializer.Serialize(m_stream, msg);

			m_stream.Position = 0;

			for (int i = 0; i < numMessages; ++i)
				m_received[i] = (MessageBase)Serializer.Deserialize(m_stream);

			return m_received;
		}

		public long Size { get { return m_stream.Position; } }
	}
}
