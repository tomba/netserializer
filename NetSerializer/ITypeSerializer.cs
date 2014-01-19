using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace NetSerializer
{
	public interface ITypeSerializer
	{
		bool Handles(Type type);
		IEnumerable<Type> GetSubtypes(Type type);
	}

	public interface IStaticTypeSerializer : ITypeSerializer
	{
		void GetStaticMethods(Type type, out MethodInfo writer, out MethodInfo reader);
	}

	public interface IDynamicTypeSerializer : ITypeSerializer
	{
		void GenerateWriterMethod(Type type, CodeGenContext ctx, ILGenerator il);
		void GenerateReaderMethod(Type type, CodeGenContext ctx, ILGenerator il);
	}
}
