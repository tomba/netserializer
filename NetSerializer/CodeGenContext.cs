/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
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
#if GENERATE_SWITCH
		public TypeData(ushort typeID, ushort caseID)
		{
			this.TypeID = typeID;
			this.CaseID = caseID;
		}
		public readonly ushort CaseID;
#else
		public TypeData(ushort typeID)
		{
			this.TypeID = typeID;
		}
#endif
		public readonly ushort TypeID;
		public bool IsDynamic;
		public MethodInfo WriterMethodInfo;
		public ILGenerator WriterILGen;
		public MethodInfo ReaderMethodInfo;
		public ILGenerator ReaderILGen;
#if !GENERATE_SWITCH
		public Serializer.SerializationInvokeHandler serializer;
		public Serializer.DeserializationInvokeHandler deserializer;
#endif
	}

	sealed class CodeGenContext
	{
		readonly Dictionary<Type, TypeData> m_typeMap;

#if GENERATE_SWITCH
		public CodeGenContext(Dictionary<Type, TypeData> typeMap, MethodInfo serializerSwitch, MethodInfo deserializerSwitch)
		{
			m_typeMap = typeMap;
			this.SerializerSwitchMethodInfo = serializerSwitch;
			this.DeserializerSwitchMethodInfo = deserializerSwitch;
		}

		public MethodInfo SerializerSwitchMethodInfo { get; private set; }
		public MethodInfo DeserializerSwitchMethodInfo { get; private set; }
#else
		public CodeGenContext(Dictionary<Type, TypeData> typeMap)
		{
			m_typeMap = typeMap;
		}
#endif

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
