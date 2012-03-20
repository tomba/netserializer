using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace NetSerializer
{
	static partial class Serializer
	{
		static DynamicMethod GenerateDynamicDeserializerStub(Type type)
		{
			var dm = new DynamicMethod("Deserialize", null,
				new Type[] { typeof(Stream), type.MakeByRefType() },
				typeof(Serializer), true);
			dm.DefineParameter(1, ParameterAttributes.None, "stream");
			dm.DefineParameter(2, ParameterAttributes.Out, "value");

			return dm;
		}

		static MethodBuilder GenerateStaticDeserializerStub(TypeBuilder tb, Type type)
		{
			var mb = tb.DefineMethod("Deserialize", MethodAttributes.Public | MethodAttributes.Static, null, new Type[] { typeof(Stream), type.MakeByRefType() });
			mb.DefineParameter(1, ParameterAttributes.None, "stream");
			mb.DefineParameter(2, ParameterAttributes.Out, "value");
			return mb;
		}

		static void GenerateDeserializerBody(Type type, ILGenerator il)
		{
			// arg0: stream, arg1: out value

			D(il, "deser {0}", type.Name);

			if (type.IsArray)
			{
				var elemType = type.GetElementType();

				var lenLocal = il.DeclareLocal(typeof(uint));

				// read array len
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldloca, lenLocal);
				il.EmitCall(OpCodes.Call, GetReaderMethodInfo(typeof(uint)), null);

				var arrLocal = il.DeclareLocal(type);

				// create new array
				il.Emit(OpCodes.Ldloc, lenLocal);
				il.Emit(OpCodes.Newarr, elemType);
				il.Emit(OpCodes.Stloc, arrLocal);

				// declare i
				var idxLocal = il.DeclareLocal(typeof(int));

				// i = 0
				il.Emit(OpCodes.Ldnull);
				il.Emit(OpCodes.Stloc, idxLocal);

				var loopBodyLabel = il.DefineLabel();
				var loopCheckLabel = il.DefineLabel();

				il.Emit(OpCodes.Br, loopCheckLabel);

				// loop body
				il.MarkLabel(loopBodyLabel);

				// read element to arr[i]
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldloc, arrLocal);
				il.Emit(OpCodes.Ldloc, idxLocal);
				il.Emit(OpCodes.Ldelema, elemType);
				if (elemType.IsValueType)
					il.EmitCall(OpCodes.Call, GetReaderMethodInfo(elemType), null);
				else
					il.EmitCall(OpCodes.Call, s_deserializerSwitchMethodInfo, null);

				// i = i + 1
				il.Emit(OpCodes.Ldloc, idxLocal);
				il.Emit(OpCodes.Ldc_I4_1);
				il.Emit(OpCodes.Add);
				il.Emit(OpCodes.Stloc, idxLocal);

				il.MarkLabel(loopCheckLabel);

				// loop condition
				il.Emit(OpCodes.Ldloc, idxLocal);
				il.Emit(OpCodes.Ldloc, arrLocal);
				il.Emit(OpCodes.Ldlen);
				il.Emit(OpCodes.Conv_I4);
				il.Emit(OpCodes.Clt);
				il.Emit(OpCodes.Brtrue, loopBodyLabel);


				// store new array to the out value
				il.Emit(OpCodes.Ldarg, 1);
				il.Emit(OpCodes.Ldloc, arrLocal);
				il.Emit(OpCodes.Stind_Ref);
			}
			else
			{
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

				var fields = GetFieldInfos(type);

				foreach (var field in fields)
				{
					il.Emit(OpCodes.Ldarg_0);
					il.Emit(OpCodes.Ldarg, 1);
					if (type.IsClass)
						il.Emit(OpCodes.Ldind_Ref);
					il.Emit(OpCodes.Ldflda, field);

					// all classes go to switch method. unambiguous classes line sealed or arrays could skip that.
					// note: null value is not handled if deserializerSwitch is not called
					if (field.FieldType.IsValueType)
						il.EmitCall(OpCodes.Call, GetReaderMethodInfo(field.FieldType), null);
					else
						il.EmitCall(OpCodes.Call, s_deserializerSwitchMethodInfo, null);
				}
			}

			D(il, "deser done");
			il.Emit(OpCodes.Ret);
		}



		static void GenerateDeserializerSwitch(ILGenerator il, IDictionary<Type, TypeData> map)
		{
			// arg0: stream, arg1: out object

			D(il, "deser switch");

			var idLocal = il.DeclareLocal(typeof(ushort));

			// read typeID
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldloca, idLocal);
			il.EmitCall(OpCodes.Call, GetReaderMethodInfo(typeof(ushort)), null);

			// +1 for 0 (null)
			var jumpTable = new Label[map.Count + 1];
			jumpTable[0] = il.DefineLabel();
			foreach (var kvp in map)
				jumpTable[kvp.Value.TypeID] = il.DefineLabel();

			il.Emit(OpCodes.Ldloc, idLocal);
			il.Emit(OpCodes.Switch, jumpTable);

			D(il, "eihx");
			il.ThrowException(typeof(Exception));

			/* null case */
			il.MarkLabel(jumpTable[0]);

			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Stind_Ref);
			il.Emit(OpCodes.Ret);

			/* cases for types */
			foreach (var kvp in map)
			{
				var data = kvp.Value;

				il.MarkLabel(jumpTable[data.TypeID]);

				var local = il.DeclareLocal(kvp.Key);

				// call deserializer for this typeID
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldloca, local);
				il.EmitCall(OpCodes.Call, data.ReaderMethodInfo, null);

				// write result object to out object
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldloc, local);
				if (kvp.Key.IsValueType)
					il.Emit(OpCodes.Box, kvp.Key);
				il.Emit(OpCodes.Stind_Ref);

				D(il, "deser switch done");

				il.Emit(OpCodes.Ret);
			}
		}
	}
}
