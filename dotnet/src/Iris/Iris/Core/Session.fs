namespace Iris.Core

#if JAVASCRIPT

open Fable.Core
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open System
open FlatBuffers
open Iris.Serialization.Raft

#endif

type Session =
  { Id: Id
  ; UserName:  UserName
  ; IpAddress: IpAddress
  ; UserAgent: UserAgent
  }

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  static member FromFB(fb: SessionFB) : Session option =
    try
      { Id = Id fb.Id
      ; UserName  = fb.UserName
      ; IpAddress = IpAddress.Parse fb.IpAddress
      ; UserAgent = fb.UserAgent
      }
      |> Some
    with
      | exn ->
        printfn "Could not de-serializae Session binary value: %s" exn.Message
        None

  static member FromBytes(bytes: Binary.Buffer) : Session option =
    Binary.createBuffer bytes
    |> SessionFB.GetRootAsSessionFB
    |> Session.FromFB

  member self.ToOffset(builder: FlatBufferBuilder) =
    let session = self.Id |> string |> builder.CreateString
    let name = self.UserName |> builder.CreateString
    let ip = self.IpAddress |> string |> builder.CreateString
    let ua = self.UserAgent |> string |> builder.CreateString
    SessionFB.StartSessionFB(builder)
    SessionFB.AddId(builder, session)
    SessionFB.AddUserName(builder, name)
    SessionFB.AddIpAddress(builder, ip)
    SessionFB.AddUserAgent(builder, ua)
    SessionFB.EndSessionFB(builder)

  member self.ToBytes() = Binary.buildBuffer self