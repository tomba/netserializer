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
		sealed class TypeData
		{
			public TypeData(ushort typeID)
			{
				this.TypeID = typeID;
				this.IsStatic = false;
			}

			public TypeData(ushort typeID, MethodInfo writer, MethodInfo reader)
			{
				this.TypeID = typeID;
				this.WriterMethodInfo = writer;
				this.ReaderMethodInfo = reader;
				this.IsStatic = true;
			}

			public readonly bool IsStatic;
			public ushort TypeID;
			public MethodInfo WriterMethodInfo;
			public ILGenerator WriterILGen;
			public MethodInfo ReaderMethodInfo;
			public ILGenerator ReaderILGen;
		}

		static Dictionary<Type, TypeData> s_map;

		delegate void SerializerSwitch(Stream stream, object ob);
		static MethodInfo s_serializerSwitchMethodInfo;
		static SerializerSwitch s_serializerSwitch;

		delegate void DeserializerSwitch(Stream stream, out object ob);
		static MethodInfo s_deserializerSwitchMethodInfo;
		static DeserializerSwitch s_deserializerSwitch;

		public static void Initialize(Type[] rootTypes)
		{
			if (s_map != null)
				throw new Exception();

			s_map = new Dictionary<Type, TypeData>();
#if DEBUG
			GenerateAssembly(rootTypes);
#endif
			GenerateDynamic(rootTypes);
		}

		public static void Serialize(Stream stream, object data)
		{
			if (!s_map.ContainsKey(data.GetType()))
				throw new ArgumentException("Type is not known");

			D("Serializing {0}", data.GetType().Name);

			s_serializerSwitch(stream, data);
		}

		public static object Deserialize(Stream stream)
		{
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

		static void AddTypes(Type[] rootTypes)
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

			rootTypes = typeSet.ToArray();
			// Sort the types so that we get the same typeID, regardless of the order in the HashSet
			Array.Sort(rootTypes, (a, b) => String.Compare(a.FullName, b.FullName, StringComparison.Ordinal));

			ushort typeID = 0;
			foreach (var type in rootTypes)
			{
				if (type.IsInterface || type.IsAbstract)
					continue;

				if (!type.IsSerializable)
					throw new NotSupportedException(String.Format("Type {0} is not marked as Serializable", type.ToString()));

				var writer = Primitives.GetWritePrimitive(type);
				var reader = Primitives.GetReadPrimitive(type.MakeByRefType());

				if ((writer != null) != (reader != null))
					throw new Exception();

				if (type.IsPrimitive && writer == null)
					throw new Exception();

				var isStatic = writer != null;

				if (isStatic)
					s_map[type] = new TypeData(typeID++, writer, reader);
				else
					s_map[type] = new TypeData(typeID++);
			}
		}

		static void CollectTypes(Type type, HashSet<Type> typeSet)
		{
			if (typeSet.Contains(type))
				return;

			typeSet.Add(type);

			if (type.IsArray)
			{
				CollectTypes(type.GetElementType(), typeSet);
			}
			else
			{
				var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
					.Where(fi => (fi.Attributes & FieldAttributes.NotSerialized) == 0);

				foreach (var field in fields)
					CollectTypes(field.FieldType, typeSet);
			}
		}

		static void GenerateDynamic(Type[] rootTypes)
		{
			AddTypes(rootTypes);

			var types = s_map.Where(kvp => kvp.Value.IsStatic == false).Select(kvp => kvp.Key);

			/* generate stubs */
			foreach (var type in types)
			{
				var dm = GenerateDynamicSerializerStub(type);
				s_map[type].WriterMethodInfo = dm;
				s_map[type].WriterILGen = dm.GetILGenerator();
			}

			foreach (var type in types)
			{
				var dm = GenerateDynamicDeserializerStub(type);
				s_map[type].ReaderMethodInfo = dm;
				s_map[type].ReaderILGen = dm.GetILGenerator();
			}

			var serializerSwitchMethod = new DynamicMethod("SerializerSwitch", null,
				new Type[] { typeof(Stream), typeof(object) },
				typeof(Serializer), true);
			serializerSwitchMethod.DefineParameter(1, ParameterAttributes.None, "stream");
			serializerSwitchMethod.DefineParameter(2, ParameterAttributes.None, "value");
			s_serializerSwitchMethodInfo = serializerSwitchMethod;

			var deserializerSwitchMethod = new DynamicMethod("DeserializerSwitch", null,
				new Type[] { typeof(Stream), typeof(object).MakeByRefType() },
				typeof(Serializer), true);
			deserializerSwitchMethod.DefineParameter(1, ParameterAttributes.None, "stream");
			deserializerSwitchMethod.DefineParameter(2, ParameterAttributes.Out, "value");
			s_deserializerSwitchMethodInfo = deserializerSwitchMethod;


			/* generate bodies */
			foreach (var type in types)
				GenerateSerializerBody(type, s_map[type].WriterILGen);

			foreach (var type in types)
				GenerateDeserializerBody(type, s_map[type].ReaderILGen);

			var ilGen = serializerSwitchMethod.GetILGenerator();
			GenerateSerializerSwitch(ilGen, s_map);
			s_serializerSwitch = (SerializerSwitch)serializerSwitchMethod.CreateDelegate(typeof(SerializerSwitch));

			ilGen = deserializerSwitchMethod.GetILGenerator();
			GenerateDeserializerSwitch(ilGen, s_map);
			s_deserializerSwitch = (DeserializerSwitch)deserializerSwitchMethod.CreateDelegate(typeof(DeserializerSwitch));
		}

		static void GenerateAssembly(Type[] rootTypes)
		{
			AddTypes(rootTypes);

			var types = s_map.Where(kvp => kvp.Value.IsStatic == false).Select(kvp => kvp.Key);

			var ab = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("NetSerializerDebug"), AssemblyBuilderAccess.RunAndSave);
			var modb = ab.DefineDynamicModule("NetSerializerDebug.dll");
			var tb = modb.DefineType("NetSerializer", TypeAttributes.Public);

			/* generate stubs */
			foreach (var type in types)
			{
				var mb = GenerateStaticSerializerStub(tb, type);
				s_map[type].WriterMethodInfo = mb;
				s_map[type].WriterILGen = mb.GetILGenerator();
			}

			foreach (var type in types)
			{
				var dm = GenerateStaticDeserializerStub(tb, type);
				s_map[type].ReaderMethodInfo = dm;
				s_map[type].ReaderILGen = dm.GetILGenerator();
			}

			var serializerSwitchMethod = tb.DefineMethod("SerializerSwitch", MethodAttributes.Public | MethodAttributes.Static, null, new Type[] { typeof(Stream), typeof(object) });
			serializerSwitchMethod.DefineParameter(1, ParameterAttributes.None, "stream");
			serializerSwitchMethod.DefineParameter(2, ParameterAttributes.None, "value");
			s_serializerSwitchMethodInfo = serializerSwitchMethod;

			var deserializerSwitchMethod = tb.DefineMethod("DeserializerSwitch", MethodAttributes.Public | MethodAttributes.Static, null, new Type[] { typeof(Stream), typeof(object).MakeByRefType() });
			deserializerSwitchMethod.DefineParameter(1, ParameterAttributes.None, "stream");
			deserializerSwitchMethod.DefineParameter(2, ParameterAttributes.Out, "value");
			s_deserializerSwitchMethodInfo = deserializerSwitchMethod;

			/* generate bodies */
			foreach (var type in types)
				GenerateSerializerBody(type, s_map[type].WriterILGen);

			foreach (var type in types)
				GenerateDeserializerBody(type, s_map[type].ReaderILGen);

			var ilGen = serializerSwitchMethod.GetILGenerator();
			GenerateSerializerSwitch(ilGen, s_map);

			ilGen = deserializerSwitchMethod.GetILGenerator();
			GenerateDeserializerSwitch(ilGen, s_map);

			tb.CreateType();
			ab.Save("NetSerializerDebug.dll");
		}

		static ushort GetTypeID(Type type)
		{
			TypeData data;

			if (s_map.TryGetValue(type, out data) == false)
				throw new Exception(String.Format("Unknown type {0}", type));

			return data.TypeID;
		}

		static ushort GetTypeID(object ob)
		{
			return GetTypeID(ob.GetType());
		}

		static FieldInfo[] GetFieldInfos(Type type)
		{
			if ((type.Attributes & TypeAttributes.Serializable) == 0)
				throw new Exception();

			var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
				.Where(fi => (fi.Attributes & FieldAttributes.NotSerialized) == 0)
				.ToArray();

			// Sort the fields so that they are in the same order, regardless how Type.GetFields works
			Array.Sort(fields, (a, b) => String.Compare(a.Name, b.Name, StringComparison.Ordinal));

			if (type.BaseType == null)
			{
				return fields;
			}
			else
			{
				var baseFields = GetFieldInfos(type.BaseType);
				return baseFields.Concat(fields).ToArray();
			}
		}

		static MethodInfo GetWriterMethodInfo(Type type)
		{
			if (!s_map.ContainsKey(type))
				throw new Exception(String.Format("Unknown type {0}", type));

			return s_map[type].WriterMethodInfo;
		}

		static ILGenerator GetWriterILGen(Type type)
		{
			return s_map[type].WriterILGen;
		}

		static MethodInfo GetReaderMethodInfo(Type type)
		{
			return s_map[type].ReaderMethodInfo;
		}

		static ILGenerator GetReaderILGen(Type type)
		{
			return s_map[type].ReaderILGen;
		}
	}
}
