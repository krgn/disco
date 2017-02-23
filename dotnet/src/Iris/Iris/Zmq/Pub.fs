namespace Iris.Zmq

// * Imports

open System
open System.Threading
open ZeroMQ
open Iris.Core

// * Pub

/// ## Pub
///
/// Thread-safe Pub socket corresponds to ZSocketType.PUB
///
/// ### Signature:
/// - addr: Address to connect to
///
/// Returns: instance of Pub
type Pub (id: Id, addr: string, prefix: string) =

  let tag = sprintf "Pub.%s"

  let mutable starter   = Unchecked.defaultof<AutoResetEvent>
  let mutable stopper   = Unchecked.defaultof<AutoResetEvent>
  let mutable requester = Unchecked.defaultof<AutoResetEvent>
  let mutable responder = Unchecked.defaultof<AutoResetEvent>

  let mutable exn: Exception option = None

  let mutable request:  byte array = [| |]

  let mutable run = true
  let mutable started = false
  let mutable disposed = false
  let mutable error = None

  let mutable thread = Unchecked.defaultof<Thread>
  let mutable sock = Unchecked.defaultof<ZSocket>
  let mutable lokk = Unchecked.defaultof<Object>
  let mutable ctx = Unchecked.defaultof<ZContext>

  // ** worker

  let worker _ =                                              // thread worker function
    if isNull sock then                                       // if not yet present
      try
        "initializing context and socket"
        |> Logger.debug id (tag "workder")

        ctx <- new ZContext()
        sock <- new ZSocket(ctx, ZSocketType.PUB)                // initialise the socket

        sprintf "connecting to %A" addr
        |> Logger.debug id (tag "worker")

        setOption sock ZSocketOption.RATE 100000
        sock.Bind(addr)                                         // connect to server
        started <- true
        starter.Set() |> ignore                                  // signal that startup is done
      with
        | ex ->
          run <- false
          exn <- Some ex
          starter.Set() |> ignore

    "entering publish loop"
    |> Logger.debug id (tag "worker")

    while run do
      try
        // wait for the signal that a new request is ready *or* that shutdown is reuqested
        "waiting for a publish"
        |> Logger.debug id (tag "worker")
        requester.WaitOne() |> ignore

        // `run` is usually true, but shutdown first sets this to false to exit the loop
        if run then
          let msg = new ZMessage()
          msg.Add(new ZFrame(prefix))
          msg.Add(new ZFrame(request))                        // create a new ZFrame to send
          sock.Send(msg)                                      // and send it via sock
          responder.Set() |> ignore                            // signal that response is ready
          dispose msg
        else
          responder.Set() |> ignore
      with
        | e ->
          e.Message
          |> sprintf "exception: %s"
          |> Logger.err id (tag "worker")

          // save exception to be rethrown on the callers thread
          exn <- Some e
          // prevent re-entering the loop
          run <- false
          // set the responder so self.Publish does not block indefinietely
          responder.Set() |> ignore

    "exited loop. disposing."
    |> Logger.debug id (tag "worker")

    sock.SetOption(ZSocketOption.LINGER, 0) |> ignore  // set linger to 0 to close socket quickly
    sock.Close()                                      // close the socket
    sock.Dispose()                                    // dispose of it
    ctx.Dispose()
    disposed <- true                                   // this socket is disposed
    started <- false                                   // and not running anymore
    stopper.Set() |> ignore                            // signal that everything was cleaned up now

    "thread-local shutdown done"
    |> Logger.debug id (tag "worker")

  // ** Constructor

  do
    lokk      <- new Object()                       // lock object
    starter   <- new AutoResetEvent(false)          // initialize the signals
    stopper   <- new AutoResetEvent(false)
    requester <- new AutoResetEvent(false)
    responder <- new AutoResetEvent(false)

  // ** Id

  member self.Id
    with get () = id

  // ** Start

  member self.Start() =
    if not disposed then
      thread <- new Thread(new ThreadStart(worker))  // create worker thread
      thread.Start()                                // start worker thread
      starter.WaitOne() |> ignore                    // wait for startup-done signal

      match error with
      | Some exn ->                                  // if an exception happened on the thread
        exn.Message                                 // format it as an error and return it
        |> Error.asSocketError (tag "Start")
        |> Either.fail
      | _ -> Right ()                                // parents thread, so it can be
                                                    // caught and handled synchronously
    else
      "already disposed"
      |> Error.asSocketError (tag "Start")
      |> Either.fail

  // ** Stop

  member self.Stop() =
    if started && not disposed then
      "stopping stocket thread"
      |> Logger.debug id (tag "Stop")

      run <- false                                  // stop the loop from iterating
      requester.Set() |> ignore                     // signal requester one more time to exit loop
      stopper.WaitOne() |> ignore                   // wait for stopper to signal disposed done
      thread.Join()

      "socket shutdown complete"
      |> Logger.debug id (tag "Stop")
    else
      "refusing to stop. wrong state"
      |> Logger.err id (tag "Stop")

  // ** Restart

  member self.Restart() =
    "restarting socket"
    |> Logger.debug id (tag "Restart")

    self.Stop()                                    // stop, if not stopped yet
    disposed <- false                               // disposed reset to default
    run <- true                                     // run reset to default
    self.Start()                                   // start the socket

  // ** Publish

  member self.Publish(req: byte array) : Either<IrisError,unit> =
    if started && not disposed then        // synchronously request the square of `req-`
      Logger.debug id (tag "Publish") "publishing message"

      lock lokk <| fun _ ->                 // lock while executing transaction
        request <- req                   // first set the requets
        requester.Set() |> ignore        // then signal a request is ready for execution
        responder.WaitOne() |> ignore    // wait for signal that execution has finished
        match exn with                  // handle exception raised on thread
        | Some e ->                      // re-raise it on callers thread
          e.Message
          |> Logger.err id (tag "Publish")

          e.Message
          |> sprintf "Exception thrown on socket thread: %s"
          |> Error.asSocketError "Pub.Publish"
          |> Either.fail
        | _  ->
          "publish successful"
          |> Logger.debug id (tag "Publish")
          Either.succeed ()             // return the response
    elif disposed then                  // disposed sockets need to be re-initialized
      "refusing request. already disposed"
      |> Logger.err id (tag "Publish")

      "Socket disposed"
      |> Error.asSocketError (tag "Publish")
      |> Either.fail
    else
      "refusing request. socket has not been started"
      |> Logger.err id (tag "Publish")

      "Socket not started"
      |> Error.asSocketError (tag "Publish")
      |> Either.fail

  member self.Running
    with get () = started && not disposed

  // ** Dispose

  interface IDisposable with
    member self.Dispose() = self.Stop()