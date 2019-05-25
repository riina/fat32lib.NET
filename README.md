# fat32lib.NET

#### .NET port of fat32lib

This library (fat32lib.NET) is a port of the library fat32lib obtained from

https://android.googlesource.com/platform/external/fat32lib/

which itself is a modified version of the library

https://github.com/waldheinz/fat32-lib

which is, according to its README, `originally based on the FAT file system driver included in the JNode operating system`

## Information

Class structure (under the root namespace `FAT32Lib`) has been preserved.

C# naming conventions have been adhered to (Pascal case for methods, prefixing interface names with "I").

Usage of some classes have been converted to .NET (near) counterparts. E.g. `ByteBuffer` > `MemoryStream`, `IllegalStateException` > `InvalidOperationException`.

`FatType` was changed to a class with static readonly objects (derived from `FatType`) `BASE_FAT12`, `BASE_FAT16`, and `BASE_FAT32`.

Javadoc comments have been converted to XML-style C# documentation.

Normal comments have been preserved.

A sample basic extraction program can be found at `src/FAT32Lib/FAT32Lib.Test`
