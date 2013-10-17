using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetSerializer
{
    public class TypeNumberAttribute : Attribute
    {
        public TypeNumberAttribute(Int16 number)
        {
            if (number < 255)
            {
                throw new ArgumentOutOfRangeException("number", "The Number for Custom types has to be bigger than 255");
            }

            Number = number;
        }

        public Int16 Number { get; private set; }
    }
}
