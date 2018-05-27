#r "packages/Suave/lib/net461/Suave.dll"

open System
open System.Text
open System.IO
 
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful

let OUT ctx r =
    let r' = (r |> string) + "\n"
    OK r' ctx

let ok_txt (f:HttpContext->'a) =
    fun ctx -> 
        let r = f ctx
        let r' = (r.ToString()) + "\n"
        OK r' ctx

let app =
    choose [
        path "/guid" >=> ok_txt (fun ctx -> Guid.NewGuid())
        path "/random" >=> ok_txt (fun ctx -> Random().Next())
        path "/now" >=> ok_txt (fun ctx -> DateTime.UtcNow)
    ]

let publicBinding = Suave.Http.HttpBinding.createSimple HTTP "0.0.0.0" 8080
let config = { defaultConfig with bindings = [publicBinding]; maxOps=1000 }
startWebServer config app
