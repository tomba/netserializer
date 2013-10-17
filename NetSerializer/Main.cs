/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;

namespace NetSerializer
{
	public static partial class Serializer
	{
		static Dictionary<Type, ushort> s_typeIDMap;

		delegate void SerializerSwitch(Stream stream, object ob);
		delegate void DeserializerSwitch(Stream stream, out object ob);

		static SerializerSwitch s_serializerSwitch;
		static DeserializerSwitch s_deserializerSwitch;

		static bool s_initialized;

		public static void Initialize(Type[] rootTypes)
		{
			if (s_initialized)
				throw new InvalidOperationException("NetSerializer already initialized");

			var types = CollectTypes(rootTypes);

#if GENERATE_DEBUGGING_ASSEMBLY
			GenerateAssembly(types);
#endif
			s_typeIDMap = GenerateDynamic(types);

			s_initialized = true;
		}

		public static void Serialize(Stream stream, object data)
		{
			if (!s_initialized)
				throw new InvalidOperationException("NetSerializer not initialized");

			s_serializerSwitch(stream, data);
		}

		public static object Deserialize(Stream stream)
		{
			if (!s_initialized)
				throw new InvalidOperationException("NetSerializer not initialized");

			object o;
			s_deserializerSwitch(stream, out o);
			return o;
		}

		internal static void SerializeInternal(Stream stream, object data)
		{
			s_serializerSwitch(stream, data);
		}

		internal static object DeserializeInternal(Stream stream)
		{
			object o;
			s_deserializerSwitch(stream, out o);
			return o;
		}

		static Type[] CollectTypes(Type[] rootTypes)
		{
			var primitives = new Type[] {
				typeof(bool),
				typeof(byte), typeof(sbyte),
				typeof(char),
				typeof(ushort), typeof(short),
				typeof(uint), typeof(int),
				typeof(ulong), typeof(long),
				typeof(float), typeof(double),
				typeof(string),
			};

			var typeSet = new HashSet<Type>(primitives);

			foreach (var type in rootTypes)
				CollectTypes(type, typeSet);

			return typeSet
				.OrderBy(t => t.FullName, StringComparer.Ordinal)
				.ToArray();
		}

		static void CollectTypes(Type type, HashSet<Type> typeSet)
		{
			if (typeSet.Contains(type))
				return;

			if (type.IsAbstract)
				return;

			if (type.IsInterface)
				return;

#if !SILVERLIGHT
			if (!type.IsSerializable)
				throw new NotSupportedException(String.Format("Type {0} is not marked as Serializable", type.FullName));
#endif

			if (type.ContainsGenericParameters)
				throw new NotSupportedException(String.Format("Type {0} contains generic parameters", type.FullName));

			typeSet.Add(type);

			if (type.IsArray)
			{
				CollectTypes(type.GetElementType(), typeSet);
			}
			else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
			{
				var args = type.GetGenericArguments();

				Debug.Assert(args.Length == 2);

				// Dictionary<K,V> is stored as KeyValuePair<K,V>[]

				var arrayType = typeof(KeyValuePair<,>).MakeGenericType(args).MakeArrayType();

				CollectTypes(arrayType, typeSet);
			}
			else
			{
#if SERIALIZE_PROPERTIES
                var fields = Helpers.GetPropertyInfos(type);
                foreach (var field in fields)
                    CollectTypes(field.PropertyType, typeSet);
#else
                var fields = Helpers.GetFieldInfos(type);
                foreach (var field in fields)
					CollectTypes(field.FieldType, typeSet);
#endif
            }
		}

		static Dictionary<Type, TypeData> GenerateTypeData(Type[] types)
		{
			var map = new Dictionary<Type, TypeData>(types.Length);

			// TypeID 0 is reserved for null
			ushort typeID = 1;
			foreach (var type in types)
			{
				MethodInfo writer;
				MethodInfo reader;

				bool isStatic = Helpers.GetPrimitives(typeof(Primitives), type, out writer, out reader);

				if (type.IsPrimitive && isStatic == false)
					throw new InvalidOperationException(String.Format("Missing primitive read/write methods for {0}", type.FullName));

				var td = new TypeData(typeID++);

				if (isStatic)
				{
					if (writer.IsGenericMethodDefinition)
					{
						Debug.Assert(type.IsGenericType);

						var genArgs = type.GetGenericArguments();

						writer = writer.MakeGenericMethod(genArgs);
						reader = reader.MakeGenericMethod(genArgs);
					}

					td.WriterMethodInfo = writer;
					td.ReaderMethodInfo = reader;
					td.IsDynamic = false;
				}
				else
				{
#if !SILVERLIGHT
					if (typeof(System.Runtime.Serialization.ISerializable).IsAssignableFrom(type))
						throw new InvalidOperationException(String.Format("Cannot serialize {0}: ISerializable not supported", type.FullName));
#endif

					td.IsDynamic = true;
				}

				map[type] = td;
			}

			return map;
		}

		static Dictionary<Type, ushort> GenerateDynamic(Type[] types)
		{
			Dictionary<Type, TypeData> map = GenerateTypeData(types);

			var nonStaticTypes = map.Where(kvp => kvp.Value.IsDynamic).Select(kvp => kvp.Key);

			/* generate stubs */
			foreach (var type in nonStaticTypes)
			{
				var dm = SerializerCodegen.GenerateDynamicSerializerStub(type);
				map[type].WriterMethodInfo = dm;
				map[type].WriterILGen = dm.GetILGenerator();
			}

			foreach (var type in nonStaticTypes)
			{
				var dm = DeserializerCodegen.GenerateDynamicDeserializerStub(type);
				map[type].ReaderMethodInfo = dm;
				map[type].ReaderILGen = dm.GetILGenerator();
			}

#if SILVERLIGHT
			var serializerSwitchMethod = new DynamicMethod("SerializerSwitch", null,
				new Type[] { typeof(Stream), typeof(object) });
#else
            var serializerSwitchMethod = new DynamicMethod("SerializerSwitch", null,
				new Type[] { typeof(Stream), typeof(object) },
				typeof(Serializer), true);
#endif
            serializerSwitchMethod.DefineParameter(1, ParameterAttributes.None, "stream");
			serializerSwitchMethod.DefineParameter(2, ParameterAttributes.None, "value");
			var serializerSwitchMethodInfo = serializerSwitchMethod;

#if SILVERLIGHT
			var deserializerSwitchMethod = new DynamicMethod("DeserializerSwitch", null,
				new Type[] { typeof(Stream), typeof(object).MakeByRefType() });
#else
            var deserializerSwitchMethod = new DynamicMethod("DeserializerSwitch", null,
				new Type[] { typeof(Stream), typeof(object).MakeByRefType() },
				typeof(Serializer), true);
#endif
            deserializerSwitchMethod.DefineParameter(1, ParameterAttributes.None, "stream");
			deserializerSwitchMethod.DefineParameter(2, ParameterAttributes.Out, "value");
			var deserializerSwitchMethodInfo = deserializerSwitchMethod;

			var ctx = new CodeGenContext(map, serializerSwitchMethodInfo, deserializerSwitchMethodInfo);

			/* generate bodies */
			foreach (var type in nonStaticTypes)
				SerializerCodegen.GenerateSerializerBody(ctx, type, map[type].WriterILGen);

			foreach (var type in nonStaticTypes)
				DeserializerCodegen.GenerateDeserializerBody(ctx, type, map[type].ReaderILGen);

			var ilGen = serializerSwitchMethod.GetILGenerator();
			SerializerCodegen.GenerateSerializerSwitch(ctx, ilGen, map);
			s_serializerSwitch = (SerializerSwitch)serializerSwitchMethod.CreateDelegate(typeof(SerializerSwitch));

			ilGen = deserializerSwitchMethod.GetILGenerator();
			DeserializerCodegen.GenerateDeserializerSwitch(ctx, ilGen, map);
			s_deserializerSwitch = (DeserializerSwitch)deserializerSwitchMethod.CreateDelegate(typeof(DeserializerSwitch));

			return map.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.TypeID);
		}

#if GENERATE_DEBUGGING_ASSEMBLY
		static void GenerateAssembly(Type[] types)
		{
			Dictionary<Type, TypeData> map = GenerateTypeData(types);

			var nonStaticTypes = map.Where(kvp => kvp.Value.IsDynamic).Select(kvp => kvp.Key);

			var ab = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("NetSerializerDebug"), AssemblyBuilderAccess.RunAndSave);
			var modb = ab.DefineDynamicModule("NetSerializerDebug.dll");
			var tb = modb.DefineType("NetSerializer", TypeAttributes.Public);

			/* generate stubs */
			foreach (var type in nonStaticTypes)
			{
				var mb = SerializerCodegen.GenerateStaticSerializerStub(tb, type);
				map[type].WriterMethodInfo = mb;
				map[type].WriterILGen = mb.GetILGenerator();
			}

			foreach (var type in nonStaticTypes)
			{
				var dm = DeserializerCodegen.GenerateStaticDeserializerStub(tb, type);
				map[type].ReaderMethodInfo = dm;
				map[type].ReaderILGen = dm.GetILGenerator();
			}

			var serializerSwitchMethod = tb.DefineMethod("SerializerSwitch", MethodAttributes.Public | MethodAttributes.Static, null, new Type[] { typeof(Stream), typeof(object) });
			serializerSwitchMethod.DefineParameter(1, ParameterAttributes.None, "stream");
			serializerSwitchMethod.DefineParameter(2, ParameterAttributes.None, "value");
			var serializerSwitchMethodInfo = serializerSwitchMethod;

			var deserializerSwitchMethod = tb.DefineMethod("DeserializerSwitch", MethodAttributes.Public | MethodAttributes.Static, null, new Type[] { typeof(Stream), typeof(object).MakeByRefType() });
			deserializerSwitchMethod.DefineParameter(1, ParameterAttributes.None, "stream");
			deserializerSwitchMethod.DefineParameter(2, ParameterAttributes.Out, "value");
			var deserializerSwitchMethodInfo = deserializerSwitchMethod;

			var ctx = new CodeGenContext(map, serializerSwitchMethodInfo, deserializerSwitchMethodInfo);

			/* generate bodies */
			foreach (var type in nonStaticTypes)
				SerializerCodegen.GenerateSerializerBody(ctx, type, map[type].WriterILGen);

			foreach (var type in nonStaticTypes)
				DeserializerCodegen.GenerateDeserializerBody(ctx, type, map[type].ReaderILGen);

			var ilGen = serializerSwitchMethod.GetILGenerator();
			SerializerCodegen.GenerateSerializerSwitch(ctx, ilGen, map);

			ilGen = deserializerSwitchMethod.GetILGenerator();
			DeserializerCodegen.GenerateDeserializerSwitch(ctx, ilGen, map);

			tb.CreateType();
			ab.Save("NetSerializerDebug.dll");
		}
#endif

		/* called from the dynamically generated code */
#if SILVERLIGHT
        public static ushort GetTypeID(object ob)
#else
		static ushort GetTypeID(object ob)
#endif
		{
			ushort id;

			if (ob == null)
				return 0;

			if (s_typeIDMap.TryGetValue(ob.GetType(), out id) == false)
				throw new InvalidOperationException(String.Format("Unknown type {0}", ob.GetType().FullName));

			return id;
		}
	}
}
