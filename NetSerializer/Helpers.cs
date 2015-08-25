/*
 * Copyright 2015 Tomi Valkeinen
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

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

		public static DynamicMethod GenerateDynamicSerializerStub(Type type)
		{
			var dm = new DynamicMethod("Serialize", null,
				new Type[] { typeof(Serializer), typeof(Stream), type },
				typeof(Serializer), true);

			dm.DefineParameter(1, ParameterAttributes.None, "serializer");
			dm.DefineParameter(2, ParameterAttributes.None, "stream");
			dm.DefineParameter(3, ParameterAttributes.None, "value");

			return dm;
		}

		public static DynamicMethod GenerateDynamicDeserializerStub(Type type)
		{
			var dm = new DynamicMethod("Deserialize", null,
				new Type[] { typeof(Serializer), typeof(Stream), type.MakeByRefType() },
				typeof(Serializer), true);
			dm.DefineParameter(1, ParameterAttributes.None, "serializer");
			dm.DefineParameter(2, ParameterAttributes.None, "stream");
			dm.DefineParameter(3, ParameterAttributes.Out, "value");

			return dm;
		}

#if GENERATE_DEBUGGING_ASSEMBLY
		public static MethodBuilder GenerateStaticSerializerStub(TypeBuilder tb, Type type)
		{
			var mb = tb.DefineMethod("Serialize", MethodAttributes.Public | MethodAttributes.Static, null,
				new Type[] { typeof(Serializer), typeof(Stream), type });
			mb.DefineParameter(1, ParameterAttributes.None, "serializer");
			mb.DefineParameter(2, ParameterAttributes.None, "stream");
			mb.DefineParameter(3, ParameterAttributes.None, "value");
			return mb;
		}

		public static MethodBuilder GenerateStaticDeserializerStub(TypeBuilder tb, Type type)
		{
			var mb = tb.DefineMethod("Deserialize", MethodAttributes.Public | MethodAttributes.Static, null,
				new Type[] { typeof(Serializer), typeof(Stream), type.MakeByRefType() });
			mb.DefineParameter(1, ParameterAttributes.None, "serializer");
			mb.DefineParameter(2, ParameterAttributes.None, "stream");
			mb.DefineParameter(3, ParameterAttributes.Out, "value");
			return mb;
		}
#endif
		public static void GenerateSerializerTrampoline(ILGenerator il, Type type, TypeData data)
		{
			if (data.NeedsInstanceParameter)
				il.Emit(OpCodes.Ldarg_0);

			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(type.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, type);

			il.Emit(OpCodes.Tailcall);
			il.Emit(OpCodes.Call, data.WriterMethodInfo);

			il.Emit(OpCodes.Ret);
		}

		public static void GenerateDeserializerTrampoline(ILGenerator il, Type type, TypeData data)
		{
			var local = il.DeclareLocal(type);

			// call deserializer for this typeID
			if (data.NeedsInstanceParameter)
				il.Emit(OpCodes.Ldarg_0);

			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldloca_S, local);

			il.Emit(OpCodes.Call, data.ReaderMethodInfo);

			// write result object to out object
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Ldloc_S, local);
			if (type.IsValueType)
				il.Emit(OpCodes.Box, type);
			il.Emit(OpCodes.Stind_Ref);

			il.Emit(OpCodes.Ret);
		}
	}
}
