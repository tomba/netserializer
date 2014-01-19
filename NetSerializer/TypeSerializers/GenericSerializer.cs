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
	public class GenericSerializer : IDynamicTypeSerializer
	{
		public bool Handles(Type type)
		{
			if (!type.IsSerializable)
				throw new NotSupportedException(String.Format("Type {0} is not marked as Serializable", type.FullName));

			if (typeof(System.Runtime.Serialization.ISerializable).IsAssignableFrom(type))
				throw new NotSupportedException(String.Format("Cannot serialize {0}: ISerializable not supported", type.FullName));

			return true;
		}

		public IEnumerable<Type> GetSubtypes(Type type)
		{
			var fields = Helpers.GetFieldInfos(type);

			foreach (var field in fields)
				yield return field.FieldType;
		}

		public void GenerateWriterMethod(Type type, CodeGenContext ctx, ILGenerator il)
		{
			// arg0: Stream, arg1: value

			var fields = Helpers.GetFieldInfos(type);

			foreach (var field in fields)
			{
				// Note: the user defined value type is not passed as reference. could cause perf problems with big structs

				il.Emit(OpCodes.Ldarg_0);
				if (type.IsValueType)
					il.Emit(OpCodes.Ldarga_S, 1);
				else
					il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldfld, field);

				Helpers.GenSerializerCall(ctx, il, field.FieldType);
			}

			il.Emit(OpCodes.Ret);
		}

		public void GenerateReaderMethod(Type type, CodeGenContext ctx, ILGenerator il)
		{
			// arg0: stream, arg1: out value

			if (type.IsClass)
			{
				// instantiate empty class
				il.Emit(OpCodes.Ldarg_1);

				var gtfh = typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Public | BindingFlags.Static);
				var guo = typeof(System.Runtime.Serialization.FormatterServices).GetMethod("GetUninitializedObject", BindingFlags.Public | BindingFlags.Static);
				il.Emit(OpCodes.Ldtoken, type);
				il.Emit(OpCodes.Call, gtfh);
				il.Emit(OpCodes.Call, guo);
				il.Emit(OpCodes.Castclass, type);

				il.Emit(OpCodes.Stind_Ref);
			}

			var fields = Helpers.GetFieldInfos(type);

			foreach (var field in fields)
			{
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldarg_1);
				if (type.IsClass)
					il.Emit(OpCodes.Ldind_Ref);
				il.Emit(OpCodes.Ldflda, field);

				Helpers.GenDeserializerCall(ctx, il, field.FieldType);
			}

			if (typeof(System.Runtime.Serialization.IDeserializationCallback).IsAssignableFrom(type))
			{
				var miOnDeserialization = typeof(System.Runtime.Serialization.IDeserializationCallback).GetMethod("OnDeserialization",
										BindingFlags.Instance | BindingFlags.Public,
										null, new[] { typeof(Object) }, null);

				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldnull);
				il.Emit(OpCodes.Constrained, type);
				il.Emit(OpCodes.Callvirt, miOnDeserialization);
			}

			il.Emit(OpCodes.Ret);
		}
	}
}
