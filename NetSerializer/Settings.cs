/*
 * Copyright 2016 Tomi Valkeinen
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;

namespace NetSerializer
{
	public class Settings
	{
		/// <summary>
		/// Gets called when an object is going to be deserialized.
		/// </summary>
		public Action<uint> BeforeDeserializingObjectWithTypeId;
        
		/// <summary>
		/// Gets called when an object is going to be serialized.
		/// </summary>
		public Action<Type> BeforeSerializingObjectOfType;
		
		/// <summary>
		/// Array of custom TypeSerializers
		/// </summary>
		public ITypeSerializer[] CustomTypeSerializers = new ITypeSerializer[0];

		/// <summary>
		/// Support IDeserializationCallback
		/// </summary>
		public bool SupportIDeserializationCallback = false;

		/// <summary>
		/// Support OnSerializing, OnSerialized, OnDeserializing, OnDeserialized attributes
		/// </summary>
		public bool SupportSerializationCallbacks = false;
	}
}
