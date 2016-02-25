namespace Iris.Service.Groups

open System
open Nessos.FsPickler
open Vsync

[<AutoOpen>]
module Base =

  type Handler<'a> = 'a -> unit

  type RawHandler = delegate of byte[] -> unit

  let private mkRawHandler (f : byte[] -> unit) = new RawHandler(f)

  type IEnum =
    abstract member ToInt : unit -> int
    
  type IrisGroup<'action when 'action :> IEnum>(name : string) =
    inherit Vsync.Group(name)

    let pickler = FsPickler.CreateBinarySerializer()

    member self.Send<'data>(action : 'action, thing : 'data) : unit =
      self.Send(action.ToInt(), pickler.Pickle(thing))

    member self.ToBytes<'data>(thing : 'data) : byte[] =
      pickler.Pickle(thing)

    member self.FromBytes<'data>(data : byte[]) : 'data =
      pickler.UnPickle<'data>(data)

    member self.CheckpointMaker(handler : Vsync.View -> unit) =
      self.RegisterMakeChkpt(new Vsync.ChkptMaker(handler))

    member self.CheckpointLoader<'data>(handler : 'data -> unit) =
      let wrapped = fun data -> self.FromBytes(data) |> handler
       in self.RegisterLoadChkpt(mkRawHandler wrapped)

    member self.SendCheckpoint<'data>(thing : 'data) =
      self.SendChkpt(self.ToBytes<'data>(thing))

    member self.DoneCheckpoint() =
      self.EndOfChkpt()

    member self.AddViewHandler(handler : Vsync.View -> unit) =
      self.ViewHandlers <- self.ViewHandlers + new Vsync.ViewHandler(handler)

    member self.AddInitializer(handler : unit -> unit) =
      self.RegisterInitializer(new Vsync.Initializer(handler))

    member self.AddRawHandler(action : 'action, handler : Handler<byte[]>) =
      let wrapped = mkRawHandler handler
      in self.Handlers.[action.ToInt()] <- self.Handlers.[action.ToInt()] + wrapped

    member self.AddHandler<'data>(action : 'action, handler : Handler<'data>) =
      let wrapped = mkRawHandler <| fun data ->
        handler <| self.FromBytes(data)
      in self.Handlers.[action.ToInt()] <- self.Handlers.[action.ToInt()] + wrapped
