// automatically generated by the FlatBuffers compiler, do not modify

namespace Iris.Serialization
{

using System;
using FlatBuffers;

public sealed class NoNodeFB : Table {
  public static NoNodeFB GetRootAsNoNodeFB(ByteBuffer _bb) { return GetRootAsNoNodeFB(_bb, new NoNodeFB()); }
  public static NoNodeFB GetRootAsNoNodeFB(ByteBuffer _bb, NoNodeFB obj) { return (obj.__init(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
  public NoNodeFB __init(int _i, ByteBuffer _bb) { bb_pos = _i; bb = _bb; return this; }


  public static void StartNoNodeFB(FlatBufferBuilder builder) { builder.StartObject(0); }
  public static Offset<NoNodeFB> EndNoNodeFB(FlatBufferBuilder builder) {
    int o = builder.EndObject();
    return new Offset<NoNodeFB>(o);
  }
};


}
