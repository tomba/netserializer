using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;

namespace NetSerializer
{
    public class NetSerializerStreamWrapper
    {
        public Stream Stream { get; private set; }

        public NetSerializerStreamWrapper(Stream stream)
        {
            this.Stream = stream;
        }
    }
}
