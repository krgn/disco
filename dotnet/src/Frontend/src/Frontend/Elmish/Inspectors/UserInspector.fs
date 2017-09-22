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

module UserInspector =

  let render dispatch (model: Model) (user: User) =
    Common.render dispatch model "User" [
      Common.stringRow "Id"         (string user.Id)
      Common.stringRow "User Name"  (string user.UserName)
      Common.stringRow "First Name" (string user.FirstName)
      Common.stringRow "Last Name"  (string user.LastName)
      Common.stringRow "Email"      (string user.Email)
      Common.stringRow "Joined"     (string user.Joined)
      Common.stringRow "Created"    (string user.Created)
    ]
