/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace NetSerializer
{
	static class DeserializerCodegen
	{
		public static DynamicMethod GenerateDynamicDeserializerStub(Type type)
		{
			var dm = new DynamicMethod("Deserialize", null,
				new Type[] { typeof(Stream), type.MakeByRefType() },
				typeof(Serializer), true);
			dm.DefineParameter(1, ParameterAttributes.None, "stream");
			dm.DefineParameter(2, ParameterAttributes.Out, "value");

			return dm;
		}

#if GENERATE_DEBUGGING_ASSEMBLY
		public static MethodBuilder GenerateStaticDeserializerStub(TypeBuilder tb, Type type)
		{
			var mb = tb.DefineMethod("Deserialize", MethodAttributes.Public | MethodAttributes.Static, null, new Type[] { typeof(Stream), type.MakeByRefType() });
			mb.DefineParameter(1, ParameterAttributes.None, "stream");
			mb.DefineParameter(2, ParameterAttributes.Out, "value");
			return mb;
		}
#endif

		public static void GenerateDeserializerSwitch(CodeGenContext ctx, ILGenerator il, IDictionary<Type, TypeData> map)
		{
			// arg0: stream, arg1: out object

			var idLocal = il.DeclareLocal(typeof(ushort));

			// read typeID
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldloca_S, idLocal);
			il.EmitCall(OpCodes.Call, ctx.GetReaderMethodInfo(typeof(ushort)), null);

			// +1 for 0 (null)
			var jumpTable = new Label[map.Count + 1];
			jumpTable[0] = il.DefineLabel();
			foreach (var kvp in map)
				jumpTable[kvp.Value.TypeID] = il.DefineLabel();

			il.Emit(OpCodes.Ldloc_S, idLocal);
			il.Emit(OpCodes.Switch, jumpTable);

			il.Emit(OpCodes.Newobj, Helpers.ExceptionCtorInfo);
			il.Emit(OpCodes.Throw);

			/* null case */
			il.MarkLabel(jumpTable[0]);

			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Stind_Ref);
			il.Emit(OpCodes.Ret);

			/* cases for types */
			foreach (var kvp in map)
			{
				var type = kvp.Key;
				var data = kvp.Value;

				il.MarkLabel(jumpTable[data.TypeID]);

				var local = il.DeclareLocal(type);

				// call deserializer for this typeID
				il.Emit(OpCodes.Ldarg_0);
				if (local.LocalIndex < 256)
					il.Emit(OpCodes.Ldloca_S, local);
				else
					il.Emit(OpCodes.Ldloca, local);

				il.EmitCall(OpCodes.Call, data.ReaderMethodInfo, null);

				// write result object to out object
				il.Emit(OpCodes.Ldarg_1);
				if (local.LocalIndex < 256)
					il.Emit(OpCodes.Ldloc_S, local);
				else
					il.Emit(OpCodes.Ldloc, local);
				if (type.IsValueType)
					il.Emit(OpCodes.Box, type);
				il.Emit(OpCodes.Stind_Ref);

				il.Emit(OpCodes.Ret);
			}
		}
	}
}
