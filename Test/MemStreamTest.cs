using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Test
{
	class MemStreamTest<T>
	{
		MemoryStream m_stream;

		public ISerializerSpecimen Specimen { get; private set; }

		public MemStreamTest(ISerializerSpecimen specimen)
		{
			this.Specimen = specimen;
		}

		public void Prepare()
		{
			m_stream = new MemoryStream();
		}

		public long Serialize(T[] msgs)
		{
			m_stream.Position = 0;

			this.Specimen.Serialize(m_stream, msgs);

			m_stream.Flush();

			return m_stream.Position;
		}

		public void Deserialize(T[] msgs)
		{
			m_stream.Position = 0;

			this.Specimen.Deserialize(m_stream, msgs);
		}
	}
}
