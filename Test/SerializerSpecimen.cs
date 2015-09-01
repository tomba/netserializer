using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NS = NetSerializer;
using PB = ProtoBuf;

namespace Test
{
	interface ISerializerSpecimen
	{
		string Name { get; }
		bool CanRun(Type type);
		void Serialize(Stream stream, MessageBase[] msgs);
		void Deserialize(Stream stream, MessageBase[] msgs, int numMessages);
	}

	class NetSerializerSpecimen : ISerializerSpecimen
	{
		NS.Serializer m_serializer;

		public NetSerializerSpecimen(NS.Serializer serializer)
		{
			m_serializer = serializer;
		}

		public string Name { get { return "NetSerializer"; } }

		public bool CanRun(Type type)
		{
			return true;
		}

		public void Serialize(Stream stream, MessageBase[] msgs)
		{
			foreach (var msg in msgs)
				m_serializer.Serialize(stream, msg);
		}

		public void Deserialize(Stream stream, MessageBase[] msgs, int numMessages)
		{
			for (int i = 0; i < numMessages; ++i)
				msgs[i] = (MessageBase)m_serializer.Deserialize(stream);
		}
	}

	class ProtobufSpecimen : ISerializerSpecimen
	{
		public string Name { get { return "protobuf-net"; } }

		public bool CanRun(Type type)
		{
			return type.GetCustomAttributes(typeof(PB.ProtoContractAttribute), false).Any();
		}

		public void Serialize(Stream stream, MessageBase[] msgs)
		{
			foreach (var msg in msgs)
				PB.Serializer.SerializeWithLengthPrefix(stream, msg, PB.PrefixStyle.Base128);
		}

		public void Deserialize(Stream stream, MessageBase[] msgs, int numMessages)
		{
			for (int i = 0; i < numMessages; ++i)
				msgs[i] = PB.Serializer.DeserializeWithLengthPrefix<MessageBase>(stream, PB.PrefixStyle.Base128);
		}
	}
}
