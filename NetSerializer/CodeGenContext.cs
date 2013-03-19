using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace NetSerializer
{
	sealed class TypeData
	{
		public TypeData(ushort typeID)
		{
			this.TypeID = typeID;
		}

		public readonly ushort TypeID;
		public bool IsDynamic;
		public MethodInfo WriterMethodInfo;
		public ILGenerator WriterILGen;
		public MethodInfo ReaderMethodInfo;
		public ILGenerator ReaderILGen;
	}

	sealed class CodeGenContext
	{
		readonly Dictionary<Type, TypeData> m_typeMap;

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
}
