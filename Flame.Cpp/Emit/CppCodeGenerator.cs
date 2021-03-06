﻿using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class CppCodeGenerator : IUnmanagedCodeGenerator, IForeachCodeGenerator,
        IExceptionCodeGenerator, IForCodeGenerator, IInitializingCodeGenerator,
        IContractCodeGenerator, IWhileCodeGenerator, IDoWhileCodeGenerator
    {
        public CppCodeGenerator(IMethod Method, ICppEnvironment Environment)
        {
            this.Method = Method;
            this.Environment = Environment;
            this.LocalManager = new CppLocalManager(this);
            this.retVar = new Lazy<IEmitVariable>(() => LocalManager.DeclareNew(new UniqueTag(), new DescribedVariableMember("result", Method.ReturnType)));
        }

        public MethodContract Contract
        {
            get
            {
                if (Method is CppMethod)
                {
                    return ((CppMethod)Method).Contract;
                }
                else
                {
                    return new MethodContract(this, Enumerable.Empty<ICppBlock>(), Enumerable.Empty<ICppBlock>());
                }
            }
        }

        #region Properties

        public IMethod Method { get; private set; }
        public ICppEnvironment Environment { get; private set; }
        public Func<INamespace, IConverter<IType, string>> TypeNamer { get { return Environment.TypeNamer; } }

        #endregion

        #region Block Generators

        public ICodeBlock EmitBreak(UniqueTag Tag)
        {
            return new KeywordStatementBlock(this, "break");
        }

        public ICodeBlock EmitContinue(UniqueTag Tag)
        {
            return new KeywordStatementBlock(this, "continue");
        }

        public ICodeBlock EmitTagged(UniqueTag Tag, ICodeBlock Contents)
        {
            return null;
        }

        private ICodeBlock EmitUnionBlock(ICppBlock UnionBlock, params ICppBlock[] UnionItems)
        {
            var unionDecls = CppBlock.HoistUnionDeclarations(UnionItems);
            if (unionDecls.Count > 0)
            {
                return EmitSequence(new CppBlock(this, unionDecls), UnionBlock);
            }
            else
            {
                return UnionBlock;
            }
        }

        public ICodeBlock EmitDoWhile(UniqueTag Tag, ICodeBlock Body, ICodeBlock Condition)
        {
            var cond = (ICppBlock)Condition;
            var body = (ICppBlock)Body;
            var doWhileBlock = new DoWhileBlock(cond, body);

            return EmitUnionBlock(doWhileBlock, cond, body);
        }

        public ICodeBlock EmitIfElse(ICodeBlock Condition, ICodeBlock IfBody, ICodeBlock ElseBody)
        {
            var cond = (ICppBlock)Condition;
            var ifBody = (ICppBlock)IfBody;
            var elseBody = (ICppBlock)ElseBody;
            var ifElseBlock = new IfElseBlock(this, cond, ifBody, elseBody);

            return EmitUnionBlock(ifElseBlock, cond, ifBody, elseBody);
        }

        public ICodeBlock EmitPop(ICodeBlock Value)
        {
            return new ExpressionStatementBlock((ICppBlock)Value);
        }

        public ICodeBlock EmitReturn(ICodeBlock Value)
        {
            return new ContractReturnBlock(this, Value as ICppBlock);
        }

        public ICodeBlock EmitSequence(ICodeBlock First, ICodeBlock Second)
        {
            return new CppBlock(this, CppBlock.InsertSequenceDeclarations((ICppBlock)First, (ICppBlock)Second));
        }

        public ICodeBlock EmitVoid()
        {
            return new EmptyBlock(this);
        }

        public ICodeBlock EmitWhile(UniqueTag Tag, ICodeBlock Condition, ICodeBlock Body)
        {
            var cond = (ICppBlock)Condition;
            var body = (ICppBlock)Body;
            var whileBlock = new WhileBlock(cond, body);

            return EmitUnionBlock(whileBlock, cond, body);
        }

        #endregion

        #region Math

        public ICodeBlock EmitBinary(ICodeBlock A, ICodeBlock B, Operator Op)
        {
            var left = (ICppBlock)A;
            var right = (ICppBlock)B;
            if (BinaryOperation.IsSupported(Op))
            {
                return new BinaryOperation(this, left, Op, right);
            }
            else
            {
                return null; // Not supported
            }
        }

        public ICodeBlock EmitUnary(ICodeBlock Value, Operator Op)
        {
            var cppBlock = (ICppBlock)Value;
            if (Op.Equals(Operator.Hash))
            {
                return new HashBlock(cppBlock);
            }
            else if (Op.Equals(Operator.Box))
            {
                // TODO: create a specialized boxing conversion node type.
                return new ConversionBlock(
                    this, cppBlock,
                    cppBlock.Type.MakePointerType(PointerKind.ReferencePointer));
            }
            else if (UnaryOperation.IsSupported(Op))
            {
                return new UnaryOperation(this, cppBlock, Op);
            }
            else
            {
                return null; // Not supported.
            }
        }

        #endregion

        #region Literals

        public ICodeBlock EmitBoolean(bool Value)
        {
            return new LiteralBlock(this, Value);
        }

        public ICodeBlock EmitInt16(short Value)
        {
            return new LiteralBlock(this, Value);
        }

        public ICodeBlock EmitInt32(int Value)
        {
            return new LiteralBlock(this, Value);
        }

        public ICodeBlock EmitInt64(long Value)
        {
            return new LiteralBlock(this, Value);
        }

        public ICodeBlock EmitInt8(sbyte Value)
        {
            return new LiteralBlock(this, Value);
        }

        public ICodeBlock EmitUInt16(ushort Value)
        {
            return new LiteralBlock(this, Value);
        }

        public ICodeBlock EmitUInt32(uint Value)
        {
            return new LiteralBlock(this, Value);
        }

        public ICodeBlock EmitUInt64(ulong Value)
        {
            return new LiteralBlock(this, Value);
        }

        public ICodeBlock EmitUInt8(byte Value)
        {
            return new LiteralBlock(this, Value);
        }

        public ICodeBlock EmitNull()
        {
            return new LiteralBlock(this, "nullptr", PrimitiveTypes.Null);
        }

        public ICodeBlock EmitChar(char Value)
        {
            return new CharLiteral(this, Value);
        }

        public ICodeBlock EmitString(string Value)
        {
            return new StringLiteral(this, Value);
        }

        public ICodeBlock EmitFloat32(float Value)
        {
            return new FloatLiteralBlock(this, Value);
        }

        public ICodeBlock EmitFloat64(double Value)
        {
            return new DoubleLiteralBlock(this, Value);
        }

        public ICodeBlock EmitBit16(ushort Value)
        {
            return EmitTypeBinary(EmitUInt16(Value), PrimitiveTypes.Bit16, Operator.StaticCast);
        }

        public ICodeBlock EmitBit32(uint Value)
        {
            return EmitTypeBinary(EmitUInt32(Value), PrimitiveTypes.Bit32, Operator.StaticCast);
        }

        public ICodeBlock EmitBit64(ulong Value)
        {
            return EmitTypeBinary(EmitUInt64(Value), PrimitiveTypes.Bit64, Operator.StaticCast);
        }

        public ICodeBlock EmitBit8(byte Value)
        {
            return EmitTypeBinary(EmitUInt8(Value), PrimitiveTypes.Bit8, Operator.StaticCast);
        }

        public ICodeBlock EmitBit(BitValue Value)
        {
            int size = Value.Size;
            if (size == 8)
                return EmitBit8(Value.ToInteger().ToUInt8());
            else if (size == 16)
                return EmitBit16(Value.ToInteger().ToUInt16());
            else if (size == 32)
                return EmitBit32(Value.ToInteger().ToUInt32());
            else if (size == 64)
                return EmitBit64(Value.ToInteger().ToUInt64());
            else
                throw new NotSupportedException("Unsupported bit size: " + size);
        }

        public ICodeBlock EmitInteger(IntegerValue Value)
        {
            var spec = Value.Spec;
            if (spec.Equals(IntegerSpec.Int8))
                return EmitInt8(Value.ToInt8());
            else if (spec.Equals(IntegerSpec.Int16))
                return EmitInt16(Value.ToInt16());
            else if (spec.Equals(IntegerSpec.Int32))
                return EmitInt32(Value.ToInt32());
            else if (spec.Equals(IntegerSpec.Int64))
                return EmitInt64(Value.ToInt64());
            else if (spec.Equals(IntegerSpec.UInt8))
                return EmitUInt8(Value.ToUInt8());
            else if (spec.Equals(IntegerSpec.UInt16))
                return EmitUInt16(Value.ToUInt16());
            else if (spec.Equals(IntegerSpec.UInt32))
                return EmitUInt32(Value.ToUInt32());
            else if (spec.Equals(IntegerSpec.UInt64))
                return EmitUInt64(Value.ToUInt64());
            else
                throw new NotSupportedException("Unsupported integer spec: " + spec.ToString());
        }

        #endregion

        #region Object Model

        public ICodeBlock EmitMethod(IMethod Method, ICodeBlock Caller, Operator Op)
        {
            return EmitMethod(Method, Caller); // TODO: rewrite direct/virtual call logic to use
                                               //       information embedded in the operator.
        }

        private ICppBlock EmitTypeBinaryCore(ICppBlock Value, IType Type, Operator Op)
        {
            if (Op.Equals(Operator.StaticCast))
            {
                return new ConversionBlock(this, Value, Type);
            }
            else if (Op.Equals(Operator.ReinterpretCast))
            {
                var srcType = Value.Type;
                if (ConversionBlock.UseImplicitCast(srcType, Type))
                {
                    return new ConversionBlock(this, Value, Type);
                }
                else if (ConversionBlock.UseConvertToSharedPtr(srcType, Type))
                {
                    return new ToReferenceBlock(Value);
                }
                else if (ConversionBlock.UseConvertToTransientPtr(srcType, Type))
                {
                    return new ToAddressBlock(Value);
                }
                else
                {
                    return new ReinterpretCastBlock(Value, Type);
                }
            }
            else if (Op.Equals(Operator.AsInstance) || Op.Equals(Operator.DynamicCast)
                || Op.Equals(Operator.UnboxValue) || Op.Equals(Operator.UnboxReference))
            {
                // TODO: throw an exception if a dynamic_cast fails
                // TODO: create specialized unbox nodes
                return new DynamicCastBlock(Value, Type);
            }
            else if (Op.Equals(Operator.IsInstance))
            {
                return new IsInstanceBlock(this, Value, Type);
            }
            else
            {
                return null;
            }
        }

        public ICodeBlock EmitTypeBinary(ICodeBlock Value, IType Type, Operator Op)
        {
            return EmitTypeBinaryCore((ICppBlock)Value, Environment.TypeConverter.Convert(Type), Op);
        }

        #region Conversions

        #endregion

        #region Default Values

        public ICodeBlock EmitDefaultValue(IType Type)
        {
            if (Type.GetIsPrimitive())
            {
                if (Type.GetIsInteger() || Type.GetIsBit())
                {
                    return new LiteralBlock(this, "0", Type);
                }
                else if (Type.GetIsFloatingPoint())
                {
                    return new LiteralBlock(this, "0.0", Type);
                }
                else if (Type.Equals(PrimitiveTypes.Char))
                {
                    return EmitChar(default(char));
                }
                else if (Type.Equals(PrimitiveTypes.Boolean))
                {
                    return EmitBoolean(false);
                }
            }
            if (Type.GetIsReferenceType() || Type.Equals(PrimitiveTypes.String))
            {
                return EmitNull();
            }
            else
            {
                return new StackConstructorBlock(Type.GetParameterlessConstructor().CreateConstructorBlock(this), new ICppBlock[0]);
            }
        }

        #endregion

        public ICodeBlock EmitInvocation(ICodeBlock Method, IEnumerable<ICodeBlock> Arguments)
        {
            var cppArgs = Arguments.Cast<ICppBlock>();
            if (Method is IPartialBlock)
            {
                return ((IPartialBlock)Method).Complete(new PartialArguments(cppArgs.ToArray()));
            }
            else
            {
                return new InvocationBlock((ICppBlock)Method, cppArgs);
            }
        }

        public ICodeBlock EmitNewObject(IMethod Constructor, IEnumerable<ICodeBlock> Arguments)
        {
            var resultType = Environment.TypeConverter.Convert(Constructor.DeclaringType);
            if (!resultType.GetIsValueType())
            {
                return new SharedPtrBlock(Method.CreateConstructorBlock(this), Arguments.Cast<ICppBlock>());
            }
            else
            {
                return new StackConstructorBlock(Method.CreateConstructorBlock(this), Arguments.Cast<ICppBlock>());
            }
        }

        public ICodeBlock EmitMethod(IMethod Method, ICodeBlock Caller)
        {
            if (Method.GetIsOperator() && PartialOperatorBlock.IsSupportedOverload(Method))
            {
                return new PartialOperatorBlock(this, Method, Caller as ICppBlock);
            }
            else if (Caller == null)
            {
                if (Method.IsConstructor)
                {
                    var resultType = Environment.TypeConverter.Convert(Method.DeclaringType);
                    if (!resultType.GetIsValueType())
                    {
                        return new PartialSharedPtrBlock(Method.CreateConstructorBlock(this));
                    }
                    else
                    {
                        return new PartialStackConstructorBlock(Method.CreateConstructorBlock(this));
                    }
                }
                else
                {
                    return Method.CreateBlock(this);
                }
            }
            else if (Method.IsConstructor && Method.GetParameters().Length == 0)
            {
                return new PartialEmptyBlock(this, MethodType.Create(Method)); // Do not emit calls to the parameterless base constructor
            }
            else
            {
                var cppCaller = (ICppBlock)Caller;
                if (Method is IAccessor)
                {
                    var accessor = (IAccessor)Method;
                    var declProp = accessor.DeclaringProperty;
                    if (cppCaller.Type.GetIsArray())
                    {
                        var arrSliceProp = cppCaller.Type.Properties.FirstOrDefault((item) => item.Name == declProp.Name && item.IsStatic == declProp.IsStatic);
                        if (arrSliceProp != null)
                        {
                            var arrSliceAccessor = arrSliceProp.GetAccessor(accessor.AccessorType);
                            if (arrSliceAccessor != null)
                            {
                                return new MemberAccessBlock(cppCaller, arrSliceAccessor, MethodType.Create(arrSliceAccessor));
                            }
                        }
                    }
                    else if (declProp.GetIsIndexer() && Method.DeclaringType.IsForeign() && declProp.IndexerParameters.Count() == 1)
                    {
                        if (accessor.GetIsGetAccessor())
                        {
                            return new PartialElementBlock(cppCaller, Method.ReturnType);
                        }
                        else if (accessor.GetIsSetAccessor())
                        {
                            return new PartialSetElementBlock(cppCaller, declProp.PropertyType);
                        }
                    }
                }
                return new MemberAccessBlock(cppCaller, Method, MethodType.Create(Method));
            }
        }

        public ICodeBlock EmitNewArray(IType ElementType, IEnumerable<ICodeBlock> Dimensions)
        {
            var ctor = Environment.GetStdxNamespace().ArraySlice.MakeGenericType(new IType[] { ElementType }).GetConstructor(new IType[] { PrimitiveTypes.Int32 }, false);
            return EmitInvocation(EmitMethod(ctor, null), Dimensions);
        }

        public ICodeBlock EmitNewVector(IType ElementType, IReadOnlyList<int> Dimensions)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Variables

        #region Locals

        public CppLocalManager LocalManager { get; private set; }

        public IEmitVariable DeclareLocal(UniqueTag Tag, IVariableMember VariableMember)
        {
            return DeclareUnmanagedLocal(Tag, VariableMember);
        }

        public IEmitVariable DeclareNewLocal(UniqueTag Tag, IVariableMember VariableMember)
        {
            return DeclareNewUnmanagedLocal(Tag, VariableMember);
        }

        public IUnmanagedEmitVariable DeclareUnmanagedLocal(UniqueTag Tag, IVariableMember VariableMember)
        {
            return LocalManager.Declare(Tag, this.ConvertVariableMember(VariableMember));
        }

        public IUnmanagedEmitVariable DeclareNewUnmanagedLocal(UniqueTag Tag, IVariableMember VariableMember)
        {
            return LocalManager.DeclareNew(Tag, this.ConvertVariableMember(VariableMember));
        }

        public IEmitVariable GetLocal(UniqueTag Tag)
        {
            return GetUnmanagedLocal(Tag);
        }

        public IUnmanagedEmitVariable GetUnmanagedLocal(UniqueTag Tag)
        {
            return LocalManager.Get(Tag);
        }

        public OwnedCppLocal DeclareOwnedVariable(IVariableMember VariableMember)
        {
            return LocalManager.DeclareOwned(new UniqueTag(), this.ConvertVariableMember(VariableMember));
        }

        #endregion

        public IEmitVariable GetElement(ICodeBlock Value, IEnumerable<ICodeBlock> Index)
        {
            return GetUnmanagedElement(Value, Index);
        }

        public IEmitVariable GetField(IField Field, ICodeBlock Target)
        {
            return GetUnmanagedField(Field, Target);
        }

        public IEmitVariable GetArgument(int Index)
        {
            return GetUnmanagedArgument(Index);
        }

        public IEmitVariable GetThis()
        {
            return new CppThis(this);
        }

        public IUnmanagedEmitVariable GetUnmanagedElement(ICodeBlock Value, IEnumerable<ICodeBlock> Index)
        {
            return new CppElement(this, (ICppBlock)Value, (ICppBlock)Index.Single());
        }

        public IUnmanagedEmitVariable GetUnmanagedField(IField Field, ICodeBlock Target)
        {
            return new CppField(this, (ICppBlock)Target, Field);
        }

        public IUnmanagedEmitVariable GetUnmanagedArgument(int Index)
        {
            return new CppArgument(this, Index);
        }

        #endregion

        #region Unmanaged

        public ICodeBlock EmitDereferencePointer(ICodeBlock Pointer)
        {
            return new DereferenceBlock((ICppBlock)Pointer);
        }

        public ICodeBlock EmitSizeOf(IType Type)
        {
            return new SizeOfBlock(this, Type.CreateBlock(this));
        }

        public ICodeBlock EmitStoreAtAddress(ICodeBlock Pointer, ICodeBlock Value)
        {
            return new ExpressionStatementBlock(new VariableAssignmentBlock((ICppBlock)EmitDereferencePointer(Pointer), (ICppBlock)Value));
        }

        #endregion

        #region Foreach

        public ICollectionBlock EmitCollectionBlock(IVariableMember Member, ICodeBlock Collection)
        {
            return new CollectionBlock(this, Member, (ICppBlock)Collection);
        }

        public ICodeBlock EmitForeachBlock(IForeachBlockHeader Header, ICodeBlock Body)
        {
            var header = (ForeachHeader)Header;
            var body = (ICppBlock)Body;
            CppBlock.HoistVariableDeclarations(header.Element.Declaration.Local, body);

            return new ForeachBlock(header, body);
        }

        public IForeachBlockHeader EmitForeachHeader(UniqueTag Tag, IEnumerable<ICollectionBlock> Collections)
        {
            if (Collections.Any() && !Collections.Skip(1).Any()) // == (Collections.Count() == 1)
            {
                var singleCollection = Collections.Single() as CollectionBlock;
                if (singleCollection != null)
                {
                    return new ForeachHeader(this, singleCollection);
                }
            }
            return null;
        }

        #endregion

        #region IExceptionCodeGenerator

        public ICatchClause EmitCatchClause(ICatchHeader Header, ICodeBlock Body)
        {
            return new CatchBlock((CatchHeader)Header, (ICppBlock)Body);
        }

        public ICatchHeader EmitCatchHeader(IVariableMember ExceptionVariable)
        {
            return new CatchHeader(this, ExceptionVariable);
        }

        public ICodeBlock EmitTryBlock(ICodeBlock TryBody, ICodeBlock FinallyBody, IEnumerable<ICatchClause> CatchClauses)
        {
            return new ExceptionHandlingBlock(this, (ICppBlock)TryBody, (ICppBlock)FinallyBody, CatchClauses.Cast<CatchBlock>());
        }

        public ICodeBlock EmitAssert(ICodeBlock Condition, ICodeBlock Message)
        {
            // Ignore the message, for now.
            var cppExpr = (ICppBlock)EmitInvocation(EmitMethod(CppPrimitives.AssertMethod, null), new ICodeBlock[] { Condition });
            return new ImplicitDependencyBlock(new ExpressionStatementBlock(cppExpr), new IHeaderDependency[] { StandardDependency.CAssert });
        }

        public ICodeBlock EmitThrow(ICodeBlock Exception)
        {
            return new ThrowBlock(this, (ICppBlock)Exception);
        }

        #endregion

        #region IForCodeGenerator

        public ICodeBlock EmitForBlock(UniqueTag Tag, ICodeBlock Initialization, ICodeBlock Condition, ICodeBlock Delta, ICodeBlock Body)
        {
            var cppInit = (ICppBlock)Initialization;
            var cppCond = (ICppBlock)Condition;
            var cppDelta = (ICppBlock)Delta;

            if (cppInit.IsSimple() && cppDelta.IsSimple())
            {
                var cppBody = (ICppBlock)Body;

                var forBlock = new ForBlock(cppInit, cppCond, cppDelta, cppBody);

                // How this works:
                // Step #1: Hoist all variable declarations common to the condition, delta and body.
                // Step #2: Assume the initialization and the rest of the for loop is executed in sequence,
                //          and hoist variable declarations accordingly
                // Step #3: If any variable declarations have been hoisted, emit them.
                //          In any case, the 'for' block is subsequently emitted.

                var unionDecls = CppBlock.HoistUnionDeclarations(cppCond, cppDelta, cppBody);
                var seqDecls = CppBlock.HoistSequenceDeclarations(new ICppBlock[] { cppInit }.Concat(unionDecls));
                if (seqDecls.Count > 0)
                {
                    return EmitSequence(new CppBlock(this, seqDecls), forBlock);
                }
                else
                {
                    return forBlock;
                }
            }
            return null;
        }

        #endregion

        #region IInitializingCodeGenerator

        public ICodeBlock EmitInitializedArray(IType ElementType, ICodeBlock[] Items)
        {
            var initList = new InitializerListBlock(this, ElementType, Items.Cast<ICppBlock>());
            var ctor = Environment.GetStdxNamespace().ArraySlice.MakeGenericType(new IType[] { ElementType }).GetConstructor(new IType[] { initList.Type }, false);
            return EmitInvocation(EmitMethod(ctor, null), new ICodeBlock[] { initList });
        }

        public ICodeBlock EmitInitializedVector(IType ElementType, ICodeBlock[] Items)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IContractCodeGenerator

        private Lazy<IEmitVariable> retVar;

        public IEmitVariable ReturnVariable
        {
            get
            {
                return retVar.Value;
            }
        }

        public ICodeBlock EmitContractBlock(ICodeBlock Precondition, ICodeBlock Postcondition, ICodeBlock Body)
        {
            return new ContractBlock((ICppBlock)Body, (ICppBlock)Precondition, (ICppBlock)Postcondition);
        }

        #endregion
    }
}
