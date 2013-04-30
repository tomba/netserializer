/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetSerializer
{
	[Serializable]
//	public sealed class ObjectRef
	public struct ObjectRef
	{
		public int obj_ref;

		public ObjectRef(int obj_ref) 
		{
			this.obj_ref = obj_ref;
		}
	}
	
	
	public class ObjectList
	{
		private int size = 0;
		private object[] elementData = new object[8];

		public void Add(object o)
		{
			if (elementData.Length == size)
			{
				//grow array if necessary
				Array.Resize(ref elementData, elementData.Length * 2);
				//??elementData = Arrays.copyOf(elementData, elementData.Length * 2);
			}
			elementData[size] = o;
			size++;
		}

		public int Count
		{
			get
			{
				return size;
			}
		}

		public int IndexOf(Object obj)
		{
			if (obj == null)
				return -1;

			Type type = obj.GetType();
			if (!type.IsClass)
					return -1;

			for (int i = 0; i < size; i++)
			{
				if (Object.ReferenceEquals(obj, (Object)elementData[i]))
					return i;
			}
			return -1;
		}

		public object GetAt(int index)
		{
			if (index >= size)
				throw new ArgumentOutOfRangeException("index");
			return elementData[index];
		}

		public object GetAt(ObjectRef oref)
		{
			if (oref.obj_ref >= size)
				throw new ArgumentOutOfRangeException("index");
			return elementData[oref.obj_ref];
		}

	}
}
