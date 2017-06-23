namespace rec Iris.Net

// * Imports

open System
open System.IO
open System.Net
open System.Net.Sockets
open System.Text
open Iris.Core
open System.Collections.Concurrent

// * Request

type Request =
  { RequestId: Guid
    ConnectionId: Guid
    Body: byte array }

  // ** PeedId

  member request.PeerId
    with get () = Guid.toId request.ConnectionId

// * RequestBuilder

[<AllowNullLiteral>]
type IRequestBuilder =
  inherit IDisposable
  abstract Process: read:int -> unit

// * RequestBuilder module

module RequestBuilder =

  // ** Constants

  let Preamble = [| 73uy; 77uy; 83uy; 71uy |] // IMSG in ASCII

  [<Literal>]
  let PreambleSize = 4                  // 4 bytes in ASCII

  [<Literal>]
  let MsgLengthSize = 8                 // int64 is 8 bytes long

  [<Literal>]
  let IdSize = 16                       // Guid has 16 bytes

  //  preamble | length of body  | length of client id | length of request id
  // ------------------------------------------------------------------------
  //     4     +        8        +        16           +      16
  // ------------------------------------------------------------------------
  let HeaderSize = PreambleSize + MsgLengthSize + IdSize + IdSize

  // ** parseLength

  let private parseLength (data: byte array) (offset: int) =
    BitConverter.ToInt64(data, offset)

  // ** serializeLength

  let private serializeLength (request: Request) =
    int64 request.Body.Length
    |> BitConverter.GetBytes

  // ** serialize

  let serialize (request: Request) : byte array =
    let totalSize = int HeaderSize + request.Body.Length
    let destination = Array.zeroCreate totalSize
    // copy the preamble
    Array.Copy(
      Preamble,
      destination,
      PreambleSize)
    // copy the encoded message length to destination
    Array.Copy(
      RequestBuilder.serializeLength request,
      0,
      destination,
      PreambleSize,
      MsgLengthSize)
    // copy the connection id (peer id) to destination
    Array.Copy(
      request.ConnectionId.ToByteArray(),
      0,
      destination,
      PreambleSize + MsgLengthSize,
      IdSize)
    // copy the request id to destination
    Array.Copy(
      request.RequestId.ToByteArray(),
      0,
      destination,
      PreambleSize + MsgLengthSize + IdSize,
      IdSize)
    // copy the message body to destination
    Array.Copy(
      request.Body,
      0,
      destination,
      HeaderSize,
      request.Body.Length)
    destination                         // done!

  // ** findPreamble

  let private findPreamble (data: byte array) =
    Array.tryFindIndex ((=) Preamble.[0]) data

  // ** PreambleMsgOffset

  let private PreambleMsgOffset = PreambleSize + MsgLengthSize

  // ** PreambleMsgIdOffset

  let private PreambleMsgIdOffset = PreambleSize + MsgLengthSize + IdSize


  // ** create

  let create (buffer: byte array) (onComplete: Request -> unit) =
    let preamble = ResizeArray()
    let length = ResizeArray()
    let client = ResizeArray()
    let request = ResizeArray()
    let body = ResizeArray()

    let mutable bodyLength = 0L

    let finalize () =
      { RequestId    = Guid (request.ToArray())
        ConnectionId = Guid (client.ToArray())
        Body         = body.ToArray() }
      |> onComplete
      preamble.Clear()
      length.Clear()
      client.Clear()
      request.Clear()
      body.Clear()
      bodyLength <- 0L

    let rec build (data: byte array) (read: int) =
      let rest = ResizeArray()
      let mutable addToRest = false
      // this is a fresh response, so we start off nice and neat
      if preamble.Count = 0 && length.Count = 0 && client.Count = 0 && request.Count = 0 then
        match findPreamble data with
        | Some offset when offset <= read -> // we found the possible start of a preamble
          let remaining = read - offset
          for i in 0 .. remaining - 1 do
            match i with
            | _ when i < PreambleSize                                  && not addToRest ->
              // add data to preamble
              preamble.Add data.[i + offset]

              // if the preamble is now complete, check if its valid
              if preamble.Count = PreambleSize then
                // if the preamble is not valid, the rest of the data is just
                // going to get appended to the rest array, and processed again
                addToRest <- (preamble.ToArray()) <> Preamble

            | _ when i >= PreambleSize        && i < PreambleMsgOffset   && not addToRest ->
              length.Add data.[i + offset]
              if length.Count = int MsgLengthSize then
                bodyLength <- parseLength (length.ToArray()) 0

            | _ when i >= PreambleMsgOffset   && i < PreambleMsgIdOffset && not addToRest ->
              client.Add data.[i + offset]

            | _ when i >= PreambleMsgIdOffset && i < HeaderSize          && not addToRest ->
              request.Add data.[i + offset]

            | _ when int64 body.Count < bodyLength                      && not addToRest ->
              body.Add data.[i + offset]

            | _ when int64 body.Count = bodyLength                      && not addToRest ->
              finalize()
              addToRest <- true
              rest.Add data.[i + offset]

            | _ -> rest.Add data.[i + offset]
        | _ -> () // ignore data when no preamble was found and this is the first call
      else
        // we're already working on something here, so keep at it
        for i in 0 .. read - 1 do
          match i with
          | _ when preamble.Count < PreambleSize  && not addToRest ->
            // add data to preamble
            preamble.Add data.[i]

            // if the preamble is now complete, check if its valid
            if preamble.Count = PreambleSize then
              // if the preamble is not valid, the rest of the data is just
              // going to get appended to the rest array, and processed again
              addToRest <- (preamble.ToArray()) <> Preamble

          | _ when length.Count   < MsgLengthSize && not addToRest ->
            length.Add data.[i]

            // if enough bytes have been read, parse the length
            if length.Count = MsgLengthSize then
              bodyLength <- parseLength (length.ToArray()) 0

          | _ when client.Count   < IdSize        && not addToRest ->
            client.Add data.[i]

          | _ when request.Count  < IdSize        && not addToRest ->
            request.Add data.[i]

          | _ when int64 body.Count < bodyLength  && not addToRest ->
            body.Add data.[i]

          | _ when int64 body.Count = bodyLength  && not addToRest ->
            finalize()
            addToRest <- true
            rest.Add data.[i]

          | i -> rest.Add data.[i]

      if preamble.Count       = PreambleSize
        && preamble.ToArray() = Preamble
        && length.Count       = MsgLengthSize
        && client.Count       = IdSize
        && request.Count      = IdSize
        && int64 body.Count   = bodyLength
      then finalize()

      // ever more to do, the recurse
      if rest.Count > 0 then
        build (rest.ToArray()) rest.Count

    { new IRequestBuilder with
        member state.Process(read: int) =
          if read > 0 then
            build buffer read

        member state.Dispose() =
          length.Clear()
          client.Clear()
          request.Clear()
          body.Clear() }

// * Request module

module Request =

  let create (id: Guid) (body: byte array) : Request =
    { RequestId = Guid.NewGuid()
      ConnectionId = id
      Body = body }

  let serialize = RequestBuilder.serialize

// * Response

type Response = Request

// * IResponseBuilder

type IResponseBuilder = IRequestBuilder

// * ResponseBuilder module

module ResponseBuilder =

  let create data callback : IResponseBuilder =
    RequestBuilder.create data callback

// * Response module

module Response =

  let fromRequest (request: Request) (body: byte array) =
    { RequestId = request.RequestId
      ConnectionId = request.ConnectionId
      Body = body }

  let serialize (response: Response) = RequestBuilder.serialize response


// * Client

//   ____ _ _            _
//  / ___| (_) ___ _ __ | |_
// | |   | | |/ _ \ '_ \| __|
// | |___| | |  __/ | | | |_
//  \____|_|_|\___|_| |_|\__|

// ** ClientConfig

type ClientConfig =
  { PeerId: Id
    PeerAddress: IpAddress
    PeerPort: Port
    Timeout: Timeout }

// ** TcpClientEvent

type TcpClientEvent =
  | Connected    of peerid:Id
  | Disconnected of peerid:Id
  | Response     of Response

// ** IClient

type IClient =
  inherit IDisposable
  abstract Start: unit -> Either<IrisError,unit>
  abstract PeerId: Id
  abstract ConnectionId: Guid
  abstract Request: Request -> unit
  abstract Subscribe: (TcpClientEvent -> unit) -> IDisposable

// * Server

//  ____
// / ___|  ___ _ ____   _____ _ __
// \___ \ / _ \ '__\ \ / / _ \ '__|
//  ___) |  __/ |   \ V /  __/ |
// |____/ \___|_|    \_/ \___|_|

// ** ServerConfig

type ServerConfig =
  { Id: Id
    Listen: IpAddress
    Port: Port }

// ** TcpServerEvent

[<NoComparison;NoEquality>]
type TcpServerEvent =
  | Connect    of connectionId:Guid * ip:IPAddress * port:int
  | Disconnect of connectionId:Guid
  | Request    of Request

// ** IServer

type IServer =
  inherit IDisposable
  abstract Start: unit -> Either<IrisError,unit>
  abstract Subscribe: (TcpServerEvent -> unit) -> IDisposable
  abstract Respond: Response -> unit

// * PubSub

type PubSubEvent =
  | Request of peer:Id * data:byte array

type IPubSub =
  inherit IDisposable
  abstract Start: unit -> Either<IrisError,unit>
  abstract Send: byte array -> unit
  abstract Subscribe: (PubSubEvent -> unit) -> IDisposable

// * Core module

module Core =

  [<Literal>]
  let BUFFER_SIZE = 4048


// * Socket module

module Socket =

  // ** isAlive

  let isAlive (socket:Socket) =
    not (socket.Poll(1, SelectMode.SelectRead) && socket.Available = 0)

  // ** dispose

  let dispose (socket:Socket) =
    try
      socket.Shutdown(SocketShutdown.Both)
      socket.Close()
    finally
      socket.Dispose()

  // ** checkState

  let rec checkState<'t>
    (socket: Socket)
    (subscriptions: ConcurrentDictionary<Guid,IObserver<'t>>)
    (ev: 't) =
    async {
      do! Async.Sleep(1000)             // check socket liveness ever second
      try
        if isAlive socket then
          return! checkState socket subscriptions ev
        else
          Observable.onNext subscriptions ev
      with
        | _ -> Observable.onNext subscriptions ev
    }
