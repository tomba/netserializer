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
using System.Text;

namespace NetSerializer
{
	sealed class PrimitivesSerializer : IStaticTypeSerializer
	{
		static Type[] s_primitives = new Type[] {
				typeof(bool),
				typeof(byte), typeof(sbyte),
				typeof(char),
				typeof(ushort), typeof(short),
				typeof(uint), typeof(int),
				typeof(ulong), typeof(long),
				typeof(float), typeof(double),
				typeof(string),
				typeof(DateTime),
				typeof(byte[]),
				typeof(Decimal),
			};

		public bool Handles(Type type)
		{
			return s_primitives.Contains(type);
		}

		public IEnumerable<Type> GetSubtypes(Type type)
		{
			return new Type[0];
		}

		public MethodInfo GetStaticWriter(Type type)
		{
			return Primitives.GetWritePrimitive(type);
		}

		public MethodInfo GetStaticReader(Type type)
		{
			return Primitives.GetReaderPrimitive(type);
		}

		public static IEnumerable<Type> GetSupportedTypes()
		{
			return s_primitives;
		}
	}
}
