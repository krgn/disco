[<FunScript.JS>]
module Iris.Web.Plugins

open FunScript
open FunScript.TypeScript

open Iris.Web.Types

[<JSEmit("""return window.IrisPlugins;""")>]
let viewPlugins () : IPluginSpec array = Array.empty 