﻿/*
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
	sealed class GenericSerializer : IDynamicTypeSerializer
	{
		public bool Handles(Serializer serializer, Type type)
		{
            // .NET Core does not include the SerializableAttribute
#if !DNXCORE50
		    if (serializer.Settings.SupportISerializableAttribute)
		    {
		        if (!type.IsSerializable)
		            throw new NotSupportedException(String.Format("Type {0} is not marked as Serializable", type.FullName));

		        if (typeof(System.Runtime.Serialization.ISerializable).IsAssignableFrom(type))
		            throw new NotSupportedException(String.Format("Cannot serialize {0}: ISerializable not supported",
		                type.FullName));
		    }
#endif
            return true;
		}

		public IEnumerable<Type> GetSubtypes(Type type)
		{
			var fields = Helpers.GetFieldInfos(type);

			foreach (var field in fields)
				yield return field.FieldType;
		}

		static IEnumerable<MethodInfo> GetMethodsWithAttributes(Type type, Type attrType)
		{
			var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

			var methods = type.GetMethods(flags)
				.Where(m => m.GetCustomAttributes(attrType, false).Any());

			if (type.GetTypeInfo().BaseType == null)
			{
				return methods;
			}
			else
			{
				var baseMethods = GetMethodsWithAttributes(type.GetTypeInfo().BaseType, attrType);
				return baseMethods.Concat(methods);
			}
		}

#if !DNXCORE50
        static void EmitCallToSerializingCallback(Type type, ILGenerator il, MethodInfo method)
		{
			if (type.GetTypeInfo().IsValueType)
				throw new NotImplementedException("Serialization callbacks not supported for Value types");

			if (type.GetTypeInfo().IsValueType)
				il.Emit(OpCodes.Ldarga_S, 2);
			else
				il.Emit(OpCodes.Ldarg_2);

			var ctxLocal = il.DeclareLocal(typeof(System.Runtime.Serialization.StreamingContext));
			il.Emit(OpCodes.Ldloca_S, ctxLocal);
			il.Emit(OpCodes.Initobj, typeof(System.Runtime.Serialization.StreamingContext));
			il.Emit(OpCodes.Ldloc_S, ctxLocal);

			il.Emit(OpCodes.Call, method);
		}

		static void EmitCallToDeserializingCallback(Type type, ILGenerator il, MethodInfo method)
		{
			if (type.GetTypeInfo().IsValueType)
				throw new NotImplementedException("Serialization callbacks not supported for Value types");

			il.Emit(OpCodes.Ldarg_2);
			if (type.GetTypeInfo().IsClass)
				il.Emit(OpCodes.Ldind_Ref);

			var ctxLocal = il.DeclareLocal(typeof(System.Runtime.Serialization.StreamingContext));
			il.Emit(OpCodes.Ldloca_S, ctxLocal);
			il.Emit(OpCodes.Initobj, typeof(System.Runtime.Serialization.StreamingContext));
			il.Emit(OpCodes.Ldloc_S, ctxLocal);

			il.Emit(OpCodes.Call, method);
		}
#endif

        public void GenerateWriterMethod(Serializer serializer, Type type, ILGenerator il)
		{
            // arg0: Serializer, arg1: Stream, arg2: value

            // .NET Core does not provide the serialization callbacks
#if !DNXCORE50
            if (serializer.Settings.SupportSerializationCallbacks)
			{
				foreach (var m in GetMethodsWithAttributes(type, typeof(System.Runtime.Serialization.OnSerializingAttribute)))
					EmitCallToSerializingCallback(type, il, m);
			}
#endif

            var fields = Helpers.GetFieldInfos(type);

			foreach (var field in fields)
			{
				// Note: the user defined value type is not passed as reference. could cause perf problems with big structs

				var fieldType = field.FieldType;

				var data = serializer.GetIndirectData(fieldType);

				if (data.WriterNeedsInstance)
					il.Emit(OpCodes.Ldarg_0);

				il.Emit(OpCodes.Ldarg_1);
				if (type.GetTypeInfo().IsValueType)
					il.Emit(OpCodes.Ldarga_S, 2);
				else
					il.Emit(OpCodes.Ldarg_2);
				il.Emit(OpCodes.Ldfld, field);

				il.Emit(OpCodes.Call, data.WriterMethodInfo);
			}

#if !DNXCORE50
            if (serializer.Settings.SupportSerializationCallbacks)
			{
				foreach (var m in GetMethodsWithAttributes(type, typeof(System.Runtime.Serialization.OnSerializedAttribute)))
					EmitCallToSerializingCallback(type, il, m);
			}
#endif

			il.Emit(OpCodes.Ret);
		}

		public void GenerateReaderMethod(Serializer serializer, Type type, ILGenerator il)
		{
			// arg0: Serializer, arg1: stream, arg2: out value

			if (type.GetTypeInfo().IsClass)
			{
				// instantiate empty class
				il.Emit(OpCodes.Ldarg_2);

				var gtfh = typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Public | BindingFlags.Static);
#if !DNXCORE50
                var guo = typeof(System.Runtime.Serialization.FormatterServices).GetMethod("GetUninitializedObject", BindingFlags.Public | BindingFlags.Static);
#else
                var guo = typeof(System.Runtime.CompilerServices.RuntimeHelpers).GetMethod("GetUninitializedObject", BindingFlags.Public | BindingFlags.Static);
#endif
                il.Emit(OpCodes.Ldtoken, type);
				il.Emit(OpCodes.Call, gtfh);
				il.Emit(OpCodes.Call, guo);
				il.Emit(OpCodes.Castclass, type);

				il.Emit(OpCodes.Stind_Ref);
			}

#if !DNXCORE50
            if (serializer.Settings.SupportSerializationCallbacks)
			{
				foreach (var m in GetMethodsWithAttributes(type, typeof(System.Runtime.Serialization.OnDeserializingAttribute)))
					EmitCallToDeserializingCallback(type, il, m);
			}
#endif

			var fields = Helpers.GetFieldInfos(type);

			foreach (var field in fields)
			{
				var fieldType = field.FieldType;

				var data = serializer.GetIndirectData(fieldType);

				if (data.ReaderNeedsInstance)
					il.Emit(OpCodes.Ldarg_0);

				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldarg_2);
				if (type.GetTypeInfo().IsClass)
					il.Emit(OpCodes.Ldind_Ref);
				il.Emit(OpCodes.Ldflda, field);

				il.Emit(OpCodes.Call, data.ReaderMethodInfo);
			}

#if !DNXCORE50
            if (serializer.Settings.SupportSerializationCallbacks)
			{
				foreach (var m in GetMethodsWithAttributes(type, typeof(System.Runtime.Serialization.OnDeserializedAttribute)))
					EmitCallToDeserializingCallback(type, il, m);
			}

			if (serializer.Settings.SupportIDeserializationCallback)
			{
				if (typeof(System.Runtime.Serialization.IDeserializationCallback).IsAssignableFrom(type))
				{
                    // .NET Core does not support all GetMethod() overloads
                    var miOnDeserialization = typeof(System.Runtime.Serialization.IDeserializationCallback)
                        .GetTypeInfo().GetMethod("OnDeserialization", new[] { typeof(Object) });

                    //var miOnDeserialization = typeof(System.Runtime.Serialization.IDeserializationCallback)
                    //    .GetTypeInfo().GetMethod("OnDeserialization",
                    //                        BindingFlags.Instance | BindingFlags.Public,
                    //                        null, new[] { typeof(Object) }, null);

                    il.Emit(OpCodes.Ldarg_2);
					il.Emit(OpCodes.Ldnull);
					il.Emit(OpCodes.Constrained, type);
					il.Emit(OpCodes.Callvirt, miOnDeserialization);
				}
			}
#endif

			il.Emit(OpCodes.Ret);
		}
	}
}
