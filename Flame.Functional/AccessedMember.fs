﻿namespace Flame.Functional

open Flame
open Flame.Build
open Flame.Compiler

/// Defines a way of classifying type members.
type AccessedMember<'a> =
    /// The given type member is an instance member.
    | Instance of 'a
    /// The given type member is an extension member.
    | Extension of 'a
    /// The given type member is a static member.
    | Static of 'a

    member this.Member = 
        match this with
        | Instance  x -> x
        | Extension x -> x
        | Static    x -> x

    /// Gets a user-friendly member prefix for this accessed member.
    member this.MemberPrefix =
        match this with
        | Instance  _ -> "an instance"
        | Extension _ -> "an extension"
        | Static    _ -> "a static"

/// Defines a number of categories for types
/// whose type members are accessed.
type AccessedExpression =
    /// The accessed type is a reference type.
    | Reference of IExpression
    /// The accessed type is a value type.
    | Value of IExpression
    /// The accessed type is generic.
    | Generic of IExpression
    /// No expression is accessed: the type member is static.
    | Global of IType

    member this.Type =
        match this with
        | Reference  x -> x.Type
        | Value      x -> x.Type
        | Generic    x -> x.Type
        | Global     x -> x

    /// Gets a user-friendly member prefix for this accessed member.
    member this.Describe (renderer : TypeRenderer) =
        match this with
        | Reference  x -> "a reference type ('" + (renderer.Name(x.Type)) + "')"
        | Value      x -> "a value type ('" + (renderer.Name(x.Type)) + "')"
        | Generic    x -> "a type parameter ('" + (renderer.Name(x.Type)) + "')"
        | Global     x -> "a type name ('" + (renderer.Name(x)) + "')"