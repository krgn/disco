[<AutoOpen>]
module Disco.Web.Core.Client

open System
open System.Collections.Generic
open Fable.Core
open Fable.Import
open Fable.Core.JsInterop
open Fable.PowerPack
open Disco.Web.Notifications
open Disco.Core
open Disco.Core.Commands

//  ____  _                        ___        __         _
// / ___|| |__   __ _ _ __ ___  __| \ \      / /__  _ __| | _____ _ __
// \___ \| '_ \ / _` | '__/ _ \/ _` |\ \ /\ / / _ \| '__| |/ / _ \ '__|
//  ___) | | | | (_| | | |  __/ (_| | \ V  V / (_) | |  |   <  __/ |
// |____/|_| |_|\__,_|_|  \___|\__,_|  \_/\_/ \___/|_|  |_|\_\___|_|

type ErrorMsg() =
  [<Emit("$0.message")>]
  member __.Message
    with get () : string = failwith "ONLY JS"

[<Global>]
type SharedWorker<'data>(url: string) =
  [<Emit("$0.onerror = $1")>]
  member __.OnError
    with set (_: ErrorMsg -> unit) = failwith "ONLY JS"

  [<Emit("$0.port")>]
  member __.Port
    with get () : MessagePort<'data> = failwith "ONLY JS"

//   ____ _ _            _
//  / ___| (_) ___ _ __ | |_
// | |   | | |/ _ \ '_ \| __|
// | |___| | |  __/ | | | |_
//  \____|_|_|\___|_| |_|\__|

type ClientContext private () =
  let mutable store: Store option =
    #if DESIGN // Mockup data
    Disco.Web.Core.MockData.getMockState() |> Store |> Some
    #else
    None
    #endif
  let mutable session : SessionId option = None
  let mutable serviceInfo: ServiceInfo option = None
  let mutable worker : SharedWorker<string> option = None
  let ctrls = Dictionary<Guid, IObserver<ClientMessage<State>>>()

  static let mutable singleton: ClientContext option = None

  static member Singleton =
    match singleton with
    | Some singleton -> singleton
    | None ->
      let client = new ClientContext()
      singleton <- Some client
      client

  member self.ServiceInfo =
    serviceInfo.Value

  member self.Start() = promise {
    let me = new SharedWorker<string>(Constants.WEB_WORKER_SCRIPT)
    me.OnError <- fun e -> sprintf "%A" e.Message |> Notifications.error
    me.Port.OnMessage <- self.HandleMessageEvent
    worker <- Some me
    #if !DESIGN
    do! self.ConnectWithWebSocket()
    #endif
  }

  member self.ConnectWithWebSocket() =
    (Commands.GetServiceInfo, [])
    ||> Fetch.postRecord Constants.WEP_API_COMMAND
    |> Promise.bind (fun res -> res.text())
    |> Promise.map (fun json ->
      try
        match ofJson<ServiceInfo option> json with
        | Some info ->
          serviceInfo <- Some info
          ClientMessage.Connect info.webSocket
          |> toJson |> self.Worker.Port.PostMessage
        | None -> serviceInfo <- None
      with
      | err ->
        err.Message
        |> sprintf "Error parsing GetServiceInfo reply: %s"
        |> Notifications.error)

  member self.Session =
    match session with
    | Some session -> session
    | None -> failwith "Client not initialized"

  member self.Worker: SharedWorker<string> =
    match worker with
    | Some worker -> worker
    | None -> failwith "Client not initialized"

  member self.Store = store

  member self.Trigger(msg: ClientMessage<StateMachine>) =
    self.Worker.Port.PostMessage(toJson msg)

  member self.Post(ev: StateMachine) =
    printfn "Client will send state machine command %A" ev
    ClientMessage.Event(self.Session, ev)
    #if DESIGN
    |> self.HandleClientMessage
    #else
    |> toJson
    |> self.Worker.Port.PostMessage
    #endif

  /// Don't send command to backend, it'll affect only the local store
  member self.PostLocal(ev: StateMachine) =
    printfn "Local update: %A" ev
    ClientMessage.Event(self.Session, ev)
    |> self.HandleClientMessage

  member self.Log (logLevel: LogLevel) (message : string) : unit =
    let log = Logger.create logLevel "frontend" message
    let msg = ClientMessage.Event(self.Session, LogMsg log)
    for ctrl in ctrls.Values do
      ctrl.OnNext msg

  member self.HandleMessageEvent (msg : MessageEvent<string>) : unit =
    let data = ofJson<ClientMessage<State>> msg.Data
    self.HandleClientMessage(data)

  member self.HandleClientMessage (data : ClientMessage<State>) : unit =
    match data with
    // initialize this client session variable
    | ClientMessage.Initialized(token) ->
      session <- Some(token)
      token
      |> sprintf "Initialized with Session: %A"
      |> Notifications.info

    // close this clients session variable
    | ClientMessage.Closed(token) ->
      token
      |> sprintf "A client closed its session: %A"
      |> Notifications.info

    | ClientMessage.Stopped ->
      Notifications.error "Worker stopped. TODO: Needs to be restarted"
      //self.Start() // TODO: Restart worker

    | ClientMessage.Event(_,ev) ->
      match ev with
      | LogMsg _
      | UpdateClock _ -> () // Delegate responsibility to controllers
      | DataSnapshot state ->
        let s = Store(state)
        store <- Some s
      | StateMachine.UnloadProject ->
        store <- None
      | ev ->
        match store with
        | Some store ->
          try store.Dispatch ev
          with exn ->
            self.Log LogLevel.Err (sprintf "Error when updating store: %s" exn.Message)
        | None ->
          "Received message but store is not initialized"
          |> self.Log LogLevel.Debug

    | ClientMessage.Connected ->
      Session.Empty self.Session |> AddSession |> self.Post
      Notifications.info "WebSocket connection established"

    | ClientMessage.Disconnected ->
      Notifications.warn "WebSocket connection closed"

    | ClientMessage.Error(reason) ->
      reason
      |> sprintf "SharedWorker Error: %A"
      |> Notifications.error

    | msg -> printfn "Unknown Event: %A" msg // TODO: Log

    // Inform listeners
    for ctrl in ctrls.Values do
      ctrl.OnNext data

  member __.OnMessage =
    { new IObservable<_> with
      member __.Subscribe(obs) =
        let guid = Guid.NewGuid()
        ctrls.Add(guid, obs)
        { new IDisposable with
            member __.Dispose() = ctrls.Remove(guid) |> ignore } }

  interface IDisposable with
    member self.Dispose() =
      ClientMessage.Close(self.Session)
      |> toJson
      |> self.Worker.Port.PostMessage
