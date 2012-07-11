using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;

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

		static void GenerateDeserializerBody(CodeGenContext ctx, Type type, ILGenerator il)
		{
			// arg0: stream, arg1: out value

			D(il, "deser {0}", type.Name);

			if (type.IsArray)
				GenDeserializerBodyForArray(ctx, type, il);
			else
				GenDeserializerBody(ctx, type, il);
		}

		static void GenDeserializerBody(CodeGenContext ctx, Type type, ILGenerator il)
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
				il.Emit(OpCodes.Ldarg_1);
				if (type.IsClass)
					il.Emit(OpCodes.Ldind_Ref);
				il.Emit(OpCodes.Ldflda, field);

				GenDeserializerCall(ctx, il, field.FieldType);
			}

			il.Emit(OpCodes.Ret);
		}

		static void GenDeserializerBodyForArray(CodeGenContext ctx, Type type, ILGenerator il)
		{
			var elemType = type.GetElementType();

			var lenLocal = il.DeclareLocal(typeof(uint));

			// read array len
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldloca_S, lenLocal);
			il.EmitCall(OpCodes.Call, ctx.GetReaderMethodInfo(typeof(uint)), null);

			var notNullLabel = il.DefineLabel();

			/* if len == 0, return null */
			il.Emit(OpCodes.Ldloc_S, lenLocal);
			il.Emit(OpCodes.Brtrue_S, notNullLabel);

			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Stind_Ref);
			il.Emit(OpCodes.Ret);

			il.MarkLabel(notNullLabel);

			var arrLocal = il.DeclareLocal(type);

			// create new array with len - 1
			il.Emit(OpCodes.Ldloc_S, lenLocal);
			il.Emit(OpCodes.Ldc_I4_1);
			il.Emit(OpCodes.Sub);
			il.Emit(OpCodes.Newarr, elemType);
			il.Emit(OpCodes.Stloc_S, arrLocal);

			// declare i
			var idxLocal = il.DeclareLocal(typeof(int));

			// i = 0
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Stloc_S, idxLocal);

			var loopBodyLabel = il.DefineLabel();
			var loopCheckLabel = il.DefineLabel();

			il.Emit(OpCodes.Br_S, loopCheckLabel);

			// loop body
			il.MarkLabel(loopBodyLabel);

			// read element to arr[i]
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldloc_S, arrLocal);
			il.Emit(OpCodes.Ldloc_S, idxLocal);
			il.Emit(OpCodes.Ldelema, elemType);

			GenDeserializerCall(ctx, il, elemType);

			// i = i + 1
			il.Emit(OpCodes.Ldloc_S, idxLocal);
			il.Emit(OpCodes.Ldc_I4_1);
			il.Emit(OpCodes.Add);
			il.Emit(OpCodes.Stloc_S, idxLocal);

			il.MarkLabel(loopCheckLabel);

			// loop condition
			il.Emit(OpCodes.Ldloc_S, idxLocal);
			il.Emit(OpCodes.Ldloc_S, arrLocal);
			il.Emit(OpCodes.Ldlen);
			il.Emit(OpCodes.Conv_I4);
			il.Emit(OpCodes.Clt);
			il.Emit(OpCodes.Brtrue_S, loopBodyLabel);


			// store new array to the out value
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldloc_S, arrLocal);
			il.Emit(OpCodes.Stind_Ref);

			il.Emit(OpCodes.Ret);
		}

		static void GenDeserializerCall(CodeGenContext ctx, ILGenerator il, Type type)
		{
			// We can call the Deserializer method directly for:
			// - Value types
			// - Array types
			// - Sealed types with static Deserializer method, as the method will handle null
			// Other reference types go through the DeserializesSwitch

			bool direct;

			if (type.IsValueType || type.IsArray)
				direct = true;
			else if (type.IsSealed && ctx.IsDynamic(type) == false)
				direct = true;
			else
				direct = false;

			var method = direct ? ctx.GetReaderMethodInfo(type) : ctx.DeserializerSwitchMethodInfo;

			il.EmitCall(OpCodes.Call, method, null);
		}

		static void GenerateDeserializerSwitch(CodeGenContext ctx, ILGenerator il, IDictionary<Type, TypeData> map)
		{
			// arg0: stream, arg1: out object

			D(il, "deser switch");

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
				var type = kvp.Key;
				var data = kvp.Value;

				il.MarkLabel(jumpTable[data.TypeID]);

				var local = il.DeclareLocal(type);

				// call deserializer for this typeID
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldloca_S, local);
				if (data.WriterMethodInfo.IsGenericMethodDefinition)
				{
					Debug.Assert(type.IsGenericType);

					var genArgs = type.GetGenericArguments();

					il.EmitCall(OpCodes.Call, data.ReaderMethodInfo.MakeGenericMethod(genArgs), null);
				}
				else
				{
					il.EmitCall(OpCodes.Call, data.ReaderMethodInfo, null);
				}

				// write result object to out object
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldloc_S, local);
				if (type.IsValueType)
					il.Emit(OpCodes.Box, type);
				il.Emit(OpCodes.Stind_Ref);

				D(il, "deser switch done");

				il.Emit(OpCodes.Ret);
			}
		}
	}
}
