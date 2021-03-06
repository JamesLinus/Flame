﻿namespace Flame.Functional

open Flame
open Flame.Build
open Flame.Compiler
open Flame.Compiler.Statements
open LazyHelpers

/// Defines a functional-style accessor.
type FunctionalAccessor private(header : FunctionalMemberHeader,
                                declProp : IProperty,
                                accType : AccessorType,
                                baseMethods : IAccessor -> IMethod seq,
                                returnType : IType Lazy,
                                parameters : IParameter LazyArrayBuilder,
                                body : IMethod -> IStatement) as this =

    inherit FunctionalMemberBase(header, declProp.DeclaringType, declProp.IsStatic)

    let appliedBaseMethods = lazy (baseMethods this |> Seq.toArray)
    let appliedBody = lazy (body this)

    new(header : FunctionalMemberHeader, declProp : IProperty, accType : AccessorType) =
        let retType = if accType.Equals(AccessorType.GetAccessor) then
                          lazy (declProp.PropertyType)
                      else
                          lazy (PrimitiveTypes.Void)
        FunctionalAccessor(header, declProp, accType,
                           (fun _ -> Seq.empty),
                           retType,
                           new LazyArrayBuilder<IParameter>(),
                           (fun _ -> null))

    /// Gets this functional-style accessor's return type.
    member this.ReturnType = returnType.Value

    /// Gets this functional-style accessor's property type.
    member this.DeclaringProperty = declProp

    /// Gets this functional-style accessor's accessor type.
    member this.AccessorType = accType

    /// Gets this functional-style accessor's parameters.
    member this.Parameters = Seq.ofArray parameters.Value

    /// Gets this functional-style accessor's body statement, with lazy evaluation.
    member this.LazyBody = appliedBody

    /// Gets this functional-style accessor's body statement.
    member this.Body = evalLazy appliedBody

    /// Gets this functional-style accessor's base methods,
    /// with a lazy evaluation scheme.
    member this.LazyBaseMethods = appliedBaseMethods

    /// Gets this functional-style accessor's base methods.
    member this.BaseMethods = Seq.ofArray appliedBaseMethods.Value

    /// Sets this functional accessor's return type.
    member this.WithReturnType value =
        new FunctionalAccessor(header, declProp, accType, baseMethods, value, parameters, body)

    /// Adds this parameter to this functional accessor.
    member this.WithParameter value =
        new FunctionalAccessor(header, declProp, accType, baseMethods, returnType, parameters.Append value, body)

    /// Sets this functional-style accessor's body.
    member this.WithBody value =
        new FunctionalAccessor(header, declProp, accType, baseMethods, returnType, parameters, value)

    /// Sets this functional-style accessor's base methods.
    member this.WithBaseMethods value =
        new FunctionalAccessor(header, declProp, accType, value, returnType, parameters, body)

    interface IAccessor with
        member this.AccessorType = this.AccessorType
        member this.DeclaringProperty = this.DeclaringProperty

    interface IMethod with
        member this.ReturnType = this.ReturnType
        member this.Parameters = this.Parameters
        member this.IsConstructor = false
        member this.BaseMethods = this.BaseMethods

        member this.GenericParameters = Seq.empty

    interface IBodyMethod with
        member this.GetMethodBody() = this.Body
