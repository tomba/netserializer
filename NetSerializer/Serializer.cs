/*
 * Copyright 2015 Tomi Valkeinen
 * 
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
	public class Serializer
	{
		Dictionary<Type, ushort> m_typeIDMap;

		SerializeDelegate[] m_serializerTrampolines;
		DeserializeDelegate[] m_deserializerTrampolines;

		delegate void SerializeDelegate(Serializer serializer, Stream stream, object ob);
		delegate void DeserializeDelegate(Serializer serializer, Stream stream, out object ob);

		static ITypeSerializer[] s_typeSerializers = new ITypeSerializer[] {
			new ObjectSerializer(),
			new PrimitivesSerializer(),
			new ArraySerializer(),
			new EnumSerializer(),
			new DictionarySerializer(),
			new GenericSerializer(),
		};

		ITypeSerializer[] m_userTypeSerializers;

		/// <summary>
		/// Initialize NetSerializer
		/// </summary>
		/// <param name="rootTypes">Types to be (de)serialized</param>
		public Serializer(IEnumerable<Type> rootTypes)
			: this(rootTypes, new ITypeSerializer[0])
		{
		}

		/// <summary>
		/// Initialize NetSerializer
		/// </summary>
		/// <param name="rootTypes">Types to be (de)serialized</param>
		/// <param name="userTypeSerializers">Array of custom serializers</param>
		public Serializer(IEnumerable<Type> rootTypes, ITypeSerializer[] userTypeSerializers)
		{
			if (userTypeSerializers.All(s => s is IDynamicTypeSerializer || s is IStaticTypeSerializer) == false)
				throw new ArgumentException("TypeSerializers have to implement IDynamicTypeSerializer or  IStaticTypeSerializer");

			m_userTypeSerializers = userTypeSerializers;

			var typeDataMap = GenerateTypeData(rootTypes);

			GenerateDynamic(typeDataMap);

			m_typeIDMap = typeDataMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.TypeID);

#if GENERATE_DEBUGGING_ASSEMBLY
			// Note: GenerateDebugAssembly overwrites some fields from typeDataMap
			GenerateDebugAssembly(typeDataMap);
#endif
		}

		public void Serialize(Stream stream, object data)
		{
			Serialize(this, stream, data);
		}

		public object Deserialize(Stream stream)
		{
			object o;
			Deserialize(this, stream, out o);
			return o;
		}

		public void Deserialize(Stream stream, out object ob)
		{
			Deserialize(this, stream, out ob);
		}

		static void Serialize(Serializer serializer, Stream stream, object ob)
		{
			if (ob == null)
			{
				Primitives.WritePrimitive(stream, 0);
				return;
			}

			var type = ob.GetType();

			ushort id;

			if (serializer.m_typeIDMap.TryGetValue(type, out id) == false)
				throw new InvalidOperationException(String.Format("Unknown type {0}", type.FullName));

			Primitives.WritePrimitive(stream, id);

			serializer.m_serializerTrampolines[id](serializer, stream, ob);
		}

		static void Deserialize(Serializer serializer, Stream stream, out object ob)
		{
			ushort id;

			Primitives.ReadPrimitive(stream, out id);

			if (id == 0)
			{
				ob = null;
				return;
			}

			serializer.m_deserializerTrampolines[id](serializer, stream, out ob);
		}

		Dictionary<Type, TypeData> GenerateTypeData(IEnumerable<Type> rootTypes)
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

				var serializer = m_userTypeSerializers.FirstOrDefault(h => h.Handles(type));

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

		void GenerateDynamic(Dictionary<Type, TypeData> map)
		{
			/* generate stubs */
			foreach (var kvp in map)
			{
				var type = kvp.Key;
				var td = kvp.Value;

				if (!td.IsGenerated)
					continue;

				td.WriterMethodInfo = Helpers.GenerateDynamicSerializerStub(type);
				td.ReaderMethodInfo = Helpers.GenerateDynamicDeserializerStub(type);
			}

			var ctx = new CodeGenContext(map);

			/* generate bodies */

			foreach (var kvp in map)
			{
				var type = kvp.Key;
				var td = kvp.Value;

				if (!td.IsGenerated)
					continue;

				var writerDm = (DynamicMethod)td.WriterMethodInfo;
				td.TypeSerializer.GenerateWriterMethod(type, ctx, writerDm.GetILGenerator());

				var readerDm = (DynamicMethod)td.ReaderMethodInfo;
				td.TypeSerializer.GenerateReaderMethod(type, ctx, readerDm.GetILGenerator());
			}

			/* generate trampolines */

			m_serializerTrampolines = new SerializeDelegate[map.Count + 1];
			m_deserializerTrampolines = new DeserializeDelegate[map.Count + 1];

			foreach (var kvp in map)
			{
				var type = kvp.Key;
				var data = kvp.Value;

				var writer = Helpers.GenerateDynamicSerializerStub(typeof(object));
				Helpers.GenerateSerializerTrampoline(writer.GetILGenerator(), type, data);
				m_serializerTrampolines[data.TypeID] = (SerializeDelegate)writer.CreateDelegate(typeof(SerializeDelegate));

				var reader = Helpers.GenerateDynamicDeserializerStub(typeof(object));
				Helpers.GenerateDeserializerTrampoline(reader.GetILGenerator(), type, data);
				m_deserializerTrampolines[data.TypeID] = (DeserializeDelegate)reader.CreateDelegate(typeof(DeserializeDelegate));
			}
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

				td.WriterMethodInfo = Helpers.GenerateStaticSerializerStub(tb, type);
				td.ReaderMethodInfo = Helpers.GenerateStaticDeserializerStub(tb, type);
			}

			var ctx = new CodeGenContext(map);

			/* generate bodies */

			foreach (var kvp in map)
			{
				var type = kvp.Key;
				var td = kvp.Value;

				if (!td.IsGenerated)
					continue;

				var writerMb = (MethodBuilder)td.WriterMethodInfo;
				td.TypeSerializer.GenerateWriterMethod(type, ctx, writerMb.GetILGenerator());

				var readerMb = (MethodBuilder)td.ReaderMethodInfo;
				td.TypeSerializer.GenerateReaderMethod(type, ctx, readerMb.GetILGenerator());
			}

			/* generate trampolines */
			foreach (var kvp in map)
			{
				var type = kvp.Key;
				var data = kvp.Value;

				var writerMethod = Helpers.GenerateStaticSerializerStub(tb, typeof(object));
				Helpers.GenerateSerializerTrampoline(writerMethod.GetILGenerator(), type, data);

				var readerMethod = Helpers.GenerateStaticDeserializerStub(tb, typeof(object));
				Helpers.GenerateDeserializerTrampoline(readerMethod.GetILGenerator(), type, data);
			}

			tb.CreateType();
			ab.Save("NetSerializerDebug.dll");
		}
#endif
	}
}
