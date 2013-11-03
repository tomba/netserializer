using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NetSerializer
{
    public interface INetserializerSerialisation
    {
        void INetSerializerSerialize(NetSerializerStreamWrapper stream);        


        //A Class implementing this interface also needs a special Constructor:
        //normaly a Constructor with Stream would be enough, but how can we check that is our, so it also gets the Netserializer object...
        //T(Netserializer n, Stream s)
    }
}
