using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnitTests.DTOs
{
    public class TypeWithNestedType
    {
        public string a { get; set; }

        public bool b { get; set; }

        public int? c { get; set; }

        public DateTime d { get; set; }

        public NestedType NestedType { get; set; }
    }
}
