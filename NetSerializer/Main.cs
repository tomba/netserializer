using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

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
				throw new Exception();

			var types = CollectTypes(rootTypes);

#if DEBUG
			GenerateAssembly(types);
#endif
			s_typeIDMap = GenerateDynamic(types);

			s_initialized = true;
		}

		public static void Serialize(Stream stream, object data)
		{
			if (!s_initialized)
				throw new Exception();

			if (!s_typeIDMap.ContainsKey(data.GetType()))
				throw new ArgumentException("Type is not known");

			D("Serializing {0}", data.GetType().Name);

			s_serializerSwitch(stream, data);
		}

		public static object Deserialize(Stream stream)
		{
			if (!s_initialized)
				throw new Exception();

			D("Deserializing");

			object o;
			s_deserializerSwitch(stream, out o);
			return o;
		}

		[System.Diagnostics.Conditional("DEBUG")]
		static void D(string fmt, params object[] args)
		{
			//Console.WriteLine("S: " + String.Format(fmt, args));
		}

		[System.Diagnostics.Conditional("DEBUG")]
		static void D(ILGenerator ilGen, string fmt, params object[] args)
		{
			//ilGen.EmitWriteLine("E: " + String.Format(fmt, args));
		}

		static void CollectTypes(Type type, HashSet<Type> typeSet)
		{
			if (typeSet.Contains(type))
				return;

			if (type.IsAbstract)
				return;

			if (type.IsInterface)
				return;

			if (!type.IsSerializable)
				throw new NotSupportedException(String.Format("Type {0} is not marked as Serializable", type.ToString()));

			typeSet.Add(type);

			if (type.IsArray)
			{
				CollectTypes(type.GetElementType(), typeSet);
			}
			else
			{
				var fields = GetFieldInfos(type);

				foreach (var field in fields)
					CollectTypes(field.FieldType, typeSet);
			}
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

		static Dictionary<Type, TypeData> GenerateTypeData(Type[] types)
		{
			var map = new Dictionary<Type, TypeData>(types.Length);

			// TypeID 0 is reserved for null
			ushort typeID = 1;
			foreach (var type in types)
			{
				var writer = Primitives.GetWritePrimitive(type);
				var reader = Primitives.GetReadPrimitive(type.MakeByRefType());

				if ((writer != null) != (reader != null))
					throw new Exception();

				var isStatic = writer != null;

				if (type.IsPrimitive && isStatic == false)
					throw new Exception();

				var td = new TypeData(typeID++);

				if (isStatic)
				{
					td.WriterMethodInfo = writer;
					td.ReaderMethodInfo = reader;
					td.IsDynamic = false;
				}
				else
				{
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
				var dm = GenerateDynamicSerializerStub(type);
				map[type].WriterMethodInfo = dm;
				map[type].WriterILGen = dm.GetILGenerator();
			}

			foreach (var type in nonStaticTypes)
			{
				var dm = GenerateDynamicDeserializerStub(type);
				map[type].ReaderMethodInfo = dm;
				map[type].ReaderILGen = dm.GetILGenerator();
			}

			var serializerSwitchMethod = new DynamicMethod("SerializerSwitch", null,
				new Type[] { typeof(Stream), typeof(object) },
				typeof(Serializer), true);
			serializerSwitchMethod.DefineParameter(1, ParameterAttributes.None, "stream");
			serializerSwitchMethod.DefineParameter(2, ParameterAttributes.None, "value");
			var serializerSwitchMethodInfo = serializerSwitchMethod;

			var deserializerSwitchMethod = new DynamicMethod("DeserializerSwitch", null,
				new Type[] { typeof(Stream), typeof(object).MakeByRefType() },
				typeof(Serializer), true);
			deserializerSwitchMethod.DefineParameter(1, ParameterAttributes.None, "stream");
			deserializerSwitchMethod.DefineParameter(2, ParameterAttributes.Out, "value");
			var deserializerSwitchMethodInfo = deserializerSwitchMethod;

			var ctx = new CodeGenContext(map, serializerSwitchMethodInfo, deserializerSwitchMethodInfo);

			/* generate bodies */
			foreach (var type in nonStaticTypes)
				GenerateSerializerBody(ctx, type, map[type].WriterILGen);

			foreach (var type in nonStaticTypes)
				GenerateDeserializerBody(ctx, type, map[type].ReaderILGen);

			var ilGen = serializerSwitchMethod.GetILGenerator();
			GenerateSerializerSwitch(ctx, ilGen, map);
			s_serializerSwitch = (SerializerSwitch)serializerSwitchMethod.CreateDelegate(typeof(SerializerSwitch));

			ilGen = deserializerSwitchMethod.GetILGenerator();
			GenerateDeserializerSwitch(ctx, ilGen, map);
			s_deserializerSwitch = (DeserializerSwitch)deserializerSwitchMethod.CreateDelegate(typeof(DeserializerSwitch));

			return map.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.TypeID);
		}

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
				var mb = GenerateStaticSerializerStub(tb, type);
				map[type].WriterMethodInfo = mb;
				map[type].WriterILGen = mb.GetILGenerator();
			}

			foreach (var type in nonStaticTypes)
			{
				var dm = GenerateStaticDeserializerStub(tb, type);
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
				GenerateSerializerBody(ctx, type, map[type].WriterILGen);

			foreach (var type in nonStaticTypes)
				GenerateDeserializerBody(ctx, type, map[type].ReaderILGen);

			var ilGen = serializerSwitchMethod.GetILGenerator();
			GenerateSerializerSwitch(ctx, ilGen, map);

			ilGen = deserializerSwitchMethod.GetILGenerator();
			GenerateDeserializerSwitch(ctx, ilGen, map);

			tb.CreateType();
			ab.Save("NetSerializerDebug.dll");
		}

		/* called from the dynamically generated code */
		static ushort GetTypeID(object ob)
		{
			ushort id;

			if (ob == null)
				return 0;

			if (s_typeIDMap.TryGetValue(ob.GetType(), out id) == false)
				throw new Exception(String.Format("Unknown type {0}", ob.GetType()));

			return id;
		}

		static IEnumerable<FieldInfo> GetFieldInfos(Type type)
		{
			if ((type.Attributes & TypeAttributes.Serializable) == 0)
				throw new Exception();

			var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
				.Where(fi => (fi.Attributes & FieldAttributes.NotSerialized) == 0)
				.OrderBy(f => f.Name, StringComparer.Ordinal);

			if (type.BaseType == null)
			{
				return fields;
			}
			else
			{
				var baseFields = GetFieldInfos(type.BaseType);
				return baseFields.Concat(fields);
			}
		}

		sealed class CodeGenContext
		{
			public Dictionary<Type, TypeData> m_typeMap;

			public CodeGenContext(Dictionary<Type, TypeData> typeMap, MethodInfo serializerSwitch, MethodInfo deserializerSwitch)
			{
				m_typeMap = typeMap;
				this.SerializerSwitchMethodInfo = serializerSwitch;
				this.DeserializerSwitchMethodInfo = deserializerSwitch;
			}

			public MethodInfo SerializerSwitchMethodInfo { get; private set; }
			public MethodInfo DeserializerSwitchMethodInfo { get; private set; }

			public MethodInfo GetWriterMethodInfo(Type type)
			{
				if (!m_typeMap.ContainsKey(type))
					throw new Exception(String.Format("Unknown type {0}", type));

				return m_typeMap[type].WriterMethodInfo;
			}

			public ILGenerator GetWriterILGen(Type type)
			{
				return m_typeMap[type].WriterILGen;
			}

			public MethodInfo GetReaderMethodInfo(Type type)
			{
				return m_typeMap[type].ReaderMethodInfo;
			}

			public ILGenerator GetReaderILGen(Type type)
			{
				return m_typeMap[type].ReaderILGen;
			}

			public bool IsDynamic(Type type)
			{
				return m_typeMap[type].IsDynamic;
			}
		}

		sealed class TypeData
		{
			public TypeData(ushort typeID)
			{
				this.TypeID = typeID;
			}

			public ushort TypeID;
			public bool IsDynamic;
			public MethodInfo WriterMethodInfo;
			public ILGenerator WriterILGen;
			public MethodInfo ReaderMethodInfo;
			public ILGenerator ReaderILGen;
		}
	}
}
