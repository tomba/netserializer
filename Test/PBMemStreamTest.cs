using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ProtoBuf;

namespace Test
{
	class PBMemStreamTest : IMemStreamTest
	{
		MessageBase[] m_received;
		MemoryStream m_stream;

		public string Framework { get { return "protobuf-net"; } }

		public void Warmup(MessageBase[] msgs)
		{
			using (var stream = new MemoryStream())
			{
				int n = msgs.Length > 10 ? 10 : msgs.Length;

				for (int i = 0; i < n; ++i)
					Serializer.SerializeWithLengthPrefix(stream, msgs[i], PrefixStyle.Base128);
				stream.Position = 0;
				for (int i = 0; i < n; ++i)
					Serializer.DeserializeWithLengthPrefix<MessageBase>(stream, PrefixStyle.Base128);
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
				Serializer.SerializeWithLengthPrefix(m_stream, msg, PrefixStyle.Base128);

			m_stream.Flush();

			return m_stream.Position;
		}

		public MessageBase[] Deserialize()
		{
			int numMessages = m_received.Length;

			m_stream.Position = 0;

			for (int i = 0; i < numMessages; ++i)
				m_received[i] = Serializer.DeserializeWithLengthPrefix<MessageBase>(m_stream, PrefixStyle.Base128);

			return m_received;
		}
	}
}
