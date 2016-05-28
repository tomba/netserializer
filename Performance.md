# Performance

Below is a performance comparison between NetSerializer and protobuf-net.
Protobuf-net is a fast Protocol Buffers compatible serializer, which was the
best serializer I could find out there when I considered the serializer for
my use case.

The tests create an array of N items of particular type, created with random
data. The items are then serialized, and this is repeated M times for the same
dataset. NetSerializer can also serialize types directly, without writing any
meta information or boxing of value types. These tests are marked with
"(direct)".

The table lists the time it takes run the test, the number of GC collections
(per generation) that happened during the test, and the size of the
outputted serialized data (when available).

There are three tests:

- MemStream Serialize - serializes an array of objects to a memory stream.

- MemStream Deserialize - deserializes the stream created with MemStream
  Serialize test.

- NetTest - uses two threads, of which the first one serializes objects and
  sends them over a local socket, and the second one receive the data and
  deserialize the objects. Note that the size is not available for NetTest, as
  tracking the sent data is not trivial. However, the dataset is the same as
  with MemStream, an so is the size of the data.

The details of the tests can be found from the source code. The tests were run
on a 64bit Windows 10 laptop.

## 100 LargeStruct x 30000
|              |                       |  time (ms) |    GC coll.  |   size (B) |
|--------------|-----------------------|------------|--------------|------------|
|NetSerializer | MemStream Serialize   |        526 |   91   0   0 |       1859 |
|NetSerializer | MemStream Deserialize |        439 |   91   0   0 |            |
|NetSerializer | NetTest               |        751 |  184   0   0 |            |
|protobuf-net  | MemStream Serialize   |        987 |  381   0   0 |       2151 |
|protobuf-net  | MemStream Deserialize |       1586 |  183   0   0 |            |
|protobuf-net  | NetTest               |       2151 |  566   0   0 |            |


## 100 LargeStruct x 30000 (direct)
|              |                       |  time (ms) |    GC coll.  |   size (B) |
|--------------|-----------------------|------------|--------------|------------|
|NetSerializer | MemStream Serialize   |        463 |    0   0   0 |       1759 |
|NetSerializer | MemStream Deserialize |        413 |    0   0   0 |            |
|NetSerializer | NetTest               |        661 |    0   0   0 |            |

## 100 Guid x 50000
|              |                       |  time (ms) |    GC coll.  |   size (B) |
|--------------|-----------------------|------------|--------------|------------|
|NetSerializer | MemStream Serialize   |        733 |  101   0   0 |       1964 |
|NetSerializer | MemStream Deserialize |        509 |  101   0   0 |            |
|NetSerializer | NetTest               |        939 |  204   0   0 |            |
|protobuf-net  | MemStream Serialize   |       4487 |  890   0   0 |       2100 |
|protobuf-net  | MemStream Deserialize |       4503 |  279   0   0 |            |
|protobuf-net  | NetTest               |       5803 | 1178   0   0 |            |

## 100 Guid x 50000 (direct)
|              |                       |  time (ms) |    GC coll.  |   size (B) |
|--------------|-----------------------|------------|--------------|------------|
|NetSerializer | MemStream Serialize   |        642 |    0   0   0 |       1876 |
|NetSerializer | MemStream Deserialize |        499 |    0   0   0 |            |
|NetSerializer | NetTest               |        819 |    0   0   0 |            |

## 100 Int32 x 100000
|              |                       |  time (ms) |    GC coll.  |   size (B) |
|--------------|-----------------------|------------|--------------|------------|
|NetSerializer | MemStream Serialize   |        617 |  152   0   0 |        461 |
|NetSerializer | MemStream Deserialize |        426 |  152   0   0 |            |
|NetSerializer | NetTest               |        742 |  306   0   0 |            |
|protobuf-net  | MemStream Serialize   |       8778 | 1527   0   0 |        648 |
|protobuf-net  | MemStream Deserialize |       9078 |  560  14   0 |            |
|protobuf-net  | NetTest               |      11416 | 2103   1   0 |            |

## 100 Int32 x 100000 (direct)
|              |                       |  time (ms) |    GC coll.  |   size (B) |
|--------------|-----------------------|------------|--------------|------------|
|NetSerializer | MemStream Serialize   |        468 |    0   0   0 |        361 |
|NetSerializer | MemStream Deserialize |        428 |    0   0   0 |            |
|NetSerializer | NetTest               |        580 |    0   0   0 |            |

## 100 U8Message x 100000
|              |                       |  time (ms) |    GC coll.  |   size (B) |
|--------------|-----------------------|------------|--------------|------------|
|NetSerializer | MemStream Serialize   |        389 |    0   0   0 |        200 |
|NetSerializer | MemStream Deserialize |        991 |  152   0   0 |            |
|NetSerializer | NetTest               |       1137 |  152   0   0 |            |
|protobuf-net  | MemStream Serialize   |       3007 |  966   0   0 |        527 |
|protobuf-net  | MemStream Deserialize |       5745 |  152   0   0 |            |
|protobuf-net  | NetTest               |       6925 | 1122   0   0 |            |

## 100 U8Message x 100000 (direct)
|              |                       |  time (ms) |    GC coll.  |   size (B) |
|--------------|-----------------------|------------|--------------|------------|
|NetSerializer | MemStream Serialize   |        281 |    0   0   0 |        100 |
|NetSerializer | MemStream Deserialize |       1047 |  152   0   0 |            |
|NetSerializer | NetTest               |       1156 |  152   0   0 |            |

## 100 S16Message x 100000
|              |                       |  time (ms) |    GC coll.  |   size (B) |
|--------------|-----------------------|------------|--------------|------------|
|NetSerializer | MemStream Serialize   |        512 |    0   0   0 |        341 |
|NetSerializer | MemStream Deserialize |       1110 |  152   0   0 |            |
|NetSerializer | NetTest               |       1347 |  152   0   0 |            |
|protobuf-net  | MemStream Serialize   |       3204 |  966   0   0 |        844 |
|protobuf-net  | MemStream Deserialize |       5986 |  152   0   0 |            |
|protobuf-net  | NetTest               |       7325 | 1122   0   0 |            |

## 100 S32Message x 100000
|              |                       |  time (ms) |    GC coll.  |   size (B) |
|--------------|-----------------------|------------|--------------|------------|
|NetSerializer | MemStream Serialize   |        593 |    0   0   0 |        461 |
|NetSerializer | MemStream Deserialize |       1207 |  152   0   0 |            |
|NetSerializer | NetTest               |       1475 |  152   0   0 |            |
|protobuf-net  | MemStream Serialize   |       3371 |  966   0   0 |        828 |
|protobuf-net  | MemStream Deserialize |       6066 |  152   0   0 |            |
|protobuf-net  | NetTest               |       7535 | 1120   0   0 |            |

## 100 S64Message x 100000
|              |                       |  time (ms) |    GC coll.  |   size (B) |
|--------------|-----------------------|------------|--------------|------------|
|NetSerializer | MemStream Serialize   |        699 |    0   0   0 |        542 |
|NetSerializer | MemStream Deserialize |       1313 |  152   0   0 |            |
|NetSerializer | NetTest               |       1603 |  152   0   0 |            |
|protobuf-net  | MemStream Serialize   |       3512 |  966   0   0 |        809 |
|protobuf-net  | MemStream Deserialize |       6268 |  152   0   0 |            |
|protobuf-net  | NetTest               |       7672 | 1124   0   0 |            |

## 100 DecimalMessage x 50000
|              |                       |  time (ms) |    GC coll.  |   size (B) |
|--------------|-----------------------|------------|--------------|------------|
|NetSerializer | MemStream Serialize   |        648 |  127   0   0 |       1360 |
|NetSerializer | MemStream Deserialize |        934 |  101   0   0 |            |
|NetSerializer | NetTest               |       1292 |  229   0   0 |            |
|protobuf-net  | MemStream Serialize   |       2716 |  610   0   0 |       2022 |
|protobuf-net  | MemStream Deserialize |       4220 |  101   0   0 |            |
|protobuf-net  | NetTest               |       5052 |  713   0   0 |            |

## 100 NullableDecimalMessage x 100000
|              |                       |  time (ms) |    GC coll.  |   size (B) |
|--------------|-----------------------|------------|--------------|------------|
|NetSerializer | MemStream Serialize   |        441 |    7   0   0 |        238 |
|NetSerializer | MemStream Deserialize |       1156 |  254   0   0 |            |
|NetSerializer | NetTest               |       1340 |  262   0   0 |            |
|protobuf-net  | MemStream Serialize   |       4033 |  973   0   0 |        353 |
|protobuf-net  | MemStream Deserialize |       6727 |  254   0   0 |            |
|protobuf-net  | NetTest               |       8203 | 1233   0   0 |            |

## 100 PrimitivesMessage x 10000
|              |                       |  time (ms) |    GC coll.  |   size (B) |
|--------------|-----------------------|------------|--------------|------------|
|NetSerializer | MemStream Serialize   |        703 |   25   0   0 |       5286 |
|NetSerializer | MemStream Deserialize |        748 |   76   0   0 |            |
|NetSerializer | NetTest               |       1010 |  102   0   0 |            |
|protobuf-net  | MemStream Serialize   |        725 |   96   0   0 |       7290 |
|protobuf-net  | MemStream Deserialize |       1089 |   50   0   0 |            |
|protobuf-net  | NetTest               |       1348 |  148   0   0 |            |

## 10 DictionaryMessage x 1000
|              |                       |  time (ms) |    GC coll.  |   size (B) |
|--------------|-----------------------|------------|--------------|------------|
|NetSerializer | MemStream Serialize   |       1310 |   75   0   0 |      86187 |
|NetSerializer | MemStream Deserialize |       1955 |  109  54   0 |            |
|NetSerializer | NetTest               |       2521 |  135  67   0 |            |
|protobuf-net  | MemStream Serialize   |       1713 |  413   0   0 |     142035 |
|protobuf-net  | MemStream Deserialize |       3576 |  233 116   0 |            |
|protobuf-net  | NetTest               |       4693 |  494 247  11 |            |

## 100 ComplexMessage x 10000
|              |                       |  time (ms) |    GC coll.  |   size (B) |
|--------------|-----------------------|------------|--------------|------------|
|NetSerializer | MemStream Serialize   |        410 |    0   0   0 |       2838 |
|NetSerializer | MemStream Deserialize |        608 |  100   0   0 |            |
|NetSerializer | NetTest               |        812 |  100   0   0 |            |
|protobuf-net  | MemStream Serialize   |        838 |   96   0   0 |       5087 |
|protobuf-net  | MemStream Deserialize |       1902 |  100   0   0 |            |
|protobuf-net  | NetTest               |       2195 |  198   0   0 |            |

## 100 StringMessage x 20000
|              |                       |  time (ms) |    GC coll.  |   size (B) |
|--------------|-----------------------|------------|--------------|------------|
|NetSerializer | MemStream Serialize   |        447 |    0   0   0 |       4886 |
|NetSerializer | MemStream Deserialize |        655 |  182   0   0 |            |
|NetSerializer | NetTest               |        903 |  182   0   0 |            |
|protobuf-net  | MemStream Serialize   |       1043 |  193   0   0 |       5085 |
|protobuf-net  | MemStream Deserialize |       3973 |  182   0   0 |            |
|protobuf-net  | NetTest               |       2428 |  378   0   0 |            |

## 100 StructMessage x 20000
|              |                       |  time (ms) |    GC coll.  |   size (B) |
|--------------|-----------------------|------------|--------------|------------|
|NetSerializer | MemStream Serialize   |        528 |    0   0   0 |       2455 |
|NetSerializer | MemStream Deserialize |        681 |  117   0   0 |            |
|NetSerializer | NetTest               |        909 |  117   0   0 |            |
|protobuf-net  | MemStream Serialize   |       1369 |  274   0   0 |       3622 |
|protobuf-net  | MemStream Deserialize |       4297 |  280   0   0 |            |
|protobuf-net  | NetTest               |       2752 |  557   0   0 |            |

## 100 BoxedPrimitivesMessage x 20000
|              |                       |  time (ms) |    GC coll.  |   size (B) |
|--------------|-----------------------|------------|--------------|------------|
|NetSerializer | MemStream Serialize   |        708 |    0   0   0 |       1723 |
|NetSerializer | MemStream Deserialize |        570 |  223   0   0 |            |
|NetSerializer | NetTest               |        862 |  223   0   0 |            |

## 10000 ByteArrayMessage x 1
|              |                       |  time (ms) |    GC coll.  |   size (B) |
|--------------|-----------------------|------------|--------------|------------|
|NetSerializer | MemStream Serialize   |        736 |    1   1   1 |  498085311 |
|NetSerializer | MemStream Deserialize |        325 |   58  29   1 |            |
|NetSerializer | NetTest               |        889 |   58  27   0 |            |
|protobuf-net  | MemStream Serialize   |       1329 |  341   6   3 |  498151945 |
|protobuf-net  | MemStream Deserialize |        418 |   58  29   1 |            |
|protobuf-net  | NetTest               |       1696 |  172  40   2 |            |

## 1000 IntArrayMessage x 1
|              |                       |  time (ms) |    GC coll.  |   size (B) |
|--------------|-----------------------|------------|--------------|------------|
|NetSerializer | MemStream Serialize   |       1533 |    0   0   0 |  177278871 |
|NetSerializer | MemStream Deserialize |       1147 |    2   1   0 |            |
|NetSerializer | NetTest               |       1821 |    3   1   0 |            |
|protobuf-net  | MemStream Serialize   |       1849 |   79   4   2 |  283510795 |
|protobuf-net  | MemStream Deserialize |       1720 |   28   3   0 |            |
|protobuf-net  | NetTest               |       2620 |   88   5   2 |            |

## 10 TriDimArrayCustomSerializersMessage x 100
|              |                       |  time (ms) |    GC coll.  |   size (B) |
|--------------|-----------------------|------------|--------------|------------|
|NetSerializer | MemStream Serialize   |       1246 |    0   0   0 |    1601277 |
|NetSerializer | MemStream Deserialize |       1175 |   30  27  25 |            |
|NetSerializer | NetTest               |       1844 |   43  41  38 |            |

As can be seen from the tests, NetSerializer is clearly faster and has smaller
memory footprint in about all of the cases. For example, the tests with
ComplexMessages show NetSerializer's MemStream Serialize cause zero garbage
collections, even though more than 20MB of data is being serialized.
