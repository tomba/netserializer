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

namespace NetSerializer
{
	sealed class ObjectSerializer : IStaticTypeSerializer
	{
		public bool Handles(Type type)
		{
			return type == typeof(object);
		}

		public IEnumerable<Type> GetSubtypes(Type type)
		{
			return new Type[0];
		}

		public MethodInfo GetStaticWriter(Type type)
		{
			return typeof(ObjectSerializer).GetMethod("Serialize", BindingFlags.Static | BindingFlags.Public);
		}

		public MethodInfo GetStaticReader(Type type)
		{
			return typeof(ObjectSerializer).GetMethod("Deserialize", BindingFlags.Static | BindingFlags.Public);
		}

		public static void Serialize(Serializer serializer, Stream stream, object ob)
		{
			if (ob == null)
			{
				Primitives.WritePrimitive(stream, (uint)0);
				return;
			}

			var type = ob.GetType();

			SerializeDelegate<object> del;

			uint id = serializer.GetTypeIdAndSerializer(type, out del);

			Primitives.WritePrimitive(stream, id);

			del(serializer, stream, ob);
		}

		public static void Deserialize(Serializer serializer, Stream stream, out object ob)
		{
			uint id;

			Primitives.ReadPrimitive(stream, out id);

			if (id == 0)
			{
				ob = null;
				return;
			}

			var del = serializer.GetDeserializeTrampolineFromId(id);
			del(serializer, stream, out ob);
		}
	}
}
