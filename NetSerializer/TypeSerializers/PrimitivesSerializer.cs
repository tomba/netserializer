using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NetSerializer
{
	public class PrimitivesSerializer : IStaticTypeSerializer
	{
		static Type[] s_primitives = new Type[] {
				typeof(bool),
				typeof(byte), typeof(sbyte),
				typeof(char),
				typeof(ushort), typeof(short),
				typeof(uint), typeof(int),
				typeof(ulong), typeof(long),
				typeof(float), typeof(double),
				typeof(string),
				typeof(DateTime),
				typeof(byte[]),
			};

		public bool Handles(Type type)
		{
			return s_primitives.Contains(type);
		}

		public IEnumerable<Type> GetSubtypes(Type type)
		{
			yield break;
		}

		public void GetStaticMethods(Type type, out MethodInfo writer, out MethodInfo reader)
		{
			var containerType = typeof(Primitives);

			writer = Primitives.GetWritePrimitive(type);
			reader = Primitives.GetReaderPrimitive(type);
		}

		public static IEnumerable<Type> GetSupportedTypes()
		{
			return s_primitives;
		}
	}
}
