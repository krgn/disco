module Iris.Service.CommandLine

open Argu
open System
open Iris.Core
open Iris.Raft
open Iris.Service


////////////////////////////////////////
//     _                              //
//    / \   _ __ __ _ ___             //
//   / _ \ | '__/ _` / __|            //
//  / ___ \| | | (_| \__ \            //
// /_/   \_\_|  \__, |___/            //
//              |___/                 //
////////////////////////////////////////

type SubCommand =
  | Create
  | Start
  | Reset
  | Dump

type CLIArguments =
  | [<EqualsAssignment>]            Bind  of string
  | [<EqualsAssignment>]            Raft  of uint16
  | [<EqualsAssignment>]            Web   of uint16
  | [<EqualsAssignment>]            Git   of uint16
  | [<EqualsAssignment>]            Ws    of uint16
  | [<EqualsAssignment>]            Dir   of string
  | [<EqualsAssignment>]            Name  of string
  | [<Mandatory;MainCommand;CliPosition(CliPosition.First)>] Cmd   of SubCommand


  interface IArgParserTemplate with
    member self.Usage =
      match self with
        | Dir     _ -> "Project directory to place the config & database in"
        | Name    _ -> "Project name when using <create>"
        | Bind    _ -> "Specify a valid IP address."
        | Web     _ -> "Http server port."
        | Git     _ -> "Git server port."
        | Ws      _ -> "WebSocket port."
        | Raft    _ -> "Raft server port (internal)."
        | Cmd     _ -> "Either one of (--create, --start, --reset or --dump)"

let parser = ArgumentParser.Create<CLIArguments>()

let validateOptions (opts: ParseResults<CLIArguments>) =
  let ensureDir result =
    if opts.Contains <@ Dir @> |> not then
      Error.exitWith MissingStartupDir
    result

  let valid =
    match opts.GetResult <@ Cmd @> with
    | Create -> true
    | Start  -> ensureDir true
    | Reset  -> ensureDir true
    | Dump   -> ensureDir true

  if opts.GetResult <@ Cmd @> = Create then
    let name = opts.Contains <@ Name @>
    let dir  = opts.Contains <@ Dir @>
    let bind = opts.Contains <@ Bind @>
    let web  = opts.Contains <@ Web @>
    let raft = opts.Contains <@ Raft @>
    let git  = opts.Contains <@ Git @>
    let ws   = opts.Contains <@ Ws @>

    if not (name && bind && web && raft && ws) then
      printfn "Error: when creating a new configuration you must specify the following options:"
      if not name then printfn "    --name=<name>"
      if not dir  then printfn "    --dir=<directory>"
      if not bind then printfn "    --bind=<binding address>"
      if not web  then printfn "    --web=<web interface port>"
      if not git  then printfn "    --git=<git server port>"
      if not raft then printfn "    --raft=<raft port>"
      if not ws   then printfn "    --ws=<ws port>"
      Error.exitWith CliParseError

let parseLogLevel = function
  | "debug" -> Debug
  | "info"  -> Info
  | "warn"  -> Warn
  | _       -> Err

////////////////////////////////////////
//  ____  _        _                  //
// / ___|| |_ __ _| |_ ___            //
// \___ \| __/ _` | __/ _ \           //
//  ___) | || (_| | ||  __/           //
// |____/ \__\__,_|\__\___| manipulation....
////////////////////////////////////////

let parseHostString (str: string) =
  let trimmed = str.Trim().Split(' ')
  match trimmed with
    | [| id; hostname; hostspec |] as arr ->
      if hostspec.StartsWith("tcp://") then
        match hostspec.Substring(6).Split(':') with
          | [| addr; port |] -> Some (uint32 id, hostname, addr, int port)
          | _ -> None
      else None
    | _ -> None

let tryAppendEntry (ctx: RaftServer) str =
  match ctx.Append (LogMsg(Debug,str)) with
    | Some response ->
      printfn "Added Entry: %s Index: %A Term: %A"
        (string response.Id)
        response.Index
        response.Term
    | _ -> failwith "an error occurred"

let timeoutRaft (ctx: RaftServer) =
  ctx.ForceTimeout()

/////////////////////////////////////////
//   ____                      _       //
//  / ___|___  _ __  ___  ___ | | ___  //
// | |   / _ \| '_ \/ __|/ _ \| |/ _ \ //
// | |__| (_) | | | \__ \ (_) | |  __/ //
//  \____\___/|_| |_|___/\___/|_|\___| //
/////////////////////////////////////////

let private withTrim (token: string) (str: string) =
  let trimmed = trim str
  if trimmed.StartsWith(token) then
    let substr = trimmed.Substring(token.Length)
    Some <| trim substr
  else None

let private withEmpty (token: string) (str: string) =
  if trim str = token
  then Some ()
  else None

let (|Exit|_|)     str = withEmpty "exit" str
let (|Quit|_|)     str = withEmpty "quit" str
let (|Status|_|)   str = withEmpty "status" str
let (|Periodic|_|) str = withEmpty "step" str
let (|Timeout|_|)  str = withEmpty "timeout" str
let (|Leave|_|)    str = withEmpty "leave" str

let (|Append|_|)  str = withTrim "append" str
let (|Join|_|)    str = withTrim "join" str
let (|AddNode|_|) str = withTrim "addnode" str
let (|RmNode|_|)  str = withTrim "rmnode" str

let (|Interval|_|) (str: string) =
  let trimmed = str.Trim()
  match trimmed.Split(' ') with
  | [| "interval"; x |] ->
    try
      uint8 x |> Some
    with
      | _ -> None
  | _ -> None

let (|LogLevel|_|) (str: string) =
  let parsed = str.Trim().Split(' ')
  match parsed with
    | [| "log"; "debug" |] -> Some "debug"
    | [| "log"; "info" |]  -> Some "info"
    | [| "log"; "warn" |]  -> Some "warn"
    | [| "log"; "err" |]   -> Some "err"
    | _                  -> None


let trySetLogLevel (str: string) (context: RaftServer) =
  let config =
    { context.Options.RaftConfig with
        LogLevel = parseLogLevel str }
  context.Options <- updateEngine config context.Options

let trySetInterval i (context: RaftServer) =
  let config = { context.Options.RaftConfig with PeriodicInterval = i }
  context.Options <- updateEngine config context.Options

let tryJoinCluster (hst: string) (context: RaftServer) =
  let parsed =
    match split [| ' ' |] hst with
      | [| ip; port |] -> Some (ip, int port)
      | _            -> None

  match parsed with
    | Some(ip, port) -> context.JoinCluster(ip, port)
    | _ -> printfn "parameters %A could not be parsed" hst

let tryLeaveCluster (context: RaftServer) =
  context.LeaveCluster()

let tryAddNode (hst: string) (context: RaftServer) =
  let parsed =
    match split [| ' ' |] hst with
      | [| id; ip; port |] -> Some (id, ip, int port)
      | _                -> None

  match parsed with
    | Some(id, ip, port) ->
      match context.AddNode(id, ip, port) with
        | Some appended ->
          printfn "Added node: %A in entry %A" id (string appended.Id)
        | _ ->
          printfn "Could not add node %A" id
    | _ ->
      printfn "parameters %A could not be parsed" hst

let tryRmNode (hst: string) (context: RaftServer) =
    match context.RmNode(trim hst) with
      | Some appended ->
        printfn "Removed node: %A in entry %A" hst (string appended.Id)
      | _ ->
        printfn "Could not removed node %A " hst

////////////////////////////////////////
//  _                                 //
// | |    ___   ___  _ __             //
// | |   / _ \ / _ \| '_ \            //
// | |__| (_) | (_) | |_) |           //
// |_____\___/ \___/| .__/            //
//                  |_|               //
////////////////////////////////////////

let consoleLoop (context: IrisService) : unit =
  let kont = ref true
  let rec proc kontinue =
    printf "~> "
    let input = Console.ReadLine()
    match input with
      | LogLevel opt -> trySetLogLevel opt context.Raft
      | Interval   i -> trySetInterval i context.Raft
      | Exit         -> context.Stop(); kontinue := false
      | Periodic     -> context.Raft.Periodic()
      | Append ety   -> tryAppendEntry context.Raft ety
      | Join hst     -> tryJoinCluster hst context.Raft
      | Leave        -> tryLeaveCluster context.Raft
      | AddNode hst  -> tryAddNode hst context.Raft
      | RmNode hst   -> tryRmNode  hst context.Raft
      | Timeout      -> timeoutRaft context.Raft
      | Status       -> printfn "%s" <| context.Raft.ToString()
      | _            -> printfn "unknown command"
    if !kontinue then
      proc kontinue
  proc kont
