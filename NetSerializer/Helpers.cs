using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NetSerializer
{
	static class Helpers
	{
		public static MethodInfo GetWritePrimitive(Type containerType, Type type)
		{
			if (type.IsEnum)
				type = Enum.GetUnderlyingType(type);

			MethodInfo writer = containerType.GetMethod("WritePrimitive", BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null,
				new Type[] { typeof(Stream), type }, null);

			if (writer != null)
				return writer;

			if (type.IsGenericType)
			{
				var genType = type.GetGenericTypeDefinition();

				var mis = containerType.GetMethods(BindingFlags.Static | BindingFlags.Public)
					.Where(mi => mi.IsGenericMethod && mi.Name == "WritePrimitive");

				foreach (var mi in mis)
				{
					var p = mi.GetParameters();

					if (p.Length != 2)
						continue;

					if (p[0].ParameterType != typeof(Stream))
						continue;

					var paramType = p[1].ParameterType;

					if (paramType.IsGenericType == false)
						continue;

					var genParamType = paramType.GetGenericTypeDefinition();

					if (genType == genParamType)
						return mi;
				}
			}

			return null;
		}

		public static MethodInfo GetReadPrimitive(Type containerType, Type type)
		{
			if (type.IsEnum)
				type = Enum.GetUnderlyingType(type);

			var reader = containerType.GetMethod("ReadPrimitive", BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null,
				new Type[] { typeof(Stream), type.MakeByRefType() }, null);

			if (reader != null)
				return reader;

			if (type.IsGenericType)
			{
				var genType = type.GetGenericTypeDefinition();

				var mis = containerType.GetMethods(BindingFlags.Static | BindingFlags.Public)
					.Where(mi => mi.IsGenericMethod && mi.Name == "ReadPrimitive");

				foreach (var mi in mis)
				{
					var p = mi.GetParameters();

					if (p.Length != 2)
						continue;

					if (p[0].ParameterType != typeof(Stream))
						continue;

					var paramType = p[1].ParameterType;

					if (paramType.IsByRef == false)
						continue;

					paramType = paramType.GetElementType();

					if (paramType.IsGenericType == false)
						continue;

					var genParamType = paramType.GetGenericTypeDefinition();

					if (genType == genParamType)
						return mi;
				}
			}

			return null;
		}

		public static IEnumerable<FieldInfo> GetFieldInfos(Type type)
		{
			Debug.Assert(type.IsSerializable);

			var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
				.Where(fi => (fi.Attributes & FieldAttributes.NotSerialized) == 0)
				.OrderBy(f => f.Name, StringComparer.Ordinal);

			if (type.BaseType == null)
			{
				return fields;
			}
			else
			{
				var baseFields = GetFieldInfos(type.BaseType);
				return baseFields.Concat(fields);
			}
		}
	}
}
