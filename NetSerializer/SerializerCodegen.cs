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

namespace NetSerializer
{
	static partial class SerializerCodegen
	{
		public static DynamicMethod GenerateDynamicSerializerStub(Type type)
		{
			var dm = new DynamicMethod("Serialize", null,
				new Type[] { typeof(Stream), type, typeof(ObjectList) },
				typeof(Serializer), true);

			dm.DefineParameter(1, ParameterAttributes.None, "stream");
			dm.DefineParameter(2, ParameterAttributes.None, "value");
			dm.DefineParameter(3, ParameterAttributes.None, "objList");

			return dm;
		}

		public static DynamicMethod GenerateDynamicSerializeInvokerStub()
		{
			var dm = new DynamicMethod(string.Empty, null,
				new Type[] { typeof(Stream), typeof(object), typeof(ObjectList) },
				typeof(Serializer), true);

			dm.DefineParameter(1, ParameterAttributes.None, "stream");
			dm.DefineParameter(2, ParameterAttributes.None, "value");
			dm.DefineParameter(3, ParameterAttributes.None, "objList");

			return dm;
		}

#if GENERATE_DEBUGGING_ASSEMBLY
		public static MethodBuilder GenerateStaticSerializerStub(TypeBuilder tb, Type type)
		{
			var mb = tb.DefineMethod("Serialize", MethodAttributes.Public | MethodAttributes.Static, null, 
						new Type[] { typeof(Stream), type, typeof(ObjectList) });
			mb.DefineParameter(1, ParameterAttributes.None, "stream");
			mb.DefineParameter(2, ParameterAttributes.None, "value");
			mb.DefineParameter(3, ParameterAttributes.None, "objList");
			return mb;
		}

		public static MethodBuilder GenerateStaticSerializeInvokerStub(TypeBuilder tb, int typeID)
		{
			var mb = tb.DefineMethod("SerializeInv" + typeID,	MethodAttributes.Public | MethodAttributes.Static, null,
						new Type[] { typeof(Stream), typeof(object), typeof(ObjectList) });
			mb.DefineParameter(1, ParameterAttributes.None, "stream");
			mb.DefineParameter(2, ParameterAttributes.None, "value");
			mb.DefineParameter(3, ParameterAttributes.None, "objList");
			return mb;
		}
#endif

		public static void GenerateSerializerBody(CodeGenContext ctx, Type type, ILGenerator il)
		{
			// arg0: Stream, arg1: value

			//--			D(il, "ser {0}", type.Name);

			if (type.IsArray)
				GenSerializerBodyForArray(ctx, type, il);
			else
				GenSerializerBody(ctx, type, il);
		}

		static void GenSerializerBody(CodeGenContext ctx, Type type, ILGenerator il)
		{
			if (type.IsClass)
			{
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
				// Note: the user defined value type is not passed as reference. could cause perf problems with big structs

				il.Emit(OpCodes.Ldarg_0);
				if (type.IsValueType)
					il.Emit(OpCodes.Ldarga_S, 1);
				else
					il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldfld, field);
				il.Emit(OpCodes.Ldarg_2);

				GenSerializerCall(ctx, il, field.FieldType);
			}

			il.Emit(OpCodes.Ret);
		}

		static void GenSerializerBodyForArray(CodeGenContext ctx, Type type, ILGenerator il)
		{
			var elemType = type.GetElementType();

			var notNullLabel = il.DefineLabel();

			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Brtrue_S, notNullLabel);

			// if value == null, write 0
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Ldarg_2);
			il.EmitCall(OpCodes.Call, ctx.GetWriterMethodInfo(typeof(uint)), null);
			il.Emit(OpCodes.Ret);

			il.MarkLabel(notNullLabel);

			//==============
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

			//==============
			// write array len + 1
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldlen);
			il.Emit(OpCodes.Ldc_I4_1);
			il.Emit(OpCodes.Add);
			il.Emit(OpCodes.Ldarg_2);
			il.EmitCall(OpCodes.Call, ctx.GetWriterMethodInfo(typeof(uint)), null);

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

			// write element at index i
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldloc_S, idxLocal);
			il.Emit(OpCodes.Ldelem, elemType);
			il.Emit(OpCodes.Ldarg_2);

			GenSerializerCall(ctx, il, elemType);

			// i = i + 1
			il.Emit(OpCodes.Ldloc_S, idxLocal);
			il.Emit(OpCodes.Ldc_I4_1);
			il.Emit(OpCodes.Add);
			il.Emit(OpCodes.Stloc_S, idxLocal);

			il.MarkLabel(loopCheckLabel);

			// loop condition
			il.Emit(OpCodes.Ldloc_S, idxLocal);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldlen);
			il.Emit(OpCodes.Conv_I4);
			il.Emit(OpCodes.Clt);
			il.Emit(OpCodes.Brtrue_S, loopBodyLabel);

			il.Emit(OpCodes.Ret);
		}

		static void GenSerializerCall(CodeGenContext ctx, ILGenerator il, Type type)
		{
			// We can call the Serializer method directly for:
			// - Value types
			// - Array types
			// - Sealed types with static Serializer method, as the method will handle null
			// Other reference types go through the SerializesSwitch

			bool direct;

			if (type.IsValueType || type.IsArray)
				direct = true;
			else if (type.IsSealed && ctx.IsDynamic(type) == false)
				direct = true;
			else
				direct = false;

#if GENERATE_SWITCH
			var method = direct ? ctx.GetWriterMethodInfo(type) : ctx.SerializerSwitchMethodInfo;
#else
			var method = direct ? ctx.GetWriterMethodInfo(type) : typeof(NetSerializer.Serializer).GetMethod("_SerializerSwitch");
#endif

			il.EmitCall(OpCodes.Call, method, null);
		}


#if GENERATE_SWITCH
		public static void GenerateSerializerSwitch(CodeGenContext ctx, ILGenerator il, IDictionary<Type, TypeData> map)
		{
			// arg0: Stream, arg1: object, arg2: objList

			//================			
			Type objStackType = typeof(ObjectList);
			Type objRefType = typeof(NetSerializer.ObjectRef);
			MethodInfo getIndexOfMethod = objStackType.GetMethod("IndexOf", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(object) }, null);

			var endLabel = il.DefineLabel();

			//==if(objList==null)  goto endLabel; 
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Brfalse_S, endLabel);

			var id = il.DeclareLocal(typeof(int));

			//int id = list.IdentityIndexOf(value);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Ldarg_1);
			il.EmitCall(OpCodes.Call, getIndexOfMethod, null);
			il.Emit(OpCodes.Stloc_S, id);

			//if (id == -1) goto endLabel;
			il.Emit(OpCodes.Ldloc_S, id);
			il.Emit(OpCodes.Ldc_I4_M1);
			il.Emit(OpCodes.Beq, endLabel);

			//== value = new ObjectRef(id);
			ConstructorInfo obj_ref_const = objRefType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(int) }, null);
			il.Emit(OpCodes.Ldloc_S, id);
			il.Emit(OpCodes.Newobj, obj_ref_const);
			il.Emit(OpCodes.Box, typeof(ObjectRef));
			il.Emit(OpCodes.Starg, 1);

			il.MarkLabel(endLabel);
			//===============			

			var idLocal_typeID = il.DeclareLocal(typeof(uint));

			// get TypeID from object's Type
			var getTypeIDcaseIDMethod = typeof(Serializer).GetMethod("GetTypeIDcaseID", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(object) }, null);
			il.Emit(OpCodes.Ldarg_1);
			il.EmitCall(OpCodes.Call, getTypeIDcaseIDMethod, null);
			il.Emit(OpCodes.Stloc_S, idLocal_typeID);

			// write typeID
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldloc_S, idLocal_typeID);
			il.Emit(OpCodes.Ldc_I4, 0xFFFF);
			il.Emit(OpCodes.And);
			il.Emit(OpCodes.Conv_U2);
			il.Emit(OpCodes.Ldarg_2);
			il.EmitCall(OpCodes.Call, ctx.GetWriterMethodInfo(typeof(ushort)), null);

			// +1 for 0 (null)
			var jumpTable = new Label[map.Count + 1];
			jumpTable[0] = il.DefineLabel();
			foreach (var kvp in map)
				jumpTable[kvp.Value.CaseID] = il.DefineLabel();

			il.Emit(OpCodes.Ldloc_S, idLocal_typeID);
			il.Emit(OpCodes.Ldc_I4, 16);
			il.Emit(OpCodes.Shr_Un);
			il.Emit(OpCodes.Ldc_I4, 0xFFFF);
			il.Emit(OpCodes.And);
			il.Emit(OpCodes.Conv_U2);

			il.Emit(OpCodes.Switch, jumpTable);

			//--			D(il, "eihx");
			ConstructorInfo exceptionCtor = typeof(Exception).GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null);
			il.Emit(OpCodes.Newobj, exceptionCtor);
			il.Emit(OpCodes.Throw);


			/* null case */
			il.MarkLabel(jumpTable[0]);
			il.Emit(OpCodes.Ret);

			/* cases for types */
			foreach (var kvp in map)
			{
				var type = kvp.Key;
				var data = kvp.Value;

				il.MarkLabel(jumpTable[data.CaseID]);

				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(type.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, type);
				il.Emit(OpCodes.Ldarg_2);

				if (data.WriterMethodInfo.IsGenericMethodDefinition)
				{
					Debug.Assert(type.IsGenericType);
					var genArgs = type.GetGenericArguments();
					il.EmitCall(OpCodes.Call, data.WriterMethodInfo.MakeGenericMethod(genArgs), null);
				}
				else
				{
					il.EmitCall(OpCodes.Call, data.WriterMethodInfo, null);
				}

				il.Emit(OpCodes.Ret);
			}
		}
#endif
	}
}
