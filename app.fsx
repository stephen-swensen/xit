#r "packages/Suave/lib/net461/Suave.dll"

open System
open System.Text
open System.IO
 
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful

module Utils =
    ///Coalesce option value
    let inline (|?) x y = defaultArg x y
    ///Coalesce null value
    let inline (|??) (x:'a) y = if obj.ReferenceEquals(x, Unchecked.defaultof<'a>) then y else x

    ///High precedence, right associative backwards pipe
    let inline (^|) f a = f a

    /// Converts the result of a TryParse() method to an Option.
    let inline tryParse (s : string) : ^o option =
        let mutable o = Unchecked.defaultof<(^o)>
        if (^o : (static member TryParse : string * ^o byref -> bool) (s, &o)) then
            Some o
        else 
            None

    let (|Int|_|) x = tryParse x : int option
    let (|Int64|_|) x = tryParse x : int64 option
    let (|Guid|_|) x = tryParse x : Guid option
    let (|Double|_|) x = tryParse x : Double option
    let (|Bool|_|) x = tryParse x : bool option
    let (|Decimal|_|) x = tryParse x : Decimal option

open Utils

module SuaveEx =
    let getRequestQueryParams ctx =
        let query = 
            ctx.request.query 
            |> List.map (fun (k,v) -> k, match v with Some(v) -> v | None -> "")
            |> List.filter (fun (k,v) -> k <> "")
        printfn "query: %A" query
        query

    let getRequestQueryParamArray ctx key = 
        let r = 
            getRequestQueryParams ctx
            |> Seq.choose (fun (k,v) -> 
                match k with
                | k when k = key && v |> String.isEmpty |> not -> Some(v) 
                | _ -> None)
            |> Seq.toList
        printfn "key: %s, values: %A" key r
        r

    let opts ctx (key:string) =
        let x = getRequestQueryParamArray ctx key
        match x with
        | [] -> None
        | _ -> Some(x)

    let opt ctx (key:string) =
        match opts ctx key with
        | Some(x::_) -> 
            printfn "key: %s, value: %s" key x
            Some(x)
        | _ -> None

    let content ctx =
        match ctx.request.rawForm with
        | [||] -> null
        | raw -> System.Text.UTF8Encoding(false).GetString(raw)

open SuaveEx

module App =
    let ok_txt (f:HttpContext->'a) =
        fun ctx -> 
            let r = f ctx
            let r' = (r.ToString()) + "\n"
            OK r' ctx

    let ok_txt_n (f:HttpContext->'a) =
        fun ctx -> 
            let n = opt ctx "n" |? "1" |> int
            let r =
                [for x in 1..n -> (f ctx).ToString()]
                |> String.concat "\n"
            let r' = r + "\n"
            OK r' ctx

    let app =
        choose [
            path "/guid" >=> ok_txt_n (fun ctx -> 
                let format = opt ctx "f" |? "D"
                Guid.NewGuid().ToString(format))
            path "/random" >=> ok_txt_n (fun ctx -> Random().Next())
            path "/now" >=> ok_txt_n (fun ctx -> DateTime.UtcNow)
            path "/echo" >=> ok_txt_n content
            path "/wc" >=> ok_txt (fun ctx ->
                let input = content ctx
                let lines = input |> Seq.filter (fun c -> c = '\n') |> Seq.length 
                let words = input.Split([|' '; '\n'|], StringSplitOptions.RemoveEmptyEntries) |> Seq.length
                let letters = input.Length
                sprintf "\t%i\t%i\t%i" lines words letters)
        ]

let errorHandler (ex: Exception) msg =
    let r = sprintf "%s: %A" msg ex
    let r = System.Text.UTF8Encoding(false).GetBytes(r + "\n")
    Response.response HTTP_500 r

let publicBinding = Suave.Http.HttpBinding.createSimple HTTP "0.0.0.0" 9123
let config = { defaultConfig with bindings = [publicBinding]; maxOps=1000; errorHandler=errorHandler }
startWebServer config App.app
