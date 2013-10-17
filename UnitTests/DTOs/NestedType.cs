using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetSerializer;

namespace UnitTests.DTOs
{
    [TypeNumber(334)]
    public class NestedType
    {
        public string a { get; set; }

        public bool b { get; set; }

        public int? c { get; set; }

        public DateTime d { get; set; }
    }
}
