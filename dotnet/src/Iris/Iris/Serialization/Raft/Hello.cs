// automatically generated by the FlatBuffers compiler, do not modify

namespace Iris.Serialization.Raft
{

using System;
using FlatBuffers;

public sealed class Hello : Table {
  public static Hello GetRootAsHello(ByteBuffer _bb) { return GetRootAsHello(_bb, new Hello()); }
  public static Hello GetRootAsHello(ByteBuffer _bb, Hello obj) { return (obj.__init(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
  public Hello __init(int _i, ByteBuffer _bb) { bb_pos = _i; bb = _bb; return this; }

  public string Name { get { int o = __offset(4); return o != 0 ? __string(o + bb_pos) : null; } }
  public ArraySegment<byte>? GetNameBytes() { return __vector_as_arraysegment(4); }

  public static Offset<Hello> CreateHello(FlatBufferBuilder builder,
      StringOffset NameOffset = default(StringOffset)) {
    builder.StartObject(1);
    Hello.AddName(builder, NameOffset);
    return Hello.EndHello(builder);
  }

  public static void StartHello(FlatBufferBuilder builder) { builder.StartObject(1); }
  public static void AddName(FlatBufferBuilder builder, StringOffset NameOffset) { builder.AddOffset(0, NameOffset.Value, 0); }
  public static Offset<Hello> EndHello(FlatBufferBuilder builder) {
    int o = builder.EndObject();
    return new Offset<Hello>(o);
  }
};


}
