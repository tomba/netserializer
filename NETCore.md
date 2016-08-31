# .NET Core port notes of NetSerialization
By Joannes Vermorel, August 2016

## The `DNXCORE50` compile flag

.NET Core is not fully backward compatible with .NET 4.x. Thus,
the compile flage `DNXCORE50` has been introduce in the solution
to adjust the code to make it compatible with .NET Core.

## `ISerializable` is no more in .NET Core

In .NET Core, the `ISerializable` interface is no more. Actually,
the need to flag classes with `ISerializable` was questionnable
even in .NET 4.x. Thus, `ISerializable` behavior can now be avoided
through `Settings.SupportISerializable` in .NET 4.x, and while it is
completely ignored under .NET Core.

## `Encoder.GetByteCount()` and `Encoder.Convert()`

The pointer-based overload of `Encoder.GetByteCount()` and `Encoder.Convert()`
have not been ported yet to .NET Core, but those overloads are already planned
for the version 1.2. For the time being, the .NET Core behavior of NetSerializer
relies on slow non-pointer overload. Once the pointer overloads become available
in .NET Core, this change should be reverted. The .NET 4.x version is still using
the fast overloads.

## Debugging Assembly and `AppDomain`

The `AppDomain` are no more in .NET Core (for now), so the compile flag
`GENERATE_DEBUGGING_ASSEMBLY` does not work in .NET Core.
