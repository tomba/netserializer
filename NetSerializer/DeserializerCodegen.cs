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
				new Type[] { typeof(Stream), type.MakeByRefType(), typeof(ObjectList) },
				typeof(Serializer), true);
			dm.DefineParameter(1, ParameterAttributes.None, "stream");
			dm.DefineParameter(2, ParameterAttributes.Out, "value");
			dm.DefineParameter(3, ParameterAttributes.None, "objList");

			return dm;
		}

		public static DynamicMethod GenerateDynamicDeserializeInvokerStub()
		{
			var dm = new DynamicMethod(string.Empty, null,
				new Type[] { typeof(Stream), typeof(object).MakeByRefType(), typeof(ObjectList) },
				typeof(Serializer), true);
			dm.DefineParameter(1, ParameterAttributes.None, "stream");
			dm.DefineParameter(2, ParameterAttributes.Out, "value");
			dm.DefineParameter(3, ParameterAttributes.None, "objList");

			return dm;
		}

#if GENERATE_DEBUGGING_ASSEMBLY
		public static MethodBuilder GenerateStaticDeserializerStub(TypeBuilder tb, Type type)
		{
			var mb = tb.DefineMethod("Deserialize", MethodAttributes.Public | MethodAttributes.Static, null, 
						new Type[] { typeof(Stream), type.MakeByRefType(), typeof(ObjectList) });
			mb.DefineParameter(1, ParameterAttributes.None, "stream");
			mb.DefineParameter(2, ParameterAttributes.Out, "value");
			mb.DefineParameter(3, ParameterAttributes.None, "objList");
			return mb;
		}

		public static MethodBuilder GenerateStaticDeserializeInvokerStub(TypeBuilder tb, int typeID)
		{
			var mb = tb.DefineMethod("DeserializeInv" + typeID, MethodAttributes.Public | MethodAttributes.Static, null,
						new Type[] { typeof(Stream), typeof(object).MakeByRefType(), typeof(ObjectList) });
			mb.DefineParameter(1, ParameterAttributes.None, "stream");
			mb.DefineParameter(2, ParameterAttributes.Out, "value");
			mb.DefineParameter(3, ParameterAttributes.None, "objList");
			return mb;
		}
#endif

		public static void GenerateDeserializerBody(CodeGenContext ctx, Type type, ILGenerator il)
		{
			// arg0: stream, arg1: out value

			//--			D(il, "deser {0}", type.Name);

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

				Type objStackType = typeof(ObjectList);
				MethodInfo getAddMethod = objStackType.GetMethod("Add", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(object) }, null);

				var endLabel = il.DefineLabel();

				//==if(objList==null)  goto endLabel; 
				il.Emit(OpCodes.Ldarg_2);
				il.Emit(OpCodes.Brfalse_S, endLabel);

				//== objList.Add(value);
				il.Emit(OpCodes.Ldarg_2);
				il.Emit(OpCodes.Ldarg_1);
				il.EmitCall(OpCodes.Call, getAddMethod, null);

				il.MarkLabel(endLabel);
			}

			var fields = Helpers.GetFieldInfos(type);

			foreach (var field in fields)
			{
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldarg_1);
				if (type.IsClass)
					il.Emit(OpCodes.Ldind_Ref);
				il.Emit(OpCodes.Ldflda, field);
				il.Emit(OpCodes.Ldarg_2);

				GenDeserializerCall(ctx, il, field.FieldType);
			}

			if (typeof(IDeserializationCallback).IsAssignableFrom(type))
			{
				var miOnDeserialization = typeof(IDeserializationCallback).GetMethod("OnDeserialization",
										BindingFlags.Instance | BindingFlags.Public,
										null, new[] { typeof(Object) }, null);

				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldnull);
				il.Emit(OpCodes.Constrained, type);
				il.Emit(OpCodes.Callvirt, miOnDeserialization);
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
			il.Emit(OpCodes.Ldarg_2);
			il.EmitCall(OpCodes.Call, ctx.GetReaderMethodInfo(typeof(uint)), null);

			var notNullLabel = il.DefineLabel();
			var dataLabel = il.DefineLabel();
			var refLabel = il.DefineLabel();

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
			il.Emit(OpCodes.Ldarg_2);

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


			Type objStackType = typeof(ObjectList);
			MethodInfo getAddMethod = objStackType.GetMethod("Add", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(object) }, null);
			var endLabel = il.DefineLabel();

			//==if(objList==null)  goto endLabel; 
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Brfalse_S, endLabel);

			//== objList.Add(value);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Ldarg_1);
			il.EmitCall(OpCodes.Call, getAddMethod, null);

			il.MarkLabel(endLabel);

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

#if GENERATE_SWITCH
			var method = direct ? ctx.GetReaderMethodInfo(type) : ctx.DeserializerSwitchMethodInfo;
#else
			var method = direct ? ctx.GetReaderMethodInfo(type) : typeof(NetSerializer.Serializer).GetMethod("_DeserializerSwitch");
#endif

			il.EmitCall(OpCodes.Call, method, null);
		}

#if GENERATE_SWITCH
		public static void GenerateDeserializerSwitch(CodeGenContext ctx, ILGenerator il, IDictionary<Type, TypeData> map)
		{
			// arg0: stream, arg1: out object, arg2: objList

			//--			D(il, "deser switch");

			var idLocal_typeID = il.DeclareLocal(typeof(ushort));
			var idLocal_caseID = il.DeclareLocal(typeof(ushort));

			// read typeID
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldloca_S, idLocal_typeID);
			il.Emit(OpCodes.Ldarg_2);
			il.EmitCall(OpCodes.Call, ctx.GetReaderMethodInfo(typeof(ushort)), null);


			// get CaseID from  TypeID
			var getCaseIDMethod = typeof(Serializer).GetMethod("GetCaseID", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(ushort) }, null);
			il.Emit(OpCodes.Ldloc_S, idLocal_typeID);
			il.EmitCall(OpCodes.Call, getCaseIDMethod, null);
			il.Emit(OpCodes.Stloc_S, idLocal_caseID);

			// +1 for 0 (null)
			var jumpTable = new Label[map.Count + 1];
			jumpTable[0] = il.DefineLabel();
			foreach (var kvp in map)
				jumpTable[kvp.Value.CaseID] = il.DefineLabel();

			il.Emit(OpCodes.Ldloc_S, idLocal_caseID);
			il.Emit(OpCodes.Switch, jumpTable);

			//--			D(il, "eihx");
			ConstructorInfo exceptionCtor = typeof(Exception).GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null);
			il.Emit(OpCodes.Newobj, exceptionCtor);
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

				il.MarkLabel(jumpTable[data.CaseID]);

				var local = il.DeclareLocal(type);

				// call deserializer for this typeID
				il.Emit(OpCodes.Ldarg_0);
				if (local.LocalIndex < 256)
					il.Emit(OpCodes.Ldloca_S, local);
				else
					il.Emit(OpCodes.Ldloca, local);
				il.Emit(OpCodes.Ldarg_2);

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

				if (type == typeof(ObjectRef))
				{
					// get Object by Ref and write found object to out object
					var getGetAt = typeof(ObjectList).GetMethod("GetAt", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(ObjectRef) }, null);

					var nullLabel = il.DefineLabel();

					//==if(objList==null)  goto nullLabel; 
					il.Emit(OpCodes.Ldarg_2);
					il.Emit(OpCodes.Brfalse_S, nullLabel);

					//== value = objList.GetAt(obj.obj_ref);
					il.Emit(OpCodes.Ldarg_1);
					il.Emit(OpCodes.Ldarg_2);
					il.Emit(OpCodes.Ldloc_S, local);
					il.EmitCall(OpCodes.Call, getGetAt, null);
					il.Emit(OpCodes.Stind_Ref);
					il.Emit(OpCodes.Ret);

					il.MarkLabel(nullLabel);

					//== value = null;
					il.Emit(OpCodes.Ldarg_1);
					il.Emit(OpCodes.Ldnull);
					il.Emit(OpCodes.Stind_Ref);

				}
				else
				{
					// write result object to out object
					il.Emit(OpCodes.Ldarg_1);
					if (local.LocalIndex < 256)
						il.Emit(OpCodes.Ldloc_S, local);
					else
						il.Emit(OpCodes.Ldloc, local);

					if (type.IsValueType)
						il.Emit(OpCodes.Box, type);
					il.Emit(OpCodes.Stind_Ref);
				}

				//--				D(il, "deser switch done");

				il.Emit(OpCodes.Ret);
			}
		}
#endif
	}
}
