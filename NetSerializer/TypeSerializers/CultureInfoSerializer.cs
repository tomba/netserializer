/*
 * Copyright 2015 Tomi Valkeinen
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace NetSerializer
{
	public class CultureInfoSerializer : IStaticTypeSerializer
	{
		public virtual bool Handles(Type type)
		{
			return type == typeof(CultureInfo);
		}

		public virtual IEnumerable<Type> GetSubtypes(Type type)
		{
			yield break;
		}

		public virtual MethodInfo GetStaticWriter(Type type)
		{
			return typeof(CultureInfoSerializer).GetMethod(
				"WritePrimitive",
				BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.ExactBinding,
				null,
				new Type[] { typeof(Stream), type },
				null);
		}

		public virtual MethodInfo GetStaticReader(Type type)
		{
			return typeof(CultureInfoSerializer).GetMethod("ReadPrimitive",
				BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.ExactBinding, null,
				new Type[] { typeof(Stream), type.MakeByRefType() }, null);
		}

		static void WritePrimitive(Stream stream, CultureInfo value)
		{
			if (value == null)
			{
				Primitives.WritePrimitive(stream, (uint)0);
				return;
			}

			Primitives.WritePrimitive(stream, (uint)(value.UseUserOverride ? 2 : 1));
			Primitives.WritePrimitive(stream, value.LCID); 
		}

		static void ReadPrimitive(Stream stream, out CultureInfo value)
		{
			uint l1;
			int l2;

			Primitives.ReadPrimitive(stream, out l1);

			if (l1 == 0)
			{
				value = null;
				return;
			}

			Primitives.ReadPrimitive(stream, out l2);
			value = new CultureInfo(l2, l1 == 2);
		}
	}
}