/*
 * Copyright 2015 Tomi Valkeinen
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NetSerializer
{
	sealed class EnumSerializer : IStaticTypeSerializer
	{
		public bool Handles(Type type)
		{
			return type.IsEnum;
		}

		public IEnumerable<Type> GetSubtypes(Type type)
		{
			var underlyingType = Enum.GetUnderlyingType(type);

			return new[] { underlyingType };
		}

		public MethodInfo GetStaticWriter(Type type)
		{
			Debug.Assert(type.IsEnum);

			var underlyingType = Enum.GetUnderlyingType(type);

			return Primitives.GetWritePrimitive(underlyingType);
		}

		public MethodInfo GetStaticReader(Type type)
		{
			Debug.Assert(type.IsEnum);

			var underlyingType = Enum.GetUnderlyingType(type);

			return Primitives.GetReaderPrimitive(underlyingType);
		}
	}
}
