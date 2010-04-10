﻿module IronJS.Runtime.Environment

open IronJS
open IronJS.Utils
open IronJS.Tools
open System.Dynamic
open System.Collections.Generic

type private AnalyzeFunc = Ast.Types.Scope -> ClrType -> ClrType list -> Ast.Types.Scope
type private ExprGenFunc = ClrType -> Ast.Types.Scope -> Ast.Types.Node -> (EtLambda * ClrType list)

(**)
let rec private calculateHashAndTypes types (hash:int ref) = 
  match types with
  | [] -> []
  | x::xsTypes ->
    if x = Constants.clrDouble then
      hash := (37 * !hash + Constants.clrDoubleHashCode)
      Constants.clrDouble :: calculateHashAndTypes xsTypes hash
      
    elif x = Constants.clrString then
      hash := (37 * !hash + Constants.clrStringHashCode)
      Constants.clrString :: calculateHashAndTypes xsTypes hash
      
    elif x = Runtime.Core.objectTypeDef then
      hash := (37 * !hash + Runtime.Core.objectTypeDefHashCode)
      Runtime.Core.objectTypeDef :: calculateHashAndTypes xsTypes hash

    elif x = Runtime.Function.functionTypeDef then
      hash := (37 * !hash + Runtime.Function.functionTypeDefHashCode)
      Runtime.Function.functionTypeDef :: calculateHashAndTypes xsTypes hash

    else
      hash := (37 * !hash + Constants.clrDynamicHashCode)
      Constants.clrDynamic :: calculateHashAndTypes xsTypes hash

(**)
let private compareTypes (a:'a list) (b:'a list) =

  if not (a.Length = b.Length) then
    false
  else
    let rec compareTypes' a b =
      match a with
      | []      -> true
      | xA::xsA ->
        match b with
        | xB::xsB -> if xA = xB then compareTypes' xsA xsB else false
        | _       -> failwith "Should never happen"

    compareTypes' a b

(**)
type DelegateCell(ast:Ast.Types.Node, closureType:ClrType, types:ClrType list) =
  let hashRef = ref (37 * closureType.GetHashCode() + ast.GetHashCode())
  let uniformTypes = calculateHashAndTypes types hashRef
  let hashCode = !hashRef

  member self.Ast = ast
  member self.Types = uniformTypes
  member self.ClosureType = closureType

  override self.GetHashCode() = hashCode
  override self.Equals obj = 
    match obj with
    | :? DelegateCell as cell ->  
      if cell.Ast = self.Ast && cell.ClosureType = self.ClosureType
        then compareTypes cell.Types self.Types
        else false
    | _ -> false

(*The currently executing environment*)
and Environment (scopeAnalyzer:AnalyzeFunc, exprGenerator:ExprGenFunc) =
  let jitCache = new Dictionary<DelegateCell, System.Delegate * ClrType list>()

  //Implementation of IEnvironment interface
  interface Runtime.Core.IEnvironment with
    member self.GetDelegate ast closureType types =
      let cell = new DelegateCell(ast, closureType, types)
      match self.GetCachedDelegate cell with
      | Some(func) -> func
      | None -> self.CacheCompiledDelegate cell (self.Compile ast closureType types)

  //Private members
  member private self.GetCachedDelegate cell =
    let success, func = jitCache.TryGetValue(cell)
    if success then Some(func) else None

  member private self.CacheCompiledDelegate cell func =
    jitCache.[cell] <- func
    func

  member private self.Compile ast closureType types =
    match ast with
    | Ast.Types.Node.Function(scope, body) -> 
      let lambda, paramTypes = (exprGenerator closureType (scopeAnalyzer scope closureType types) body)
      lambda.Compile(), paramTypes

    | _ -> failwith "Can only compile Ast.Types.Node.Function"