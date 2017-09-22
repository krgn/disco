namespace Iris.Web.Inspectors

open System
open System.Collections.Generic
open Fable.Import
open Fable.Import.React
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Core
open Fable.Core.JsInterop
open Fable.PowerPack
open Elmish.React
open Iris.Core
open Iris.Web.Core
open Iris.Web.Helpers
open Iris.Web.Types
open State

module SessionInspector =

  let render dispatch (model: Model) (session: Session) =
    Common.render dispatch model "Session" [
      Common.stringRow "Id"         (string session.Id)
      Common.stringRow "IP Address" (string session.IpAddress)
      Common.stringRow "User Agent" (string session.UserAgent)
    ]
