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
	static class SerializerCodegen
	{
		public static DynamicMethod GenerateDynamicSerializerStub(Type type)
		{
#if SILVERLIGHT
            var dm = new DynamicMethod("Serialize", null,
                new Type[] { typeof(Stream), type });
#else
			var dm = new DynamicMethod("Serialize", null,
				new Type[] { typeof(Stream), type },
				typeof(Serializer), true);
#endif

			dm.DefineParameter(1, ParameterAttributes.None, "stream");
			dm.DefineParameter(2, ParameterAttributes.None, "value");

			return dm;
		}

#if GENERATE_DEBUGGING_ASSEMBLY
		public static MethodBuilder GenerateStaticSerializerStub(TypeBuilder tb, Type type)
		{
			var mb = tb.DefineMethod("Serialize", MethodAttributes.Public | MethodAttributes.Static, null, new Type[] { typeof(Stream), type });
			mb.DefineParameter(1, ParameterAttributes.None, "stream");
			mb.DefineParameter(2, ParameterAttributes.None, "value");
			return mb;
		}
#endif

		public static void GenerateSerializerBody(CodeGenContext ctx, Type type, ILGenerator il)
		{
			// arg0: Stream, arg1: value

			if (type.IsArray)
				GenSerializerBodyForArray(ctx, type, il);
			else
				GenSerializerBody(ctx, type, il);
		}

		static void GenSerializerBody(CodeGenContext ctx, Type type, ILGenerator il)
		{
#if SERIALIZE_PROPERTIES
            var properties = Helpers.GetPropertyInfos(type);

		    var wrtBt = typeof (Stream).GetMethod("WriteByte", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] {typeof (Byte)}, null);
            foreach (var property in properties)
            {
                var mth = property.GetGetMethod();

                // Note: the user defined value type is not passed as reference. could cause perf problems with big structs

                //Ldarg_0 = Stream, ldarg_1 0 object to serialize

                var underlying = Nullable.GetUnderlyingType(property.PropertyType);
                if (underlying != null)
                {
                    var hasValueMth = property.PropertyType.GetProperty("HasValue").GetGetMethod();
                    var getValueMth = property.PropertyType.GetProperty("Value").GetGetMethod();

                    var loc = il.DeclareLocal(property.PropertyType);

                    //il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Callvirt, mth);                 //Call Property Getter
                    il.Emit(OpCodes.Stloc, loc);
                    il.Emit(OpCodes.Ldloca, loc);
                    il.Emit(OpCodes.Call, hasValueMth);             //Call HasValue

                    var hasValueLbl = il.DefineLabel();
                    var endLbl = il.DefineLabel();

                    il.Emit(OpCodes.Brtrue_S, hasValueLbl);         //Nullable -> No Value Write 0 to Stream and jump to end
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Call, wrtBt);
                    il.Emit(OpCodes.Br_S, endLbl);

                    il.MarkLabel(hasValueLbl);                      //Nullable -> No Value Write 1 to Stream, Load Value and GenSerializerCall
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Call, wrtBt);

                    il.Emit(OpCodes.Ldarg_0);                       //Stream als Arg0 für GenSerializerCall
                    il.Emit(OpCodes.Ldloca, loc);            
                    il.Emit(OpCodes.Call, getValueMth);             //Call GetValue
                    GenSerializerCall(ctx, il, Nullable.GetUnderlyingType(property.PropertyType));
                    il.MarkLabel(endLbl);                        
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_0);
                    if (type.IsValueType)
                        il.Emit(OpCodes.Ldarga_S, 1);
                    else
                        il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Call, mth);             //Call Property Getter

                    GenSerializerCall(ctx, il, property.PropertyType);
                }
            }
#else
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

				GenSerializerCall(ctx, il, field.FieldType);
			}
#endif

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
			il.EmitCall(OpCodes.Call, ctx.GetWriterMethodInfo(typeof(uint)), null);
			il.Emit(OpCodes.Ret);

			il.MarkLabel(notNullLabel);

			// write array len + 1
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldlen);
			il.Emit(OpCodes.Ldc_I4_1);
			il.Emit(OpCodes.Add);
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

			var method = direct ? ctx.GetWriterMethodInfo(type) : ctx.SerializerSwitchMethodInfo;

			il.EmitCall(OpCodes.Call, method, null);
		}


		public static void GenerateSerializerSwitch(CodeGenContext ctx, ILGenerator il, IDictionary<Type, TypeData> map)
		{
			// arg0: Stream, arg1: object

			var idLocal = il.DeclareLocal(typeof(ushort));

			// get TypeID from object's Type
#if SILVERLIGHT
			var getTypeIDMethod = typeof(Serializer).GetMethod("GetTypeID", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(object) }, null);
#else
            var getTypeIDMethod = typeof(Serializer).GetMethod("GetTypeID", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(object) }, null);
#endif
			il.Emit(OpCodes.Ldarg_1);
			il.EmitCall(OpCodes.Call, getTypeIDMethod, null);
			il.Emit(OpCodes.Stloc_S, idLocal);

			// write typeID
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldloc_S, idLocal);
			il.EmitCall(OpCodes.Call, ctx.GetWriterMethodInfo(typeof(ushort)), null);

			// +1 for 0 (null)
			var jumpTable = new Label[map.Count + 1];
			jumpTable[0] = il.DefineLabel();
			foreach (var kvp in map)
				jumpTable[kvp.Value.TypeID] = il.DefineLabel();

			il.Emit(OpCodes.Ldloc_S, idLocal);
			il.Emit(OpCodes.Switch, jumpTable);

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

				il.MarkLabel(jumpTable[data.TypeID]);

				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(type.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, type);

				il.EmitCall(OpCodes.Call, data.WriterMethodInfo, null);

				il.Emit(OpCodes.Ret);
			}
		}
	}
}
