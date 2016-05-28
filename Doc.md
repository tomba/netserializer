# NetSerializer - A fast, simple serializer for .Net

## Supported Types

NetSerializer supports serializing the following types:

- All primitive types (Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32,
  Int64, UInt64, IntPtr, UIntPtr, Char, Double, and Single)
- Strings
- Enums
- Single dimensional arrays
- Structs and classes marked with `Serializable` Attribute. Note, however,
  that classes implementing `ISerializable` are _not_ supported, except for
  the cases below.
- Dictionary<K,V>
- DateTime
- Types with user defined [custom serializers](#custom-serializers)

## Caveats

- `ISerializable` is not supported, so not all classes marked with
  `[Serializable]` are supported.
- Versioning not supported. Any change in the types may result in incompatible
  data.
- The above also implies that if client-server architecture is used, both
  client and server must have the exact same types.
- With client-server, both client and server must set up the NetSerializer in
  the same way, providing the same types (and type IDs).

## Usage

The types to be serialized need to be marked with the standard
`[Serializable]`. You can also use `[NonSerialized]` for fields you don't
want to serialize. Nothing else needs to be done for the types to be
serialized.

Then you need to initialize NetSerializer by giving it a list of types you
will be serializing. NetSerializer will scan through the given types, and
recursively add all the types used by the given types, and assign type IDs and
create (de)serialization code.

All the types you will be serializing should be reachable from this list of
types. If your types contain fields of base class types or interfaces,
NetSerializer does not know what concrete types you will be serializing via
those fields. You need to collect those types somehow and provide them to
NetSerializer when initializing (See [Collecting Types](#collecting-types)).

It is important that the list of types provided to NetSerializer contains
exactly the same types on all NetSerializer instances (e.g. client and
server), as the type IDs are assigned according to the list.

There is also a possibility to add types later via [type maps](#type-maps),
which is somewhat complex and only recommended if absolutely needed.

## Example

Initialization:

```
var types = YourCollectTypesMethod();
var ser = new Serializer(types);
```

Serializing:

`ser.Serialize(stream, ob);`

Deserializing:

`var ob = (YourType)ser.Deserialize(stream);`

## Collecting Types

There are many ways to collect the types for NetSerializer. Here is one
example.

Let's say we have a bunch of message types we want to serialize. All those
message types inherit `MessageBase`.

With this helper:

```
IEnumerable<Type> GetSubclasses(Type type)
{
	return type.Assembly.GetTypes().Where(t => t.IsSubclassOf(type));
}
```

we can use the following code to collect all the message types:

```
var messageTypes = GetSubclasses(typeof(MessageBase));
var ser = new Serializer(messageTypes);
```

## Direct Serialization

NetSerializer supports so called direct serialization via the following
methods:

```
SerializeDirect<T>()
DeserializeDirect<T>()
```

These methods can be used when the serializer _and_ the deserializer know the
exact type of the object. This has two benefits:

- No type ID will be written, saving a byte of two
- Value types are not boxed, reducing the amount of garbage generated

It is quite rare to need direct serialization, but if you need to serialize
lots of value types, direct serialization is faster than normal serialization.

## Type Maps

When NetSerializer serializes data, it uses a number (type ID) to represent
the type in question, so that the deserializer knows what type to instantiate.
Normally these type IDs are automatically reserved when initializing
NetSerializer, and NetSerializer stores these type IDs in a type map (Type -> TypeID map).

For advanced use cases it is also possible for the user to manually assign
type IDs for types in a type map. This type map can be provided to
NetSerializer when initializing NetSerializer, or later with
`AddTypes(Dictionary<Type, uint> typeMap)`.

It is critical to keep in mind that the exact same type map must be (somehow)
provided to all instances of NetSerializer (e.g. client and server). How this
is communicated between the different instances depends on the use case and is
out of scope of this document.

Alternatively types can be added at runtime with `AddTypes(IEnumerable<Type>
rootTypes)`, which assigns type IDs automatically and returns a type map of
the added types, which must then be delivered to other instances.

## Settings

You can provide `NetSerializer.Settings` instance when initializing
netserializer to enable support for `IDeserializationCallback` or
`OnSerializing`, `OnSerialized`, `OnDeserializing`, `OnDeserialized`
attributes.

## Custom Serializers

NetSerializer also supports creating custom serializers. Custom serializers can
be used to serialize types not directly supported by NetSerializer.

However, at the moment custom serializers are somewhat limited in
functionality, and you may not be able to create efficient custom serializers.
Issue #39 tracks this.

## Known Issues

- Mono issue with CultureInfo: [Issue #38](https://github.com/tomba/netserializer/pull/38),
  [Mono pull req](https://github.com/mono/mono/pull/2942)
- String serialization on CentOS 6:
  [Issue #55](https://github.com/tomba/netserializer/pull/55)
