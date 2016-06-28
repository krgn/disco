// automatically generated by the FlatBuffers compiler, do not modify

namespace Iris.Serialization.Raft
{

using System;
using FlatBuffers;

public sealed class StateMachineFB : Table {
  public static StateMachineFB GetRootAsStateMachineFB(ByteBuffer _bb) { return GetRootAsStateMachineFB(_bb, new StateMachineFB()); }
  public static StateMachineFB GetRootAsStateMachineFB(ByteBuffer _bb, StateMachineFB obj) { return (obj.__init(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
  public StateMachineFB __init(int _i, ByteBuffer _bb) { bb_pos = _i; bb = _bb; return this; }

  public StateMachineTypeFB Type { get { int o = __offset(4); return o != 0 ? (StateMachineTypeFB)bb.GetUshort(o + bb_pos) : StateMachineTypeFB.OpenProjectTypeFB; } }
  public string Command { get { int o = __offset(6); return o != 0 ? __string(o + bb_pos) : null; } }
  public ArraySegment<byte>? GetCommandBytes() { return __vector_as_arraysegment(6); }

  public static Offset<StateMachineFB> CreateStateMachineFB(FlatBufferBuilder builder,
      StateMachineTypeFB Type = StateMachineTypeFB.OpenProjectTypeFB,
      StringOffset CommandOffset = default(StringOffset)) {
    builder.StartObject(2);
    StateMachineFB.AddCommand(builder, CommandOffset);
    StateMachineFB.AddType(builder, Type);
    return StateMachineFB.EndStateMachineFB(builder);
  }

  public static void StartStateMachineFB(FlatBufferBuilder builder) { builder.StartObject(2); }
  public static void AddType(FlatBufferBuilder builder, StateMachineTypeFB Type) { builder.AddUshort(0, (ushort)Type, 0); }
  public static void AddCommand(FlatBufferBuilder builder, StringOffset CommandOffset) { builder.AddOffset(1, CommandOffset.Value, 0); }
  public static Offset<StateMachineFB> EndStateMachineFB(FlatBufferBuilder builder) {
    int o = builder.EndObject();
    return new Offset<StateMachineFB>(o);
  }
};


}
