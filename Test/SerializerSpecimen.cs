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
		bool CanRun(Type type, bool direct);
		void Warmup<T>(T[] msgs);
		void Serialize<T>(Stream stream, T[] msgs);
		void Deserialize<T>(Stream stream, T[] msgs);
		void SerializeDirect<T>(Stream stream, T[] msgs);
		void DeserializeDirect<T>(Stream stream, T[] msgs);
	}

	class NetSerializerSpecimen : ISerializerSpecimen
	{
		NS.Serializer m_serializer;

		public NetSerializerSpecimen(NS.Serializer serializer)
		{
			m_serializer = serializer;
		}

		public string Name { get { return "NetSerializer"; } }

		public bool CanRun(Type type, bool direct)
		{
			return true;
		}

		public void Warmup<T>(T[] msgs)
		{
			using (var stream = new MemoryStream())
			{
				Serialize(stream, msgs);

				stream.Position = 0;

				Deserialize(stream, msgs);
			}
		}

		public void Serialize<T>(Stream stream, T[] msgs)
		{
			foreach (var msg in msgs)
				m_serializer.Serialize(stream, msg);
		}

		public void Deserialize<T>(Stream stream, T[] msgs)
		{
			for (int i = 0; i < msgs.Length; ++i)
				msgs[i] = (T)m_serializer.Deserialize(stream);
		}

		public void SerializeDirect<T>(Stream stream, T[] msgs)
		{
			foreach (T msg in msgs)
				m_serializer.SerializeDirect(stream, msg);
		}

		public void DeserializeDirect<T>(Stream stream, T[] msgs)
		{
			for (int i = 0; i < msgs.Length; ++i)
				m_serializer.DeserializeDirect<T>(stream, out msgs[i]);
		}
	}

	class ProtobufSpecimen : ISerializerSpecimen
	{
		public string Name { get { return "protobuf-net"; } }

		public bool CanRun(Type type, bool direct)
		{
			if (direct)
				return false;

			if (type.IsPrimitive || type == typeof(Guid))
				return true;

			return type.GetCustomAttributes(typeof(PB.ProtoContractAttribute), false).Any();
		}

		public void Warmup<T>(T[] msgs)
		{
			using (var stream = new MemoryStream())
			{
				Serialize(stream, msgs);

				stream.Position = 0;

				Deserialize(stream, msgs);
			}
		}

		public void Serialize<T>(Stream stream, T[] msgs)
		{
			foreach (var msg in msgs)
				PB.Serializer.SerializeWithLengthPrefix(stream, msg, PB.PrefixStyle.Base128);
		}

		public void Deserialize<T>(Stream stream, T[] msgs)
		{
			for (int i = 0; i < msgs.Length; ++i)
				msgs[i] = PB.Serializer.DeserializeWithLengthPrefix<T>(stream, PB.PrefixStyle.Base128);
		}

		/* hum I guess direct serializing cannot be done with protobuf */

		public void SerializeDirect<T>(Stream stream, T[] msgs)
		{
			foreach (T msg in msgs)
				PB.Serializer.SerializeWithLengthPrefix(stream, msg, PB.PrefixStyle.Base128);
		}

		public void DeserializeDirect<T>(Stream stream, T[] msgs)
		{
			for (int i = 0; i < msgs.Length; ++i)
				msgs[i] = PB.Serializer.DeserializeWithLengthPrefix<T>(stream, PB.PrefixStyle.Base128);
		}
	}
}
