// automatically generated by the FlatBuffers compiler, do not modify

namespace Iris.Serialization.Raft
{

using System;
using FlatBuffers;

public sealed class AppendEntriesFB : Table {
  public static AppendEntriesFB GetRootAsAppendEntriesFB(ByteBuffer _bb) { return GetRootAsAppendEntriesFB(_bb, new AppendEntriesFB()); }
  public static AppendEntriesFB GetRootAsAppendEntriesFB(ByteBuffer _bb, AppendEntriesFB obj) { return (obj.__init(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
  public AppendEntriesFB __init(int _i, ByteBuffer _bb) { bb_pos = _i; bb = _bb; return this; }

  public ulong Term { get { int o = __offset(4); return o != 0 ? bb.GetUlong(o + bb_pos) : (ulong)0; } }
  public ulong PrevLogIdx { get { int o = __offset(6); return o != 0 ? bb.GetUlong(o + bb_pos) : (ulong)0; } }
  public ulong PrevLogTerm { get { int o = __offset(8); return o != 0 ? bb.GetUlong(o + bb_pos) : (ulong)0; } }
  public ulong LeaderCommit { get { int o = __offset(10); return o != 0 ? bb.GetUlong(o + bb_pos) : (ulong)0; } }
  public LogFB GetEntries(int j) { return GetEntries(new LogFB(), j); }
  public LogFB GetEntries(LogFB obj, int j) { int o = __offset(12); return o != 0 ? obj.__init(__indirect(__vector(o) + j * 4), bb) : null; }
  public int EntriesLength { get { int o = __offset(12); return o != 0 ? __vector_len(o) : 0; } }

  public static Offset<AppendEntriesFB> CreateAppendEntriesFB(FlatBufferBuilder builder,
      ulong Term = 0,
      ulong PrevLogIdx = 0,
      ulong PrevLogTerm = 0,
      ulong LeaderCommit = 0,
      VectorOffset EntriesOffset = default(VectorOffset)) {
    builder.StartObject(5);
    AppendEntriesFB.AddLeaderCommit(builder, LeaderCommit);
    AppendEntriesFB.AddPrevLogTerm(builder, PrevLogTerm);
    AppendEntriesFB.AddPrevLogIdx(builder, PrevLogIdx);
    AppendEntriesFB.AddTerm(builder, Term);
    AppendEntriesFB.AddEntries(builder, EntriesOffset);
    return AppendEntriesFB.EndAppendEntriesFB(builder);
  }

  public static void StartAppendEntriesFB(FlatBufferBuilder builder) { builder.StartObject(5); }
  public static void AddTerm(FlatBufferBuilder builder, ulong Term) { builder.AddUlong(0, Term, 0); }
  public static void AddPrevLogIdx(FlatBufferBuilder builder, ulong PrevLogIdx) { builder.AddUlong(1, PrevLogIdx, 0); }
  public static void AddPrevLogTerm(FlatBufferBuilder builder, ulong PrevLogTerm) { builder.AddUlong(2, PrevLogTerm, 0); }
  public static void AddLeaderCommit(FlatBufferBuilder builder, ulong LeaderCommit) { builder.AddUlong(3, LeaderCommit, 0); }
  public static void AddEntries(FlatBufferBuilder builder, VectorOffset EntriesOffset) { builder.AddOffset(4, EntriesOffset.Value, 0); }
  public static VectorOffset CreateEntriesVector(FlatBufferBuilder builder, Offset<LogFB>[] data) { builder.StartVector(4, data.Length, 4); for (int i = data.Length - 1; i >= 0; i--) builder.AddOffset(data[i].Value); return builder.EndVector(); }
  public static void StartEntriesVector(FlatBufferBuilder builder, int numElems) { builder.StartVector(4, numElems, 4); }
  public static Offset<AppendEntriesFB> EndAppendEntriesFB(FlatBufferBuilder builder) {
    int o = builder.EndObject();
    return new Offset<AppendEntriesFB>(o);
  }
};


}
