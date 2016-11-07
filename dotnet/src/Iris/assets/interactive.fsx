#I __SOURCE_DIRECTORY__

#r "Argu.dll"
#r "Fable.Core.dll"
#r "Fleck.dll"
#r "FsCheck.dll"
#r "FSharp.Configuration.dll"
#r "FSharp.Control.AsyncSeq.dll"
#r "FSharpx.Async.dll"
#r "FSharpx.Collections.dll"
#r "FSharpx.Extras.dll"
#r "Fuchu.dll"
#r "Fuchu.FsCheck.dll"
#r "Gallio40.dll"
#r "Gallio.dll"
#r "Iris.Serialization.dll"
#r "LibGit2Sharp.dll"
#r "MbUnit40.dll"
#r "MbUnit.dll"
#r "nunit.framework.dll"
#r "SharpYaml.dll"
#r "Suave.dll"
#r "xunit.abstractions.dll"
#r "xunit.assert.dll"
#r "xunit.core.dll"
#r "xunit.execution.desktop.dll"
#r "ZeroMQ.dll"
#r "System.Management"

//#r "Iris.Core.dll"
#load
    "../../../Iris/Core/Constants.fs"
    "../../../Iris/Core/Either.fs"
    "../../../Iris/Core/Serialization.fs"
    "../../../Iris/Core/Error.fs"
    "../../../Iris/Core/Id.fs"
    "../../../Iris/Core/IpAddress.fs"
    "../../../Iris/Core/Aliases.fs"
    "../../../Iris/Core/Util.fs"
    "../../../Iris/Core/User.fs"
    "../../../Iris/Core/Session.fs"
    "../../../Iris/Core/Color.fs"
    "../../../Iris/Core/LogLevel.fs"
    "../../../Iris/Core/Git.fs"
    "../../../Iris/Core/IOBox.fs"
    "../../../Iris/Core/Patch.fs"
    "../../../Iris/Core/Cue.fs"
    "../../../Iris/Core/CueList.fs"
    "../../../Iris/Raft/Validation.fs"
    "../../../Iris/Raft/Continue.fs"
    "../../../Iris/Raft/Node.fs"
    "../../../Iris/Core/StateMachine.fs"
    "../../../Iris/Raft/LogEntry.fs"
    "../../../Iris/Raft/Log.fs"
    "../../../Iris/Raft/Types.fs"
    "../../../Iris/Raft/Raft.fs"
    "../../../Iris/Core/Region.fs"
    "../../../Iris/Core/Signal.fs"
    "../../../Iris/Core/Display.fs"
    "../../../Iris/Core/Task.fs"
    "../../../Iris/Core/ViewPort.fs"
    "../../../Iris/Core/Vvvv.fs"
    "../../../Iris/Core/Yaml.fs"
    "../../../Iris/Core/Config.fs"
    "../../../Iris/Core/Project.fs"
    "../../../Iris/Core/Uri.fs"
    "../../../Iris/Service/GitDaemon.fs"
    "../../../Iris/Service/Request.fs"
    "../../../Iris/Service/Zmq/Rep.fs"
    "../../../Iris/Service/Zmq/Req.fs"
    "../../../Iris/Service/RaftAppState.fs"
    "../../../Iris/Service/Zmq/ZmqUtils.fs"
    "../../../Iris/Service/Persistence.fs"
    "../../../Iris/Service/Utilities.fs"
    "../../../Iris/Service/Stm.fs"
    "../../../Iris/Service/RaftServer.fs"
    "../../../Iris/Service/AssetServer.fs"
    "../../../Iris/Service/WebSocket.fs"
    "../../../Iris/Service/IrisService.fs"
    "../../../Iris/Service/CommandLine.fs"

open System
open System.IO

open Iris.Service

let assetServer = AssetServer()
let socketServer = WebSocket.WsServer()