/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */


//??TODO Add support for anonymous   var q = new {Test = "ZZZ", Test1 = 1};
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Threading;

namespace NetSerializer
{
	public static partial class Serializer
	{

#if !GENERATE_SWITCH
		public delegate void SerializationInvokeHandler(Stream stream, object val, ObjectList objList);
		public delegate void DeserializationInvokeHandler(Stream stream, out object val, ObjectList objList);

		private static DeserializationInvokeHandler GetDeserializationInvoker(TypeBuilder tb, MethodInfo methodInfo, Type val_type,  int typeID)
		{
			DynamicMethod dynamicMethod = null;
			ILGenerator il;
#if GENERATE_DEBUGGING_ASSEMBLY
			if (tb != null)
			{
				var methodBuilder = DeserializerCodegen.GenerateStaticDeserializeInvokerStub(tb, typeID);
				il = methodBuilder.GetILGenerator();
			}
			else
#endif
			{
				dynamicMethod = DeserializerCodegen.GenerateDynamicDeserializeInvokerStub();
				il = dynamicMethod.GetILGenerator();
			}

			var local = il.DeclareLocal(val_type);

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldloca_S, local);
			il.Emit(OpCodes.Ldarg_2);

			if (methodInfo.IsGenericMethodDefinition)
			{
				Debug.Assert(val_type.IsGenericType);
				var genArgs = val_type.GetGenericArguments();
				il.EmitCall(OpCodes.Call, methodInfo.MakeGenericMethod(genArgs), null);
			}
			else
			{
				il.EmitCall(OpCodes.Call, methodInfo, null);
			}

			// write result object to out object
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldloc_S, local);

			if (val_type.IsValueType)
				il.Emit(OpCodes.Box, val_type);
			il.Emit(OpCodes.Stind_Ref);
			il.Emit(OpCodes.Ret);

			if (tb != null)
				return null;
			else
				return (DeserializationInvokeHandler)dynamicMethod.CreateDelegate(typeof(DeserializationInvokeHandler));
		}

		private static SerializationInvokeHandler GetSerializationInvoker(TypeBuilder tb, MethodInfo methodInfo, Type val_type, int typeID)
		{
			DynamicMethod dynamicMethod = null;
			ILGenerator il;
#if GENERATE_DEBUGGING_ASSEMBLY
			if (tb != null)
			{
				var methodBuilder = SerializerCodegen.GenerateStaticSerializeInvokerStub(tb, typeID);
				il = methodBuilder.GetILGenerator();
			}
			else
#endif
			{
				dynamicMethod = SerializerCodegen.GenerateDynamicSerializeInvokerStub();
				il = dynamicMethod.GetILGenerator();
			}

			var local = il.DeclareLocal(val_type);

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(val_type.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, val_type);
			il.Emit(OpCodes.Ldarg_2);

			if (methodInfo.IsGenericMethodDefinition)
			{
				Debug.Assert(val_type.IsGenericType);
				var genArgs = val_type.GetGenericArguments();
				il.EmitCall(OpCodes.Call, methodInfo.MakeGenericMethod(genArgs), null);
			}
			else
			{
				il.EmitCall(OpCodes.Call, methodInfo, null);
			}

			il.Emit(OpCodes.Ret);

			if (tb != null)
				return null;
			else
				return (SerializationInvokeHandler)dynamicMethod.CreateDelegate(typeof(SerializationInvokeHandler));
		}
#endif

		class SerializationID
		{
			static internal Dictionary<Type, ushort> predefinedID;
			internal const ushort userIDstart = 1000;
			static internal ushort userID = userIDstart;

			static SerializationID()
			{
				predefinedID = new Dictionary<Type, ushort>();
				// TypeID 0 is reserved for null
				predefinedID.Add(typeof(NetSerializer.ObjectRef), 1);
/**/
				predefinedID.Add(typeof(bool),		2);
				predefinedID.Add(typeof(bool?),		3);
				predefinedID.Add(typeof(byte),		4);
				predefinedID.Add(typeof(byte?),		5);
				predefinedID.Add(typeof(sbyte),		6);
				predefinedID.Add(typeof(sbyte?),	7);
				predefinedID.Add(typeof(char),		8);
				predefinedID.Add(typeof(char?),		9);
				predefinedID.Add(typeof(ushort),	10);
				predefinedID.Add(typeof(ushort?),	11);
				predefinedID.Add(typeof(short),		12);
				predefinedID.Add(typeof(short?),	13);
				predefinedID.Add(typeof(uint),		14);
				predefinedID.Add(typeof(uint?),		15);
 /**/ 
				predefinedID.Add(typeof(int),		16);
/**/
				predefinedID.Add(typeof(int?),		17);
				predefinedID.Add(typeof(ulong),		18);
				predefinedID.Add(typeof(ulong?),	19);
 /**/ 
				predefinedID.Add(typeof(long),		20);
/**/
				predefinedID.Add(typeof(long?),		21);
				predefinedID.Add(typeof(float),		22);
				predefinedID.Add(typeof(float?),	23);
				predefinedID.Add(typeof(double),	24);
				predefinedID.Add(typeof(double?),	25);
 /**/ 
				predefinedID.Add(typeof(string),	26);
/**/
				predefinedID.Add(typeof(DateTime),	27);
				predefinedID.Add(typeof(object),	28);

				predefinedID.Add(typeof(TimeSpan),	29);
				predefinedID.Add(typeof(DateTimeOffset), 30);
				predefinedID.Add(typeof(decimal),	31);
				predefinedID.Add(typeof(Guid),		32);

				predefinedID.Add(typeof(bool[]),  33);
				predefinedID.Add(typeof(byte[]),  34);
				predefinedID.Add(typeof(sbyte[]), 35);
				predefinedID.Add(typeof(char[]),  36);
				predefinedID.Add(typeof(ushort[]), 37);
				predefinedID.Add(typeof(short[]), 38);
				predefinedID.Add(typeof(uint[]),  39);
				predefinedID.Add(typeof(int[]),   40);
				predefinedID.Add(typeof(ulong[]), 41);
				predefinedID.Add(typeof(long[]),  42);
				predefinedID.Add(typeof(float[]), 43);
				predefinedID.Add(typeof(double[]), 44);
/**/
/**/
				predefinedID.Add(typeof(ArrayList), 45);
				predefinedID.Add(typeof(BitArray), 46);
				predefinedID.Add(typeof(Hashtable), 47);
				predefinedID.Add(typeof(Queue), 48);
				predefinedID.Add(typeof(Stack), 49);
#if !MONO
				predefinedID.Add(typeof(SortedList), 50);
#endif
 /**/ 
			}
		}


#if USE_LOCK
		static ReaderWriterLockSlim lck = new ReaderWriterLockSlim();
#endif
		static Dictionary<ushort, TypeData> s_typeID_TypeData;
		static Dictionary<Type, TypeData> s_typeMap = new Dictionary<Type, TypeData>();

#if GENERATE_SWITCH
		static Dictionary<ushort, ushort> s_caseID_typeIDMap;       // typeID => caseID
		static Dictionary<Type, uint> s_Type_caseIDtypeIDMap;       // Type => (typeID | (caseID << 16))

		delegate void SerializerSwitch(Stream stream, object ob, ObjectList objList);
		delegate void DeserializerSwitch(Stream stream, out object ob, ObjectList objList);
		static SerializerSwitch s_serializerSwitch;
		static DeserializerSwitch s_deserializerSwitch;
#endif

		static bool s_initialized;

		public static void Initialize(Type[] rootTypes)
		{
			if (s_initialized)
				throw new InvalidOperationException("NetSerializer already initialized");

#if USE_LOCK
			lck.EnterWriteLock();
			try
#endif
			{
				var types = CollectTypes(rootTypes);

#if GENERATE_DEBUGGING_ASSEMBLY
				GenerateAssembly(types, s_typeMap);
#endif
				s_typeMap = GenerateDynamic(types, s_typeMap);
				s_typeID_TypeData = s_typeMap.ToDictionary(kvp => kvp.Value.TypeID, kvp => kvp.Value);

#if GENERATE_SWITCH
				s_Type_caseIDtypeIDMap = s_typeMap.ToDictionary(kvp => kvp.Key, kvp => (uint)(kvp.Value.TypeID | (kvp.Value.CaseID << 16)));
				s_caseID_typeIDMap = s_typeMap.ToDictionary(kvp => kvp.Value.TypeID, kvp => kvp.Value.CaseID);
#endif
				s_initialized = true;
			}
#if USE_LOCK
			finally
			{
				lck.ExitWriteLock();
			}
#endif
		}

		public static void Register(Type[] regTypes)
		{
			if (!s_initialized)
				throw new InvalidOperationException("NetSerializer not initialized");

#if USE_LOCK
			lck.EnterWriteLock();
			try
#endif
			{
				var ctypes = CollectTypes(regTypes);
				var types = ctypes.Select(v => v).Where(v => !s_typeMap.ContainsKey(v)).ToArray<Type>();

#if GENERATE_DEBUGGING_ASSEMBLY
				GenerateAssembly(types, s_typeMap);
#endif
				s_typeMap = GenerateDynamic(types, s_typeMap);
				s_typeID_TypeData = s_typeMap.ToDictionary(kvp => kvp.Value.TypeID, kvp => kvp.Value);
#if GENERATE_SWITCH
				s_Type_caseIDtypeIDMap = s_typeMap.ToDictionary(kvp => kvp.Key, kvp => (uint)(kvp.Value.TypeID | (kvp.Value.CaseID << 16)));
				s_caseID_typeIDMap = s_typeMap.ToDictionary(kvp => kvp.Value.TypeID, kvp => kvp.Value.CaseID);
#endif
			}
#if USE_LOCK
			finally
			{
				lck.ExitWriteLock();
			}
#endif
		}

		public static void SerializeDeep(Stream stream, object data)
		{
			if (!s_initialized)
				throw new InvalidOperationException("NetSerializer not initialized");

#if USE_LOCK
			lck.EnterReadLock();
			try
#endif
			{
				Serializer.Serialize(stream, data, new ObjectList());
			}
#if USE_LOCK
			finally
			{
				lck.ExitReadLock();
			}
#endif
		}

		public static void Serialize(Stream stream, object data)
		{
			if (!s_initialized)
				throw new InvalidOperationException("NetSerializer not initialized");

#if USE_LOCK
			lck.EnterReadLock();
			try
#endif
			{
				Serializer.Serialize(stream, data, null);
			}
#if USE_LOCK
			finally
			{
				lck.ExitReadLock();
			}
#endif
		}

		internal static void Serialize(Stream stream, object data, ObjectList objList)
		{
			D("Serializing {0}", data.GetType().Name);

#if GENERATE_SWITCH
			s_serializerSwitch(stream, data, objList);
#else
			_SerializerSwitch(stream, data, objList);
#endif
		}

		public static object Deserialize(Stream stream)
		{
			if (!s_initialized)
				throw new InvalidOperationException("NetSerializer not initialized");

#if USE_LOCK
			lck.EnterReadLock();
			try
#endif
			{
				return Serializer.Deserialize(stream, null);
			}
#if USE_LOCK
			finally
			{
				lck.ExitReadLock();
			}
#endif
		}

		public static object DeserializeDeep(Stream stream)
		{
			if (!s_initialized)
				throw new InvalidOperationException("NetSerializer not initialized");

#if USE_LOCK
			lck.EnterReadLock();
			try
#endif
			{
				return Serializer.Deserialize(stream, new ObjectList());
			}
#if USE_LOCK
			finally
			{
				lck.ExitReadLock();
			}
#endif
		}

		internal static object Deserialize(Stream stream, ObjectList objList)
		{
			D("Deserializing");

			object o;
#if GENERATE_SWITCH
			s_deserializerSwitch(stream, out o, objList);
#else
			_DeserializerSwitch(stream, out o, objList);
#endif
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
				throw new NotSupportedException(String.Format("Type {0} is not marked as Serializable", type.FullName));

			if (type.ContainsGenericParameters)
				throw new NotSupportedException(String.Format("Type {0} contains generic parameters", type.FullName));

			typeSet.Add(type);

			if (type.IsArray)
			{
				CollectTypes(type.GetElementType(), typeSet);
			}
			else if (type.IsGenericType)
			{
				var args = type.GetGenericArguments();
				Type genType = type.GetGenericTypeDefinition();
				if (args.Length == 2)
				{
					if (genType == typeof(Tuple<,>))
					{
						CollectTypes(args[0], typeSet);
						CollectTypes(args[1], typeSet);
					}
					else if (genType == typeof(Dictionary<,>)
							|| genType == typeof(ConcurrentDictionary<,>)
							|| genType == typeof(SortedDictionary<,>)
							|| genType == typeof(SortedList<,>)
							)
					{
						Debug.Assert(args.Length == 2);
						var keyValueType = typeof(KeyValuePair<,>).MakeGenericType(args);
						CollectTypes(keyValueType, typeSet);
					}
				}
				else if (args.Length == 1 &&
						(genType == typeof(List<>)
					  || genType == typeof(ConcurrentQueue<>)
					  || genType == typeof(ConcurrentStack<>)
					  || genType == typeof(BlockingCollection<>)
					  || genType == typeof(Nullable<>)
					  || genType == typeof(Tuple<>)
					  || genType == typeof(HashSet<>)
					  || genType == typeof(LinkedList<>)
					  || genType == typeof(Queue<>)
					  || genType == typeof(SortedSet<>)
					  || genType == typeof(Stack<>)
					  || genType == typeof(ConcurrentBag<>)
					))
				{
					Debug.Assert(args.Length == 1);
					CollectTypes(args[0], typeSet);
				}
				else if ((args.Length == 3 && genType == typeof(Tuple<,,>))
						|| (args.Length == 4 && genType == typeof(Tuple<,,,>))
						|| (args.Length == 5 && genType == typeof(Tuple<,,,,>))
						|| (args.Length == 6 && genType == typeof(Tuple<,,,,,>))
						|| (args.Length == 7 && genType == typeof(Tuple<,,,,,,>))
						|| (args.Length == 8 && genType == typeof(Tuple<,,,,,,,>))
						)
				{
					foreach(var v in args)
						CollectTypes(v, typeSet);
				}

			}
			else
			{
				var fields = Helpers.GetFieldInfos(type);

				foreach (var field in fields)
					CollectTypes(field.FieldType, typeSet);
			}
		}


		static Type[] CollectTypes(Type[] rootTypes)
		{
			var typeSet = s_initialized ? new HashSet<Type>() : new HashSet<Type>(SerializationID.predefinedID.Keys);

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
#if GENERATE_SWITCH
			ushort caseID = (ushort)(s_Type_caseIDtypeIDMap == null ? 1 : s_Type_caseIDtypeIDMap.Count + 1);
#endif
			foreach (var type in types)
			{
				ushort typeID;
				if (!SerializationID.predefinedID.TryGetValue(type, out typeID))
				{
					typeID = SerializationID.userID++;
				}

				MethodInfo writer;
				MethodInfo reader;

				bool isStatic = Helpers.GetPrimitives(typeof(Primitives), type, out writer, out reader);

				if (type.IsPrimitive && isStatic == false)
					throw new InvalidOperationException(String.Format("Missing primitive read/write methods for {0}", type.FullName));

#if GENERATE_SWITCH
				var td = new TypeData(typeID, caseID++);
#else
				var td = new TypeData(typeID);
#endif
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
					if (typeof(System.Runtime.Serialization.ISerializable).IsAssignableFrom(type))
						throw new InvalidOperationException(String.Format("Cannot serialize {0}: ISerializable not supported", type.FullName));

					td.IsDynamic = true;
				}

				map[type] = td;
			}

			return map;
		}

		static Dictionary<Type, TypeData> GenerateDynamic(Type[] types, Dictionary<Type, TypeData> typeMap)
		{
			Dictionary<Type, TypeData> _map = GenerateTypeData(types);
			Dictionary<Type, TypeData> map = typeMap.Concat(_map).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

			var nonStaticTypes = map.Where(kvp => kvp.Value.IsDynamic).Select(kvp => kvp.Key);

			/* generate stubs */
			foreach (var type in nonStaticTypes)
			{
				var s_dm = SerializerCodegen.GenerateDynamicSerializerStub(type);
				var typeData = map[type];
				typeData.WriterMethodInfo = s_dm;
				typeData.WriterILGen = s_dm.GetILGenerator();

				var d_dm = DeserializerCodegen.GenerateDynamicDeserializerStub(type);
				typeData.ReaderMethodInfo = d_dm;
				typeData.ReaderILGen = d_dm.GetILGenerator();
			}

#if GENERATE_SWITCH
			var serializerSwitchMethod = new DynamicMethod("SerializerSwitch", null,
				new Type[] { typeof(Stream), typeof(object), typeof(ObjectList) },
				typeof(Serializer), true);
			serializerSwitchMethod.DefineParameter(1, ParameterAttributes.None, "stream");
			serializerSwitchMethod.DefineParameter(2, ParameterAttributes.None, "value");
			serializerSwitchMethod.DefineParameter(3, ParameterAttributes.None, "objList");
			var serializerSwitchMethodInfo = serializerSwitchMethod;

			var deserializerSwitchMethod = new DynamicMethod("DeserializerSwitch", null,
				new Type[] { typeof(Stream), typeof(object).MakeByRefType(), typeof(ObjectList) },
				typeof(Serializer), true);
			deserializerSwitchMethod.DefineParameter(1, ParameterAttributes.None, "stream");
			deserializerSwitchMethod.DefineParameter(2, ParameterAttributes.Out, "value");
			deserializerSwitchMethod.DefineParameter(3, ParameterAttributes.Out, "objList");
			var deserializerSwitchMethodInfo = deserializerSwitchMethod;

			var ctx = new CodeGenContext(map, serializerSwitchMethodInfo, deserializerSwitchMethodInfo);
#else
			var ctx = new CodeGenContext(map);
#endif

			/* generate bodies */
			foreach (var type in nonStaticTypes)
			{
				SerializerCodegen.GenerateSerializerBody(ctx, type, map[type].WriterILGen);
				DeserializerCodegen.GenerateDeserializerBody(ctx, type, map[type].ReaderILGen);
			}


#if GENERATE_SWITCH
			var ilGen = serializerSwitchMethod.GetILGenerator();
			SerializerCodegen.GenerateSerializerSwitch(ctx, ilGen, map);
			s_serializerSwitch = (SerializerSwitch)serializerSwitchMethod.CreateDelegate(typeof(SerializerSwitch));

			ilGen = deserializerSwitchMethod.GetILGenerator();
			DeserializerCodegen.GenerateDeserializerSwitch(ctx, ilGen, map);
			s_deserializerSwitch = (DeserializerSwitch)deserializerSwitchMethod.CreateDelegate(typeof(DeserializerSwitch));
#else
			foreach (var kvp in map)
			{
				kvp.Value.serializer = GetSerializationInvoker(null, kvp.Value.WriterMethodInfo, kvp.Key, (int)kvp.Value.TypeID);
				kvp.Value.deserializer = GetDeserializationInvoker(null, kvp.Value.ReaderMethodInfo, kvp.Key, (int)kvp.Value.TypeID);
			}
#endif
			
			return map;
		}

#if GENERATE_DEBUGGING_ASSEMBLY
		static void GenerateAssembly(Type[] types, Dictionary<Type, TypeData> typeMap)
		{
			Dictionary<Type, TypeData> _map = GenerateTypeData(types);
			Dictionary<Type, TypeData> map = typeMap.Concat(_map).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

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

#if GENERATE_SWITCH
			var serializerSwitchMethod = tb.DefineMethod("SerializerSwitch", MethodAttributes.Public | MethodAttributes.Static, null, new Type[] { typeof(Stream), typeof(object), typeof(ObjectList) });
			serializerSwitchMethod.DefineParameter(1, ParameterAttributes.None, "stream");
			serializerSwitchMethod.DefineParameter(2, ParameterAttributes.None, "value");
			serializerSwitchMethod.DefineParameter(3, ParameterAttributes.None, "objList");
			var serializerSwitchMethodInfo = serializerSwitchMethod;

			var deserializerSwitchMethod = tb.DefineMethod("DeserializerSwitch", MethodAttributes.Public | MethodAttributes.Static, null, new Type[] { typeof(Stream), typeof(object).MakeByRefType(), typeof(ObjectList) });
			deserializerSwitchMethod.DefineParameter(1, ParameterAttributes.None, "stream");
			deserializerSwitchMethod.DefineParameter(2, ParameterAttributes.Out, "value");
			deserializerSwitchMethod.DefineParameter(3, ParameterAttributes.None, "objList");
			var deserializerSwitchMethodInfo = deserializerSwitchMethod;

			var ctx = new CodeGenContext(map, serializerSwitchMethodInfo, deserializerSwitchMethodInfo);
#else
			var ctx = new CodeGenContext(map);
#endif

			/* generate bodies */
			foreach (var type in nonStaticTypes)
			{
				SerializerCodegen.GenerateSerializerBody(ctx, type, map[type].WriterILGen);
				DeserializerCodegen.GenerateDeserializerBody(ctx, type, map[type].ReaderILGen);
			}


#if GENERATE_SWITCH
			var ilGen = serializerSwitchMethod.GetILGenerator();
			SerializerCodegen.GenerateSerializerSwitch(ctx, ilGen, map);

			ilGen = deserializerSwitchMethod.GetILGenerator();
			DeserializerCodegen.GenerateDeserializerSwitch(ctx, ilGen, map);
#else
			foreach (var kvp in map)
			{
				GetSerializationInvoker(tb, kvp.Value.WriterMethodInfo, kvp.Key, (int)kvp.Value.TypeID);
				GetDeserializationInvoker(tb, kvp.Value.ReaderMethodInfo, kvp.Key, (int)kvp.Value.TypeID);
			}
#endif
			tb.CreateType();
			ab.Save("NetSerializerDebug.dll");
			SerializationID.userID = SerializationID.userIDstart;
		}
#endif

#if GENERATE_SWITCH
		/* called from the dynamically generated code */
		static uint GetTypeIDcaseID(object ob)
		{
			uint id;

			if (ob == null)
				return 0;

			if (s_Type_caseIDtypeIDMap.TryGetValue(ob.GetType(), out id) == false)
				throw new InvalidOperationException(String.Format("Unknown type {0}", ob.GetType().FullName));

			return id;
		}


		/* called from the dynamically generated code */
		static ushort GetCaseID(ushort typeID)
		{
			ushort id;

			if (typeID == 0)  //handle for null values
				return 0;

			if (s_caseID_typeIDMap.TryGetValue(typeID, out id) == false)
				throw new InvalidOperationException(String.Format("Unknown typeID = {0}", typeID));

			return id;
		}
#else

		public static void _SerializerSwitch(Stream stream, object value, ObjectList objList)
		{
			if (objList != null)
			{
				int index = objList.IndexOf(value);
				if (index != -1)
				{
					value = new ObjectRef(index);
				}
			}

			if (value == null)
			{
				Primitives.WritePrimitive(stream, (ushort)0, objList);
			}
			else
			{
				TypeData typeData;
				if (!s_typeMap.TryGetValue(value.GetType(), out typeData))
					throw new InvalidOperationException(String.Format("Unknown type = {0}", value.GetType().FullName));

				Primitives.WritePrimitive(stream, typeData.TypeID, objList);
				typeData.serializer(stream, value, objList);
			}

		}


		public static void _DeserializerSwitch(Stream stream, out object value, ObjectList objList)
		{
			ushort num;
			Primitives.ReadPrimitive(stream, out num, objList);

			if (num == 0)
			{
				value = null;
				return;
			}
			else if (num == 1)
			{
				ObjectRef ref2;
				Primitives.ReadPrimitive(stream, out ref2, objList);
				if (objList == null)
				{
					value = null;
					return;
				}
				value = objList.GetAt(ref2);
				return;

			}
			else
			{
				TypeData typeData;
				if (!s_typeID_TypeData.TryGetValue(num, out typeData))
					throw new InvalidOperationException(String.Format("Unknown typeId = {0}", num));

				typeData.deserializer(stream, out value, objList);
			}

		}
#endif	
	
	}
}
