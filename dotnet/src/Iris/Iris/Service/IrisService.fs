namespace Iris.Service.Core

open System
open System.IO
open Iris.Raft
open Iris.Core
open Iris.Core.Utils
open Iris.Service
open Iris.Service.Types
open Iris.Service.Core
open Iris.Service.Raft.Server
open LibGit2Sharp
open ZeroMQ
open FSharpx.Functional

[<AutoOpen>]
module Hooks =
  let saveAsset (location: FilePath) (payload: string) =
    failwith "never"

  let inline maybeSave (project: Project) (thing: ^t) =
    failwith "something"

//  ___      _     ____                  _
// |_ _|_ __(_)___/ ___|  ___ _ ____   _(_) ___ ___
//  | || '__| / __\___ \ / _ \ '__\ \ / / |/ __/ _ \
//  | || |  | \__ \___) |  __/ |   \ V /| | (_|  __/
// |___|_|  |_|___/____/ \___|_|    \_/ |_|\___\___|
//

type IrisService(project: Project) =
  let signature = new Signature("Karsten Gebbert", "k@ioctl.it", new DateTimeOffset(DateTime.Now))

  let store : Store = new Store(State.Empty)

  let kontext = new ZContext()

  let raftserver = new RaftServer(project.Config, kontext)
  let wsserver   = new WsServer(project.Config, raftserver)
  let httpserver = new AssetServer(project.Config)

  let setup _ =
    // WEBSOCKET
    wsserver.OnOpen <- fun (session: Session) ->
      wsserver.Send session.Id (DataSnapshot store.State)
      let msg =
        match raftserver.Append(AddSession session) with
        | Some entry -> Iris.Core.LogLevel.Debug, (sprintf "Added session to Raft log with id: %A" entry.Id)
        | _          -> Iris.Core.LogLevel.Err, "Could not add new session log."
      wsserver.Broadcast (LogMsg msg)

    wsserver.OnClose <- fun sessionid ->
      match Map.tryFind sessionid store.State.Sessions with
      | Some session ->
        let msg =
          match raftserver.Append(RemoveSession session) with
          | Some entry -> Iris.Core.LogLevel.Debug, (sprintf "Remove session added to Raft log with id: %A" entry.Id)
          | _          -> Iris.Core.LogLevel.Err, "Could not remove session log."
        wsserver.Broadcast (LogMsg msg)
      | _ ->
        let msg = Iris.Core.LogLevel.Err, "Session not found. Something spooky is going on"
        wsserver.Broadcast (LogMsg msg)

    wsserver.OnError <- fun sessionid ->
      match Map.tryFind sessionid store.State.Sessions with
      | Some session ->
        let msg =
          match raftserver.Append(RemoveSession session) with
          | Some entry -> Iris.Core.LogLevel.Debug, (sprintf "Remove session (due to Error) added to Raft log with id: %A" entry.Id)
          | _          -> Iris.Core.LogLevel.Err, "Could not remove session log."
        wsserver.Broadcast (LogMsg msg)
      | _ ->
        let msg = Iris.Core.LogLevel.Err, "Session not found. Something spooky is going on"
        wsserver.Broadcast (LogMsg msg)

    wsserver.OnMessage <- fun sessionid command ->
      let msg =
        match raftserver.Append(command) with
        | Some entry -> Iris.Core.LogLevel.Debug, (sprintf "Entry added to Raft log with id: %A" entry.Id)
        | _          -> Iris.Core.LogLevel.Err, "Could not add new entry to Raft log :("
      wsserver.Broadcast (LogMsg msg)

    // RAFTSERVER
    raftserver.OnConfigured <-
      Array.map (fun (node: RaftNode) -> string node.Id)
      >> Array.fold (fun s id -> sprintf "%s %s" s  id) "New Configuration with: "
      >> (fun str -> LogMsg(Iris.Core.LogLevel.Debug, str))
      >> wsserver.Broadcast

    raftserver.OnLogMsg <- fun _ msg ->
      wsserver.Broadcast(LogMsg(Iris.Core.LogLevel.Debug, msg))

    raftserver.OnNodeAdded   <- AddNode    >> wsserver.Broadcast
    raftserver.OnNodeUpdated <- UpdateNode >> wsserver.Broadcast
    raftserver.OnNodeRemoved <- RemoveNode >> wsserver.Broadcast

    raftserver.OnApplyLog <- fun sm ->
      store.Dispatch sm
      wsserver.Broadcast sm

  do setup ()

  member self.Raft
    with get () : RaftServer = raftserver

  //  ___       _             __
  // |_ _|_ __ | |_ ___ _ __ / _| __ _  ___ ___  ___
  //  | || '_ \| __/ _ \ '__| |_ / _` |/ __/ _ \/ __|
  //  | || | | | ||  __/ |  |  _| (_| | (_|  __/\__ \
  // |___|_| |_|\__\___|_|  |_|  \__,_|\___\___||___/
  //
  interface IDisposable with
    member self.Dispose() =
      self.Stop()

  //  _     _  __       ____           _
  // | |   (_)/ _| ___ / ___|   _  ___| | ___
  // | |   | | |_ / _ \ |  | | | |/ __| |/ _ \
  // | |___| |  _|  __/ |__| |_| | (__| |  __/
  // |_____|_|_|  \___|\____\__, |\___|_|\___|
  //                        |___/
  member self.Start() =
    printfn "Starting Http Server on %d" project.Config.PortConfig.Http
    httpserver.Start()

    printfn "Starting WebSocket Server on %d" project.Config.PortConfig.WebSocket
    wsserver.Start()

    printfn "Starting Raft Server %d" project.Config.PortConfig.Raft
    raftserver.Start()

  member self.Stop() =
    dispose raftserver
    dispose wsserver
    dispose httpserver
    dispose kontext

  //  ____            _           _
  // |  _ \ _ __ ___ (_) ___  ___| |_
  // | |_) | '__/ _ \| |/ _ \/ __| __|
  // |  __/| | | (_) | |  __/ (__| |_
  // |_|   |_|  \___// |\___|\___|\__|
  //               |__/

  // member self.SaveProject(id, msg) =
  //   match saveProject id signature msg !state with
  //     | Success (commit, newstate) ->
  //       state := newstate
  //       Either.succeed commit
  //     | Fail err -> Either.fail err

  //   ____                _
  //  / ___|_ __ ___  __ _| |_ ___
  // | |   | '__/ _ \/ _` | __/ _ \
  // | |___| | |  __/ (_| | ||  __/
  //  \____|_|  \___|\__,_|\__\___|

  // member self.CreateProject(name, path) =
  //   createProject name path signature !state
  //     >>= fun (project, state') ->
  //       // add and start the process groups for this project
  //       let result =
  //         project.Name
  //           >>= startProcess
  //           >>= (addProcess project.Id >> succeed)

  //       match result with
  //         | Success _ -> state := state'
  //                        self.Ctrl.Load(project.Id, project.Name)
  //                        succeed project
  //         | Fail err  -> logger tag err
  //                        fail err
  //   printfn "hm"

  //   ____ _
  //  / ___| | ___  ___  ___
  // | |   | |/ _ \/ __|/ _ \
  // | |___| | (_) \__ \  __/
  //  \____|_|\___/|___/\___|

  // member self.CloseProject(pid) =
  //   findProject pid !state
  //     >>= fun project ->
  //       combine project (closeProject pid !state)
  //     >>= fun (project, state') ->
  //       // remove and stop process groups
  //       let result = removeProcess project.Id >>= stopProcess
  //       match result with
  //         | Success _ -> state := state'                    // save global state
  //                        self.Ctrl.Close(pid, project.Name) // notify everybody
  //                        succeed project
  //         | Fail err  -> logger tag err
  //                        fail err

  //   printfn "fm"

  //  _                    _
  // | |    ___   __ _  __| |
  // | |   / _ \ / _` |/ _` |
  // | |__| (_) | (_| | (_| |
  // |_____\___/ \__,_|\__,_|

  // member self.LoadProject(path : FilePath) =
  //   loadProject path !state
  //     >>= fun (project, state') ->
  //       // add and start the process groups for this project
  //       let result =
  //         ProjectProcess.Create project
  //           >>= startProcess
  //           >>= (addProcess project.Id >> succeed)

  //       match result with
  //         | Success _ -> self.Ctrl.Load(project.Id, project.Name)
  //                        state := state'
  //                        succeed project
  //         | Fail err  -> logger tag err
  //                        fail err
  //   printfn "oh"
