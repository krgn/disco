namespace Iris.Core

#if FABLE_COMPILER

open Fable.Core
open Fable.Import
open Fable.Import.JS
open Iris.Core.FlatBuffers

#else

open FlatBuffers

#endif

//  ____  _
// | __ )(_)_ __   __ _ _ __ _   _
// |  _ \| | '_ \ / _` | '__| | | |
// | |_) | | | | | (_| | |  | |_| |
// |____/|_|_| |_|\__,_|_|   \__, |
//                           |___/

[<RequireQualifiedAccess>]
module Binary =

  let createBuffer (bytes: byte[]) : ByteBuffer =
#if FABLE_COMPILER
    ByteBuffer.Create(bytes)
#else
    ByteBuffer(bytes)
#endif

  let inline encode (value : ^t when ^t : (member ToBytes : unit -> byte[])) =
    (^t : (member ToBytes : unit -> byte[]) value)

  let inline decode< ^t when ^t : (static member FromBytes : byte[] -> Either<IrisError, ^t>)>
                                  (bytes: byte[]) :
                                  Either<IrisError, ^t > =
    try
      (^t : (static member FromBytes : byte[] -> Either<IrisError, ^t>) bytes)
    with
      | exn ->
        ((typeof< ^t >).Name + ".FromBytes", exn.Message)
        |> ParseError
        |> Either.fail

  let inline toOffset< ^t, ^a when ^a : (member ToOffset : FlatBufferBuilder -> Offset< ^t >)>
                     (builder: FlatBufferBuilder)
                     (thing: ^a)
                     : Offset< ^t > =
    (^a : (member ToOffset : FlatBufferBuilder -> Offset< ^t >) (thing,builder))

  let inline buildBuffer< ^t, ^a when ^a : (member ToOffset : FlatBufferBuilder -> Offset< ^t >)> (thing: ^a) : byte[] =
#if FABLE_COMPILER
    let builder = FlatBufferBuilder.Create(1)
    let offset = toOffset builder thing
    builder.Finish(offset)
    builder.SizedByteArray()
#else
    let builder = FlatBufferBuilder(1)
    let offset = toOffset builder thing
    builder.Finish(offset.Value)
    builder.SizedByteArray()
#endif

// __   __              _
// \ \ / /_ _ _ __ ___ | |
//  \ V / _` | '_ ` _ \| |
//   | | (_| | | | | | | |
//   |_|\__,_|_| |_| |_|_|

#if FABLE_COMPILER
#else

[<RequireQualifiedAccess>]
module Yaml =
  open SharpYaml.Serialization

  let inline encode< ^t when ^t : (member ToYaml : Serializer -> string)> (thing: ^t) =
    let serializer = Serializer()
    (^t : (member ToYaml : Serializer -> string) thing,serializer)

  let inline decode< ^err, ^t when ^t : (static member FromYaml : string -> Either< ^err, ^t >)> (str: string) =
    (^t : (static member FromYaml : string -> Either< ^err, ^t >) str)

  let inline toYaml< ^a, ^t when ^t : (member ToYamlObject : unit -> ^a)> (thing: ^t) : ^a =
    (^t : (member ToYamlObject : unit -> ^a) thing)

  let inline fromYaml< ^err, ^a, ^t when ^t : (static member FromYamlObject : ^a -> Either< ^err, ^t >)> (thing: ^a) =
    (^t : (static member FromYamlObject : ^a -> Either< ^err, ^t >) thing)

  let inline arrayToMap< ^err, ^i, ^a, ^t when ^t : (static member FromYamlObject : ^a -> Either< ^err, ^t >)
                                          and  ^t : (member Id : ^i)
                                          and  ^i : comparison> (things: ^a array)
                                          : Either< ^err, Map< ^i, ^t > > =
    Array.fold
      (fun (m: Either< ^err, Map< ^i, ^t > >) (yml: ^a) -> either {
        let! things = m
        let! thing = fromYaml yml
        let id = (^t : (member Id : ^i) thing)
        return Map.add id thing things
      })
      (Right Map.empty)
      things

#endif
