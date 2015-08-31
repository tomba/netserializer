using NetSerializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Test
{
	class TriDimArrayCustomSerializer : IStaticTypeSerializer
	{
		public bool Handles(Type type)
		{
			return type == typeof(int[,,]);
		}

		public IEnumerable<Type> GetSubtypes(Type type)
		{
			yield break;
		}

		public MethodInfo GetStaticWriter(Type type)
		{
			return typeof(TriDimArrayCustomSerializer).GetMethod("WritePrimitive",
				BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.ExactBinding, null,
				new Type[] { typeof(Stream), type }, null);
		}

		public MethodInfo GetStaticReader(Type type)
		{
			return typeof(TriDimArrayCustomSerializer).GetMethod("ReadPrimitive",
				BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.ExactBinding, null,
				new Type[] { typeof(Stream), type.MakeByRefType() }, null);
		}

		static void WritePrimitive(Stream stream, int[,,] value)
		{
			if (value == null)
			{
				Primitives.WritePrimitive(stream, (uint)0);
				return;
			}

			int l1 = value.GetLength(0);
			int l2 = value.GetLength(1);
			int l3 = value.GetLength(2);

			Primitives.WritePrimitive(stream, (uint)l1 + 1);
			Primitives.WritePrimitive(stream, (uint)l2);
			Primitives.WritePrimitive(stream, (uint)l3);

			for (int z = 0; z < l1; ++z)
				for (int y = 0; y < l2; ++y)
					for (int x = 0; x < l3; ++x)
						Primitives.WritePrimitive(stream, value[z, y, x]);
		}

		static void ReadPrimitive(Stream stream, out int[,,] value)
		{
			uint l1, l2, l3;

			Primitives.ReadPrimitive(stream, out l1);

			if (l1 == 0)
			{
				value = null;
				return;
			}

			l1 -= 1;

			Primitives.ReadPrimitive(stream, out l2);
			Primitives.ReadPrimitive(stream, out l3);

			value = new int[l1, l2, l3];

			for (int z = 0; z < l1; ++z)
				for (int y = 0; y < l2; ++y)
					for (int x = 0; x < l3; ++x)
						Primitives.ReadPrimitive(stream, out value[z, y, x]);
		}
	}
}
