/*
 * Copyright 2015 Tomi Valkeinen
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Reflection;
using System.Reflection.Emit;

namespace NetSerializer
{
	sealed class TypeData
	{
		public TypeData(Type type, uint typeID, ITypeSerializer typeSerializer)
		{
			this.Type = type;
			this.TypeID = typeID;
			this.TypeSerializer = typeSerializer;
		}

		public Type Type { get; private set; }
		public uint TypeID { get; private set; }

		public ITypeSerializer TypeSerializer { get; private set; }

		public MethodInfo WriterMethodInfo;
		public MethodInfo ReaderMethodInfo;

		public SerializeDelegate<object> WriterTrampolineDelegate;
		public Delegate WriterDirectDelegate;

		public DeserializeDelegate<object> ReaderTrampolineDelegate;
		public Delegate ReaderDirectDelegate;

		public bool WriterNeedsInstance
		{
			get
			{
				return this.WriterMethodInfo.GetParameters().Length == 3;
			}
		}

		public bool ReaderNeedsInstance
		{
			get
			{
				return this.ReaderMethodInfo.GetParameters().Length == 3;
			}
		}

		public bool CanCallDirect
		{
			get
			{
				// We can call the (De)serializer method directly for:
				// - Value types
				// - Array types
				// - Sealed types with static (De)serializer method, as the method will handle null
				// Other types go through the ObjectSerializer

				var type = this.Type;

				if (type.IsValueType || type.IsArray)
					return true;

				if (type.IsSealed && (this.TypeSerializer is IStaticTypeSerializer))
					return true;

				return false;
			}
		}
	}
}
