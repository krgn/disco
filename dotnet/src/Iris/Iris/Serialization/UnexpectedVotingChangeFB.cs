// automatically generated by the FlatBuffers compiler, do not modify

namespace Iris.Serialization
{

using System;
using FlatBuffers;

public sealed class UnexpectedVotingChangeFB : Table {
  public static UnexpectedVotingChangeFB GetRootAsUnexpectedVotingChangeFB(ByteBuffer _bb) { return GetRootAsUnexpectedVotingChangeFB(_bb, new UnexpectedVotingChangeFB()); }
  public static UnexpectedVotingChangeFB GetRootAsUnexpectedVotingChangeFB(ByteBuffer _bb, UnexpectedVotingChangeFB obj) { return (obj.__init(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
  public UnexpectedVotingChangeFB __init(int _i, ByteBuffer _bb) { bb_pos = _i; bb = _bb; return this; }


  public static void StartUnexpectedVotingChangeFB(FlatBufferBuilder builder) { builder.StartObject(0); }
  public static Offset<UnexpectedVotingChangeFB> EndUnexpectedVotingChangeFB(FlatBufferBuilder builder) {
    int o = builder.EndObject();
    return new Offset<UnexpectedVotingChangeFB>(o);
  }
};


}
