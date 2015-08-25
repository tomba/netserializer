/*
 * Copyright 2015 Tomi Valkeinen
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NetSerializer
{
	class ObjectSerializer : IStaticTypeSerializer
	{
		public bool Handles(Type type)
		{
			return type == typeof(object);
		}

		public IEnumerable<Type> GetSubtypes(Type type)
		{
			return new Type[0];
		}

		public void GetStaticMethods(Type type, out MethodInfo writer, out MethodInfo reader)
		{
			writer = typeof(Serializer).GetMethod("Serialize", BindingFlags.Static | BindingFlags.NonPublic);
			reader = typeof(Serializer).GetMethod("Deserialize", BindingFlags.Static | BindingFlags.NonPublic);
		}
	}
}
