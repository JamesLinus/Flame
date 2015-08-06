﻿namespace Flame.Functional

open Flame
open Flame.Build
open Flame.Compiler
open LazyHelpers

/// A header for function members.
type FunctionalMemberHeader(name : string, attrs : IAttribute LazyArrayBuilder) =
    new(name) =
        FunctionalMemberHeader(name, new LazyArrayBuilder<IAttribute>())
    new(name, attrs : seq<IAttribute>) =
        FunctionalMemberHeader(name, new LazyArrayBuilder<IAttribute>(attrs))

    /// Gets this functional-style member header's name.
    member this.Name = name
    /// Gets this functional-style member header's attributes.
    member this.Attributes = Seq.ofArray attrs.Value

    /// Adds an attribute to this functional-style member header.
    member this.WithAttribute value =
        new FunctionalMemberHeader(name, attrs.Append value)

/// Provides common functionality for objects that support
/// constructing types in a functional fashion.
type FunctionalType private(header : FunctionalMemberHeader, declNs : INamespace, 
                            baseTypes : IType LazyArrayBuilder, nestedTypes : LazyApplicationArray<INamespace, IType>, 
                            genericParams : LazyApplicationArray<IGenericMember, IGenericParameter>,
                            methods : LazyApplicationArray<IType, IMethod>, properties : LazyApplicationArray<IType, IProperty>, 
                            fields : LazyApplicationArray<IType, IField>, ns : LazyApplicationArray<INamespaceBranch, INamespaceBranch>) as this =

    let appliedNestedTypes = nestedTypes.ApplyLazy this
    let appliedGenericParams = genericParams.ApplyLazy this
    let appliedMethods = methods.ApplyLazy this
    let appliedProperties = properties.ApplyLazy this
    let appliedFields = fields.ApplyLazy this
    let appliedNs = ns.ApplyLazy this
    let ctorPartition = appliedMethods |~ Array.partition (fun x -> x.IsConstructor)

    new(header, declNs) =
        FunctionalType(header, declNs,
                       new LazyArrayBuilder<IType>(), new LazyApplicationArray<INamespace, IType>(),
                       new LazyApplicationArray<IGenericMember, IGenericParameter>(),
                       new LazyApplicationArray<IType, IMethod>(), new LazyApplicationArray<IType, IProperty>(),
                       new LazyApplicationArray<IType, IField>(), new LazyApplicationArray<INamespaceBranch, INamespaceBranch>())
    new(name, declNs) =
        FunctionalType(new FunctionalMemberHeader(name), declNs)
    
    /// Gets this functional type's base types.
    member this.BaseTypes         = baseTypes.Value
    /// Gets this functional type's nested types.
    member this.NestedTypes       = evalLazy appliedNestedTypes
    /// Gets this functional type's generic parameters.
    member this.GenericParameters = evalLazy appliedGenericParams
    /// Gets this functional type's methods.
    member this.Methods           = snd ctorPartition.Value
    /// Gets this functional type's constructors.
    member this.Constructors      = fst ctorPartition.Value
    /// Gets this functional type's properties.
    member this.Properties        = evalLazy appliedProperties
    /// Gets this functional type's fields.
    member this.Fields            = evalLazy appliedFields
    /// Gets this functional type's members.
    member this.Members           = Seq.concat [ Seq.cast<ITypeMember> appliedMethods.Value; Seq.cast<ITypeMember> this.Properties; Seq.cast<ITypeMember> this.Fields ]
                                               |> Array.ofSeq

    /// Gets this functional type's nested namespaces.
    member this.NestedNamespaces  = evalLazy appliedNs

    /// Sets this type's header.
    member this.WithHeader newHeader = 
        new FunctionalType(newHeader, declNs, baseTypes, nestedTypes, genericParams, methods, properties, fields, ns)

    /// Adds an attribute to this functional-style type.
    member this.WithAttribute attr =
        new FunctionalType(header.WithAttribute attr, declNs, baseTypes, nestedTypes, genericParams, methods, properties, fields, ns)

    /// Adds a base type to this functional-style type.
    member this.WithBaseType baseType =
        new FunctionalType(header, declNs, baseTypes.Append baseType, nestedTypes, genericParams, methods, properties, fields, ns)

    /// Adds a nested type to this functional-style type.
    member this.WithNestedType nestedType =
        new FunctionalType(header, declNs, baseTypes, nestedTypes.Append nestedType, genericParams, methods, properties, fields, ns)

    /// Adds a nested namespace to this functional-style type.
    member this.WithNestedNamespace value =
        new FunctionalType(header, declNs, baseTypes, nestedTypes, genericParams, methods, properties, fields, ns.Append value)

    /// Adds a generic parameter to this functional-style type.
    member this.WithGenericParameter value =
        new FunctionalType(header, declNs, baseTypes, nestedTypes, genericParams.Append value, methods, properties, fields, ns)

    /// Adds a method to this functional-style type.
    member this.WithMethod value =
        let newMethods = methods.Append value
        new FunctionalType(header, declNs, baseTypes, nestedTypes, genericParams, newMethods, properties, fields, ns)

    /// Adds a property to this functional-style type.
    member this.WithProperty value =
        new FunctionalType(header, declNs, baseTypes, nestedTypes, genericParams, methods, properties.Append value, fields, ns)

    /// Adds a field to this functional-style type.
    member this.WithField value =
        new FunctionalType(header, declNs, baseTypes, nestedTypes, genericParams, methods, properties, fields.Append value, ns)

    interface IFunctionalNamespace with
        /// Adds a nested type to this functional-style type.
        member this.WithType func =
            (this.WithNestedType func) :> IFunctionalNamespace

        member this.WithNamespace value =
            (this.WithNestedNamespace value) :> IFunctionalNamespace

    interface IMember with
        member this.Name = header.Name
        member this.FullName = MemberExtensions.CombineNames(declNs.FullName, header.Name)
        member this.GetAttributes() = header.Attributes

    interface IType with
        member this.AsContainerType() = null
        member this.IsContainerType = false
        member this.DeclaringNamespace = declNs
        
        member this.MakeArrayType rank = new DescribedArrayType(this :> IType, rank) :> IArrayType
        member this.MakePointerType kind = new DescribedPointerType(this :> IType, kind) :> IPointerType
        member this.MakeVectorType dims = new DescribedVectorType(this :> IType, dims) :> IVectorType

        member this.GetGenericDeclaration() = this :> IType
        member this.MakeGenericType tArgs = new DescribedGenericTypeInstance(this :> IType, tArgs) :> IType
        member this.GetGenericArguments() = Seq.empty
        member this.GetGenericParameters() = Seq.ofArray this.GenericParameters

        member this.GetBaseTypes() = this.BaseTypes
        member this.GetMethods() = this.Methods
        member this.GetConstructors() = this.Constructors
        member this.GetProperties() = this.Properties
        member this.GetFields() = this.Fields
        member this.GetMembers() = this.Members

        member this.GetDefaultValue() = null

    interface INamespace with
        member this.DeclaringAssembly = declNs.DeclaringAssembly
        member this.GetTypes() = this.NestedTypes

    interface INamespaceBranch with
        member this.GetNamespaces() = Seq.ofArray this.NestedNamespaces

/// Defines a base class for functional-style members.
[<AbstractClass>]
type FunctionalMemberBase(header : FunctionalMemberHeader, declType : IType,
                          isStatic : bool) =

    /// Gets this functional-style member's declaring type.
    member this.DeclaringType  = declType

    /// Figures out whether this functional-style member is static.
    member this.IsStatic       = isStatic

    interface IMember with
        member this.Name = header.Name
        member this.FullName = MemberExtensions.CombineNames(declType.FullName, header.Name)
        member this.GetAttributes() = header.Attributes

    interface ITypeMember with
        member this.DeclaringType = declType
        member this.IsStatic = isStatic