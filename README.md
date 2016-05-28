# NetSerializer - A fast, simple serializer for .Net

NetSerializer is a simple and very fast serializer for .Net languages. It is
the fastest serializer I have found for my use cases.

The main pros of NetSerializer are:

- Excellent for network serialization
- Supports classes, structs, enums, interfaces, abstract classes
- No versioning or other extra information is serialized, only what is strictly needed
- No type IDs for primitive types, structs or sealed classes, so less data to be sent
- No dynamic type lookup for primitive types, structs or sealed classes, so
  deserialization is faster
- No extra attributes needed (like DataContract/Member), just add the standard
  [Serializable]
- Thread safe without locks
- The data is written to the stream and read from the stream directly, without
  the need for temporary buffers

The simpleness of NetSerializer has a drawback which must be considered by the
user: no versioning or other meta information is sent, which means that the
sender and the receiver have to have the same versions of the types being
serialized.

This means that it's a bad idea to save the serialized data for longer periods
of time, as a version upgrade could make the data non-deserializable.

For this reason I think the best (and perhaps only) use for NetSerializer is
for sending data over network, between a client and a server which have
verified version compatibility when the connection is made.

## Documentation

See [Documentation](Doc.md) page.

## Performance

See [Performance](Performance.md) page.

## Donations

If you feel NetSerializer is a great piece of software and the author deserves
some beer money, you can donate with bitcoin.

Bitcoin: 13YgwAye9Uz85xzjZXV4Son9uA6Kwy1ZAa

If you don't have bitcoins or don't want to donate money, you can also just
send an email telling me what you think of NetSerializer and how you use it.
