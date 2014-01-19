using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace NetSerializer
{
	static class Helpers
	{
		public static readonly ConstructorInfo ExceptionCtorInfo = typeof(Exception).GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null);
		public static readonly MethodInfo GetTypeIDMethodInfo = typeof(Serializer).GetMethod("GetTypeID", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(object) }, null);

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

		public static void GenSerializerCall(CodeGenContext ctx, ILGenerator il, Type type)
		{
			// We can call the Serializer method directly for:
			// - Value types
			// - Array types
			// - Sealed types with static Serializer method, as the method will handle null
			// Other reference types go through the SerializesSwitch

			bool direct;

			if (type.IsValueType || type.IsArray)
				direct = true;
			else if (type.IsSealed && ctx.IsGenerated(type) == false)
				direct = true;
			else
				direct = false;

			var method = direct ? ctx.GetWriterMethodInfo(type) : ctx.SerializerSwitchMethodInfo;

			il.EmitCall(OpCodes.Call, method, null);
		}

		public static void GenDeserializerCall(CodeGenContext ctx, ILGenerator il, Type type)
		{
			// We can call the Deserializer method directly for:
			// - Value types
			// - Array types
			// - Sealed types with static Deserializer method, as the method will handle null
			// Other reference types go through the DeserializesSwitch

			bool direct;

			if (type.IsValueType || type.IsArray)
				direct = true;
			else if (type.IsSealed && ctx.IsGenerated(type) == false)
				direct = true;
			else
				direct = false;

			var method = direct ? ctx.GetReaderMethodInfo(type) : ctx.DeserializerSwitchMethodInfo;

			il.EmitCall(OpCodes.Call, method, null);
		}

	}
}
