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

		static ITypeSerializer[] s_typeSerializers = new ITypeSerializer[] {
			new ObjectSerializer(),
			new PrimitivesSerializer(),
			new ArraySerializer(),
			new EnumSerializer(),
			new DictionarySerializer(),
			new GenericSerializer(),
		};

		static ITypeSerializer[] s_userTypeSerializers;

		public static bool IsInitialized { get; private set; }

		/// <summary>
		/// Initialize NetSerializer
		/// </summary>
		/// <param name="rootTypes">Types to be (de)serialized</param>
		public static void Initialize(IEnumerable<Type> rootTypes)
		{
			Initialize(rootTypes, new ITypeSerializer[0]);
		}

		/// <summary>
		/// Initialize NetSerializer
		/// </summary>
		/// <param name="rootTypes">Types to be (de)serialized</param>
		/// <param name="userTypeSerializers">Array of custom serializers</param>
		public static void Initialize(IEnumerable<Type> rootTypes, ITypeSerializer[] userTypeSerializers)
		{
			if (IsInitialized)
				throw new InvalidOperationException("NetSerializer already initialized");

			if (userTypeSerializers.All(s => s is IDynamicTypeSerializer || s is IStaticTypeSerializer) == false)
				throw new ArgumentException("TypeSerializers have to implement IDynamicTypeSerializer or  IStaticTypeSerializer");

			s_userTypeSerializers = userTypeSerializers;

			var typeDataMap = GenerateTypeData(rootTypes);

			GenerateDynamic(typeDataMap);

			s_typeIDMap = typeDataMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.TypeID);

#if GENERATE_DEBUGGING_ASSEMBLY
			// Note: GenerateDebugAssembly overwrites some fields from typeDataMap
			GenerateDebugAssembly(typeDataMap);
#endif
			IsInitialized = true;
		}

		public static void Serialize(Stream stream, object data)
		{
			if (!IsInitialized)
				throw new InvalidOperationException("NetSerializer not initialized");

			s_serializerSwitch(stream, data);
		}

		public static object Deserialize(Stream stream)
		{
			if (!IsInitialized)
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

		static Dictionary<Type, TypeData> GenerateTypeData(IEnumerable<Type> rootTypes)
		{
			var map = new Dictionary<Type, TypeData>();
			var stack = new Stack<Type>(PrimitivesSerializer.GetSupportedTypes().Concat(rootTypes));

			stack.Push(typeof(object));

			// TypeID 0 is reserved for null
			ushort typeID = 1;

			while (stack.Count > 0)
			{
				var type = stack.Pop();

				if (map.ContainsKey(type))
					continue;

				if (type.IsAbstract || type.IsInterface)
					continue;

				if (type.ContainsGenericParameters)
					throw new NotSupportedException(String.Format("Type {0} contains generic parameters", type.FullName));

				var serializer = s_userTypeSerializers.FirstOrDefault(h => h.Handles(type));

				if (serializer == null)
					serializer = s_typeSerializers.FirstOrDefault(h => h.Handles(type));

				if (serializer == null)
					throw new NotSupportedException(String.Format("No serializer for {0}", type.FullName));

				foreach (var t in serializer.GetSubtypes(type))
					stack.Push(t);

				TypeData typeData;

				if (serializer is IStaticTypeSerializer)
				{
					var sts = (IStaticTypeSerializer)serializer;

					MethodInfo writer;
					MethodInfo reader;

					sts.GetStaticMethods(type, out writer, out reader);

					Debug.Assert(writer != null && reader != null);

					typeData = new TypeData(typeID++, writer, reader);

				}
				else if (serializer is IDynamicTypeSerializer)
				{
					var dts = (IDynamicTypeSerializer)serializer;

					typeData = new TypeData(typeID++, dts);
				}
				else
				{
					throw new Exception();
				}

				map[type] = typeData;
			}

			return map;
		}

		static void GenerateDynamic(Dictionary<Type, TypeData> map)
		{
			/* generate stubs */
			foreach (var kvp in map)
			{
				var type = kvp.Key;
				var td = kvp.Value;

				if (!td.IsGenerated)
					continue;

				var writerDm = SerializerCodegen.GenerateDynamicSerializerStub(type);
				td.WriterMethodInfo = writerDm;
				td.WriterILGen = writerDm.GetILGenerator();

				var readerDm = DeserializerCodegen.GenerateDynamicDeserializerStub(type);
				td.ReaderMethodInfo = readerDm;
				td.ReaderILGen = readerDm.GetILGenerator();
			}

			var ctx = new CodeGenContext(map);

			/* generate bodies */

			foreach (var kvp in map)
			{
				var type = kvp.Key;
				var td = kvp.Value;

				if (!td.IsGenerated)
					continue;

				td.TypeSerializer.GenerateWriterMethod(type, ctx, td.WriterILGen);
				td.TypeSerializer.GenerateReaderMethod(type, ctx, td.ReaderILGen);
			}

			var writer = (DynamicMethod)ctx.GetWriterMethodInfo(typeof(object));
			var reader = (DynamicMethod)ctx.GetReaderMethodInfo(typeof(object));

			s_serializerSwitch = (SerializerSwitch)writer.CreateDelegate(typeof(SerializerSwitch));
			s_deserializerSwitch = (DeserializerSwitch)reader.CreateDelegate(typeof(DeserializerSwitch));
		}

#if GENERATE_DEBUGGING_ASSEMBLY
		static void GenerateDebugAssembly(Dictionary<Type, TypeData> map)
		{
			var ab = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("NetSerializerDebug"), AssemblyBuilderAccess.RunAndSave);
			var modb = ab.DefineDynamicModule("NetSerializerDebug.dll");
			var tb = modb.DefineType("NetSerializer", TypeAttributes.Public);

			/* generate stubs */
			foreach (var kvp in map)
			{
				var type = kvp.Key;
				var td = kvp.Value;

				if (!td.IsGenerated)
					continue;

				var mb = SerializerCodegen.GenerateStaticSerializerStub(tb, type);
				td.WriterMethodInfo = mb;
				td.WriterILGen = mb.GetILGenerator();

				var dm = DeserializerCodegen.GenerateStaticDeserializerStub(tb, type);
				td.ReaderMethodInfo = dm;
				td.ReaderILGen = dm.GetILGenerator();
			}

			var ctx = new CodeGenContext(map);

			/* generate bodies */

			foreach (var kvp in map)
			{
				var type = kvp.Key;
				var td = kvp.Value;

				if (!td.IsGenerated)
					continue;

				td.TypeSerializer.GenerateWriterMethod(type, ctx, td.WriterILGen);
				td.TypeSerializer.GenerateReaderMethod(type, ctx, td.ReaderILGen);
			}

			tb.CreateType();
			ab.Save("NetSerializerDebug.dll");
		}
#endif

		/* called from the dynamically generated code */
		static ushort GetTypeID(object ob)
		{
			ushort id;

			if (ob == null)
				return 0;

			var type = ob.GetType();

			if (s_typeIDMap.TryGetValue(type, out id) == false)
				throw new InvalidOperationException(String.Format("Unknown type {0}", type.FullName));

			return id;
		}
	}
}
