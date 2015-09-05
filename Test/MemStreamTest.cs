using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Test
{
	class MemStreamTest<T> : IDisposable
	{
		MemoryStream m_stream;

		public ISerializerSpecimen Specimen { get; private set; }

		public MemStreamTest(ISerializerSpecimen specimen)
		{
			this.Specimen = specimen;
		}

		public void Dispose()
		{
			if (m_stream != null)
			{
				m_stream.Dispose();
				m_stream = null;
			}
		}

		public void Prepare()
		{
			m_stream = new MemoryStream();
		}

		public long Serialize(T[] msgs, bool direct)
		{
			m_stream.Position = 0;

			if (direct)
				this.Specimen.SerializeDirect(m_stream, msgs);
			else
				this.Specimen.Serialize(m_stream, msgs);

			m_stream.Flush();

			return m_stream.Position;
		}

		public void Deserialize(T[] msgs, bool direct)
		{
			m_stream.Position = 0;

			if (direct)
				this.Specimen.DeserializeDirect(m_stream, msgs);
			else
				this.Specimen.Deserialize(m_stream, msgs);
		}
	}
}
