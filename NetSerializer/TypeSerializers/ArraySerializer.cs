using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace NetSerializer
{
	class ArraySerializer : IDynamicTypeSerializer
	{
		public bool Handles(Type type)
		{
			if (!type.IsArray)
				return false;

			if (type.GetArrayRank() != 1)
				throw new NotSupportedException(String.Format("Multi-dim arrays not supported: {0}", type.FullName));

			return true;
		}

		public IEnumerable<Type> GetSubtypes(Type type)
		{
			yield return type.GetElementType();
		}

		public void GenerateWriterMethod(Type type, CodeGenContext ctx, ILGenerator il)
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

			Helpers.GenSerializerCall(ctx, il, elemType);

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

		public void GenerateReaderMethod(Type type, CodeGenContext ctx, ILGenerator il)
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

			Helpers.GenDeserializerCall(ctx, il, elemType);

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
	}
}
