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
using System.Reflection;

namespace NetSerializer
{
	public class VersionSerializer : IStaticTypeSerializer
	{
		public virtual bool Handles(Type type)
		{
			return type == typeof(Version);
		}

		public virtual IEnumerable<Type> GetSubtypes(Type type)
		{
			yield break;
		}

		public virtual MethodInfo GetStaticWriter(Type type)
		{
			return typeof(VersionSerializer).GetMethod(
				"WritePrimitive",
				BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.ExactBinding,
				null,
				new Type[] { typeof(Stream), type },
				null);
		}

		public virtual MethodInfo GetStaticReader(Type type)
		{
			return typeof(VersionSerializer).GetMethod("ReadPrimitive",
				BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.ExactBinding, null,
				new Type[] { typeof(Stream), type.MakeByRefType() }, null);
		}

		static void WritePrimitive(Stream stream, Version value)
		{
			if (value == null)
			{
				Primitives.WritePrimitive(stream, (uint)0);
				return;
			}

			Primitives.WritePrimitive(stream, (uint)1);
			Primitives.WritePrimitive(stream, value.Major);
			Primitives.WritePrimitive(stream, value.Minor);
			Primitives.WritePrimitive(stream, value.Revision);
			Primitives.WritePrimitive(stream, value.Build);
		}

		static void ReadPrimitive(Stream stream, out Version value)
		{
			uint l1;
			int l2, l3, l4, l5;

			Primitives.ReadPrimitive(stream, out l1);

			if (l1 == 0)
			{
				value = null;
				return;
			}

			Primitives.ReadPrimitive(stream, out l2);
			Primitives.ReadPrimitive(stream, out l3);
			Primitives.ReadPrimitive(stream, out l4);
			Primitives.ReadPrimitive(stream, out l5);
			value = new Version(l2, l3, l4, l5);
		}
	}
}
