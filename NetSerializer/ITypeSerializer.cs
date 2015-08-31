/*
 * Copyright 2015 Tomi Valkeinen
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace NetSerializer
{
	public interface ITypeSerializer
	{
		/// <summary>
		/// Returns if this TypeSerializer handles the given type
		/// </summary>
		bool Handles(Type type);

		/// <summary>
		/// Return types that are needed to serialize the given type
		/// </summary>
		IEnumerable<Type> GetSubtypes(Type type);
	}

	public interface IStaticTypeSerializer : ITypeSerializer
	{
		/// <summary>
		/// Get static method used to serialize the given type
		/// </summary>
		MethodInfo GetStaticWriter(Type type);

		/// <summary>
		/// Get static method used to deserialize the given type
		/// </summary>
		MethodInfo GetStaticReader(Type type);
	}

	public interface IDynamicTypeSerializer : ITypeSerializer
	{
		/// <summary>
		/// Generate code to serialize the given type
		/// </summary>
		void GenerateWriterMethod(Serializer serializer, Type type, ILGenerator il);

		/// <summary>
		/// Generate code to deserialize the given type
		/// </summary>
		void GenerateReaderMethod(Serializer serializer, Type type, ILGenerator il);
	}
}
