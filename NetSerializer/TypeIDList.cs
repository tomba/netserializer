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
using System.Linq;
using System.Threading;

namespace NetSerializer
{
	/// <summary>
	/// Threadsafe TypeID -> TypeData list, which supports lockless reading.
	/// </summary>
	class TypeIDList
	{
		TypeData[] m_array;
		object m_writeLock = new object();

		const int InitialLength = 256;

		public TypeIDList()
		{
			m_array = new TypeData[InitialLength];
		}

		public bool ContainsTypeID(uint typeID)
		{
			return typeID < m_array.Length && m_array[typeID] != null;
		}

		public TypeData this[uint idx]
		{
			get
			{
				return m_array[idx];
			}

			set
			{
				lock (m_writeLock)
				{
					if (idx >= m_array.Length)
					{
						var newArray = new TypeData[NextPowOf2(idx + 1)];
						Array.Copy(m_array, newArray, m_array.Length);
						m_array = newArray;
					}

					Debug.Assert(m_array[idx] == null);

					m_array[idx] = value;
				}
			}
		}

		uint NextPowOf2(uint v)
		{
			v--;
			v |= v >> 1;
			v |= v >> 2;
			v |= v >> 4;
			v |= v >> 8;
			v |= v >> 16;
			v++;
			return v;
		}
	}
}
