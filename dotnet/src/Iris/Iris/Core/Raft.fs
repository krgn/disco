namespace Iris.Core

open Argu
open System
open FlatBuffers
open Pallet.Core
open Iris.Serialization.Raft

//  ____        __ _      ___        _   _
// |  _ \ __ _ / _| |_   / _ \ _ __ | |_(_) ___  _ __  ___
// | |_) / _` | |_| __| | | | | '_ \| __| |/ _ \| '_ \/ __|
// |  _ < (_| |  _| |_  | |_| | |_) | |_| | (_) | | | \__ \
// |_| \_\__,_|_|  \__|  \___/| .__/ \__|_|\___/|_| |_|___/
//                            |_|

type RaftOptions =
  { RaftId           : string
  ; Debug            : bool
  ; IpAddr           : string
  ; WebPort          : int
  ; RaftPort         : int
  ; Start            : bool
  ; LeaderId         : string option
  ; LeaderIp         : string option
  ; LeaderPort       : uint32 option
  }


//   ____ _     ___      _
//  / ___| |   |_ _|    / \   _ __ __ _ ___
// | |   | |    | |    / _ \ | '__/ _` / __|
// | |___| |___ | |   / ___ \| | | (_| \__ \
//  \____|_____|___| /_/   \_\_|  \__, |___/
//                                |___/

type GeneralArgs =
  | [<Mandatory>][<EqualsAssignment>] Bind       of string
  | [<Mandatory>][<EqualsAssignment>] RaftNodeId of string
  | [<Mandatory>][<EqualsAssignment>] RaftPort   of uint32
  | [<Mandatory>][<EqualsAssignment>] WebPort    of uint32
  |                                   Debug
  |                                   Start
  |                                   Join
  |              [<EqualsAssignment>] LeaderId   of string
  |              [<EqualsAssignment>] LeaderIp   of string
  |              [<EqualsAssignment>] LeaderPort of uint32

  interface IArgParserTemplate with

    member self.Usage =
      match self with
        | Bind       _ -> "Specify a valid IP address."
        | WebPort    _ -> "Http server port."
        | RaftPort   _ -> "Raft server port (internal)."
        | RaftNodeId _ -> "Raft server ID (internal)."
        | Debug        -> "Log output to console."
        | Start        -> "Start a new cluster"
        | Join         -> "Join an existing cluster"
        | LeaderId   _ -> "Leader id when joining an existing cluster"
        | LeaderIp   _ -> "Ip address of leader when joining a cluster"
        | LeaderPort _ -> "Port of leader when joining a cluster"


//  _____ ____    _   _      _
// |  ___| __ )  | | | | ___| |_ __   ___ _ __ ___
// | |_  |  _ \  | |_| |/ _ \ | '_ \ / _ \ '__/ __|
// |  _| | |_) | |  _  |  __/ | |_) |  __/ |  \__ \
// |_|   |____/  |_| |_|\___|_| .__/ \___|_|  |___/
//                            |_|
[<AutoOpen>]
module RaftMsgFB =

  let getValue (t : Offset<'a>) : int = t.Value

  let build builder tipe value =
    RaftMsgFB.StartRaftMsgFB(builder)
    RaftMsgFB.AddMsgType(builder, tipe)
    RaftMsgFB.AddMsg(builder, value)
    RaftMsgFB.EndRaftMsgFB(builder)
    |> getValue

  let createAppendEntriesFB (builder: FlatBufferBuilder) (nid: NodeId) (ar: AppendEntries) =
    let sid = string nid |> builder.CreateString
    RequestAppendEntriesFB.CreateRequestAppendEntriesFB(builder, sid, ar.ToOffset builder)
    |> getValue

  let createAppendResponseFB (builder: FlatBufferBuilder) (nid: NodeId) (ar: AppendResponse) =
    let id = string nid |> builder.CreateString
    RequestAppendResponseFB.CreateRequestAppendResponseFB(builder, id, ar.ToOffset builder)
    |> getValue

  let createRequestVoteFB (builder: FlatBufferBuilder) (nid: NodeId) (vr: VoteRequest) =
    let id = string nid |> builder.CreateString
    RequestVoteFB.CreateRequestVoteFB(builder, id, vr.ToOffset builder)
    |> getValue

  let createRequestVoteResponseFB (builder: FlatBufferBuilder) (nid: NodeId) (vr: VoteResponse) =
    let id = string nid |> builder.CreateString
    RequestVoteResponseFB.CreateRequestVoteResponseFB(builder, id, vr.ToOffset builder)
    |> getValue

  let createInstallSnapshotFB (builder: FlatBufferBuilder) (nid: NodeId) (is: InstallSnapshot) =
    let id = string nid |> builder.CreateString
    RequestInstallSnapshotFB.CreateRequestInstallSnapshotFB(builder, id, is.ToOffset builder)
    |> getValue

  let createSnapshotResponseFB (builder: FlatBufferBuilder) (nid: NodeId) (sr: SnapshotResponse) =
    let id = string nid |> builder.CreateString
    RequestSnapshotResponseFB.CreateRequestSnapshotResponseFB(builder, id, sr.ToOffset builder)
    |> getValue

//  ____        __ _     __  __
// |  _ \ __ _ / _| |_  |  \/  |___  __ _
// | |_) / _` | |_| __| | |\/| / __|/ _` |
// |  _ < (_| |  _| |_  | |  | \__ \ (_| |
// |_| \_\__,_|_|  \__| |_|  |_|___/\__, |
//                                  |___/
type RaftMsg =
  | RequestVote             of sender:NodeId * req:VoteRequest
  | RequestVoteResponse     of sender:NodeId * vote:VoteResponse
  | AppendEntries           of sender:NodeId * ae:AppendEntries
  | AppendEntriesResponse   of sender:NodeId * ar:AppendResponse
  | InstallSnapshot         of sender:NodeId * is:InstallSnapshot
  | InstallSnapshotResponse of sender:NodeId * ir:SnapshotResponse
  | HandShake               of sender:Node
  | HandWaive               of sender:Node
  | Redirect                of leader:Node
  | Welcome
  | Arrivederci
  | ErrorResponse           of RaftError
  | EmptyResponse

  with

    member self.ToBytes () : byte array =
      let builder = new FlatBufferBuilder(1)

      match self with
      | RequestVote(nid, req) ->
        createRequestVoteFB builder nid req
        |> build builder RaftMsgTypeFB.RequestVoteFB
        |> builder.Finish

      | RequestVoteResponse(nid, resp) ->
        createRequestVoteResponseFB builder nid resp
        |> build builder RaftMsgTypeFB.RequestVoteResponseFB
        |> builder.Finish

      | AppendEntries(nid, ae) ->
        createAppendEntriesFB builder nid ae
        |> build builder RaftMsgTypeFB.RequestAppendEntriesFB
        |> builder.Finish

      | AppendEntriesResponse(nid, ar) ->
        createAppendResponseFB builder nid ar
        |> build builder RaftMsgTypeFB.RequestAppendResponseFB
        |> builder.Finish

      | InstallSnapshot(nid, is) ->
        createInstallSnapshotFB builder nid is
        |> build builder RaftMsgTypeFB.RequestInstallSnapshotFB
        |> builder.Finish

      | InstallSnapshotResponse(nid, ir) ->
        createSnapshotResponseFB builder nid ir
        |> build builder RaftMsgTypeFB.RequestSnapshotResponseFB
        |> builder.Finish

      | HandShake node ->
        HandShakeFB.CreateHandShakeFB(builder, node.ToOffset builder)
        |> getValue
        |> build builder RaftMsgTypeFB.HandShakeFB
        |> builder.Finish

      | HandWaive node ->
        HandWaiveFB.CreateHandWaiveFB(builder, node.ToOffset builder)
        |> getValue
        |> build builder RaftMsgTypeFB.HandWaiveFB
        |> builder.Finish

      | Redirect node ->
        RedirectFB.CreateRedirectFB(builder, node.ToOffset builder)
        |> getValue
        |> build builder RaftMsgTypeFB.RedirectFB
        |> builder.Finish

      | Welcome ->
        WelcomeFB.StartWelcomeFB(builder)
        WelcomeFB.EndWelcomeFB(builder)
        |> getValue
        |> build builder RaftMsgTypeFB.WelcomeFB
        |> builder.Finish

      | Arrivederci ->
        ArrivederciFB.StartArrivederciFB(builder)
        ArrivederciFB.EndArrivederciFB(builder)
        |> getValue
        |> build builder RaftMsgTypeFB.ArrivederciFB
        |> builder.Finish

      | ErrorResponse err ->
        ErrorResponseFB.CreateErrorResponseFB(builder, err.ToOffset builder)
        |> getValue
        |> build builder RaftMsgTypeFB.ErrorResponseFB
        |> builder.Finish

      | EmptyResponse ->
        EmptyResponseFB.StartEmptyResponseFB(builder)
        EmptyResponseFB.EndEmptyResponseFB(builder)
        |> getValue
        |> build builder RaftMsgTypeFB.EmptyResponseFB
        |> builder.Finish

      builder.SizedByteArray()

    static member FromBytes (bytes: byte array) : RaftMsg option =
      let msg = RaftMsgFB.GetRootAsRaftMsgFB(new ByteBuffer(bytes))
      match msg.MsgType with
        | RaftMsgTypeFB.RequestVoteFB ->
          let entry = msg.GetMsg(new RequestVoteFB())
          let request = VoteRequest<IrisNode>.FromFB(entry.Request)

          RequestVote(RaftId entry.NodeId, request)
          |> Some

        | RaftMsgTypeFB.RequestVoteResponseFB ->
          let entry = msg.GetMsg(new RequestVoteResponseFB())
          let response = VoteResponse.FromFB entry.Response

          RequestVoteResponse(RaftId entry.NodeId, response)
          |> Some

        | RaftMsgTypeFB.RequestAppendEntriesFB ->
          let entry = msg.GetMsg(new RequestAppendEntriesFB())
          let request = AppendEntries.FromFB entry.Request

          AppendEntries(RaftId entry.NodeId, request)
          |> Some

        | RaftMsgTypeFB.RequestAppendResponseFB ->
          let entry = msg.GetMsg(new RequestAppendResponseFB())
          let response = AppendResponse.FromFB entry.Response

          AppendEntriesResponse(RaftId entry.NodeId, response)
          |> Some

        | RaftMsgTypeFB.RequestInstallSnapshotFB ->
          let entry = msg.GetMsg(new RequestInstallSnapshotFB())
          let request = InstallSnapshot.FromFB entry.Request

          InstallSnapshot(RaftId entry.NodeId, request)
          |> Some

        | RaftMsgTypeFB.RequestSnapshotResponseFB ->
          let entry = msg.GetMsg(new RequestSnapshotResponseFB())
          let response = SnapshotResponse.FromFB entry.Response

          InstallSnapshotResponse(RaftId entry.NodeId, response)
          |> Some

        | RaftMsgTypeFB.HandShakeFB ->
          let entry = msg.GetMsg(new HandShakeFB())
          let node = Node.FromFB entry.Node

          HandShake(node) |> Some

        | RaftMsgTypeFB.HandWaiveFB ->
          let entry = msg.GetMsg(new HandWaiveFB())
          let node = Node.FromFB entry.Node

          HandWaive(node) |> Some

        | RaftMsgTypeFB.RedirectFB ->
          let entry = msg.GetMsg(new RedirectFB())
          let node = Node.FromFB entry.Node

          Redirect(node) |> Some

        | RaftMsgTypeFB.WelcomeFB ->
          Welcome |> Some

        | RaftMsgTypeFB.ArrivederciFB ->
          Arrivederci |> Some

        | RaftMsgTypeFB.ErrorResponseFB ->
          let entry = msg.GetMsg(new ErrorResponseFB())

          ErrorResponse(RaftError.FromFB entry.Error)
          |> Some

        | RaftMsgTypeFB.EmptyResponseFB -> Some EmptyResponse

        | _ ->
          failwith "unable to de-serialize unknown garbage RaftMsgTypeFB"
