﻿using Flame.Compiler;
using Flame.Compiler.Emit;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class ILCodeGenerator : IBranchingCodeGenerator, IUnmanagedCodeGenerator, 
                                   IExceptionCodeGenerator
    {
        public ILCodeGenerator(IMethod Method)
        {
            this.Method = Method;
        }

        public IMethod Method { get; private set; }

        #region EmitBinary

        public ICodeBlock EmitBinary(ICodeBlock A, ICodeBlock B, Operator Op)
        {
            var ilLeft = (ICecilBlock)A;
            var ilRight = (ICecilBlock)B;

            // Optimize `0 - x` to `-x`
            // TODO: Maybe get rid of this. 
            //       This is really something that should be done elsewhere.
            if (Op.Equals(Operator.Subtract) && ilLeft.IsInt32Literal() && ilLeft.GetInt32Literal() == 0)
            {
                if (ilRight.IsInt32Literal())
                {
                    return new RetypedBlock((ICecilBlock)EmitInt32(-ilRight.GetInt32Literal()), ilRight.BlockType);
                }
                else
                {
                    return new NegateBlock(ilRight);
                }
            }
            else if (BinaryOpBlock.IsSupported(Op))
            {
                return new BinaryOpBlock(this, ilLeft, ilRight, Op);
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region EmitUnary

        public ICodeBlock EmitUnary(ICodeBlock Value, Operator Op)
        {
            var val = (ICecilBlock)Value;
            if (UnaryOpBlock.Supports(Op, val.BlockType))
            {
                return new UnaryOpBlock(this, val, Op);
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region Blocks

        public ICodeBlock EmitBreak(BlockTag Target)
        {
            return new BreakBlock(this, Target);
        }

        public ICodeBlock EmitContinue(BlockTag Target)
        {
            return new ContinueBlock(this, Target);
        }

        public ICodeBlock EmitIfElse(ICodeBlock Condition, ICodeBlock IfBody, ICodeBlock ElseBody)
        {
            return new IfElseBlock(this, (ICecilBlock)Condition, (ICecilBlock)IfBody, (ICecilBlock)ElseBody);
        }

        public ICodeBlock EmitPop(ICodeBlock Value)
        {
            return new PopBlock(this, (ICecilBlock)Value);
        }

        public ICodeBlock EmitReturn(ICodeBlock Value)
        {
            return new ReturnBlock(this, (ICecilBlock)Value);
        }

        public ICodeBlock EmitSequence(ICodeBlock First, ICodeBlock Second)
        {
            return new SequenceBlock(this, (ICecilBlock)First, (ICecilBlock)Second);
        }

        public ICodeBlock EmitVoid()
        {
            return new EmptyBlock(this);
        }

        public ICodeBlock EmitTagged(BlockTag Tag, ICodeBlock Contents)
        {
            return new TaggedBlock(this, Tag, (ICecilBlock)Contents);
        }

        #endregion

        #region Branching

        public ILabel CreateLabel()
        {
            return new ILLabel(this);
        }

        #endregion

        #region Primitive Values

        public ICodeBlock EmitNull()
        {
            return new OpCodeBlock(this, OpCodes.Ldnull, new SinglePushBehavior(PrimitiveTypes.Null));
        }

        public ICodeBlock EmitString(string Value)
        {
            return new OpCodeStringBlock(this, OpCodes.Ldstr, Value, new SinglePushBehavior(PrimitiveTypes.String));
        }

        public ICodeBlock EmitBit16(ushort Value)
        {
            return new RetypedBlock((ICecilBlock)EmitUInt16(Value), PrimitiveTypes.Bit16);
        }

        public ICodeBlock EmitBit32(uint Value)
        {
            return new RetypedBlock((ICecilBlock)EmitUInt32(Value), PrimitiveTypes.Bit32);
        }

        public ICodeBlock EmitBit64(ulong Value)
        {
            return new RetypedBlock((ICecilBlock)EmitUInt64(Value), PrimitiveTypes.Bit64);
        }

        public ICodeBlock EmitBit8(byte Value)
        {
            return new RetypedBlock((ICecilBlock)EmitUInt8(Value), PrimitiveTypes.Bit8);
        }

        public ICodeBlock EmitBoolean(bool Value)
        {
            return new OpCodeBlock(this, Value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0, new SinglePushBehavior(PrimitiveTypes.Boolean));
        }

        public ICodeBlock EmitChar(char Value)
        {
            return EmitInt16((short)Value);
        }

        public ICodeBlock EmitFloat32(float Value)
        {
            return new OpCodeFloat32Block(this, OpCodes.Ldc_R4, Value, new SinglePushBehavior(PrimitiveTypes.Float32));
        }

        public ICodeBlock EmitFloat64(double Value)
        {
            return new OpCodeFloat64Block(this, OpCodes.Ldc_R8, Value, new SinglePushBehavior(PrimitiveTypes.Float64));
        }

        public ICodeBlock EmitInt16(short Value)
        {
            return EmitLoadInt32Command(Value, PrimitiveTypes.Int16);
        }

        public ICodeBlock EmitInt32(int Value)
        {
            return EmitLoadInt32Command(Value, PrimitiveTypes.Int32);
        }

        public ICodeBlock EmitInt64(long Value)
        {
            return new OpCodeInt64Block(this, OpCodes.Ldc_I8, Value, new SinglePushBehavior(PrimitiveTypes.Int64));
        }

        public ICodeBlock EmitInt8(sbyte Value)
        {
            return EmitLoadInt32Command(Value, PrimitiveTypes.Int8);
        }

        protected ICodeBlock EmitLoadInt32Command(int Value, IType Type)
        {
            var behavior = new SinglePushBehavior(Type);
            switch (Value)
            {
                case 0:
                    return new OpCodeBlock(this, OpCodes.Ldc_I4_0, behavior);
                case -1:
                    return new OpCodeBlock(this, OpCodes.Ldc_I4_M1, behavior);
                case 1:
                    return new OpCodeBlock(this, OpCodes.Ldc_I4_1, behavior);
                case 2:
                    return new OpCodeBlock(this, OpCodes.Ldc_I4_2, behavior);
                case 3:
                    return new OpCodeBlock(this, OpCodes.Ldc_I4_3, behavior);
                case 4:
                    return new OpCodeBlock(this, OpCodes.Ldc_I4_4, behavior);
                case 5:
                    return new OpCodeBlock(this, OpCodes.Ldc_I4_5, behavior);
                case 6:
                    return new OpCodeBlock(this, OpCodes.Ldc_I4_6, behavior);
                case 7:
                    return new OpCodeBlock(this, OpCodes.Ldc_I4_7, behavior);
                case 8:
                    return new OpCodeBlock(this, OpCodes.Ldc_I4_8, behavior);
                default:
                    if (IsBetween(Value, sbyte.MinValue, sbyte.MaxValue))
                    {
                        return new OpCodeInt8Block(this, OpCodes.Ldc_I4_S, (sbyte)Value, behavior);
                    }
                    else
                    {
                        return new OpCodeInt32Block(this, OpCodes.Ldc_I4, Value, behavior);
                    }
            }
        }

        public ICodeBlock EmitUInt16(ushort Value)
        {
            return EmitLoadInt32Command((int)(short)Value, PrimitiveTypes.UInt16);
        }

        public ICodeBlock EmitUInt32(uint Value)
        {
            return EmitLoadInt32Command((int)Value, PrimitiveTypes.UInt32);
        }

        public ICodeBlock EmitUInt64(ulong Value)
        {
            return new OpCodeInt64Block(this, OpCodes.Ldc_I8, (long)Value, new SinglePushBehavior(PrimitiveTypes.UInt64));
        }

        public ICodeBlock EmitUInt8(byte Value)
        {
            return EmitLoadInt32Command((int)(sbyte)Value, PrimitiveTypes.UInt8);
        }

        #endregion

        #region Object Model

        #region Default Value

        public ICodeBlock EmitDefaultValue(IType Type)
        {
            return new DefaultValueBlock(this, Type);
        }

        #endregion

        #region Method Calls

        public ICodeBlock EmitMethod(IMethod Method, ICodeBlock Caller)
        {
            return new MethodBlock(this, Method, (ICecilBlock)Caller);
        }

        public ICodeBlock EmitInvocation(ICodeBlock Method, IEnumerable<ICodeBlock> Arguments)
        {
            return new InvocationBlock((ICecilBlock)Method, Arguments.Cast<ICecilBlock>());
        }

        #endregion

        #region EmitConversion

        public ICodeBlock EmitConversion(ICodeBlock Value, IType Type)
        {
            var ilVal = (ICecilBlock)Value;
            if (ilVal.IsInt32Literal() && Type.get_IsInteger() && Type.GetPrimitiveMagnitude() < 4)
            {
                return new RetypedBlock(ilVal, Type);
            }
            else
            {
                return new ConversionBlock(this, ilVal, Type);
            }
        }

        #endregion

        #region EmitIsOfType

        public ICodeBlock EmitIsOfType(IType Type, ICodeBlock Value)
        {
            return new TypeCheckBlock(this, (ICecilBlock)Value, Type);
        }

        #endregion

        #region EmitNewArray/EmitNewVector

        public ICodeBlock EmitNewArray(IType ElementType, IEnumerable<ICodeBlock> Dimensions)
        {
            return new NewArrayBlock(this, ElementType, Dimensions.Cast<ICecilBlock>().ToArray());
        }

        public ICodeBlock EmitNewVector(IType ElementType, IReadOnlyList<int> Dimensions)
        {
            return EmitNewArray(ElementType, Dimensions.Select((item) => EmitInt32(item)));
        }

        #endregion

        #endregion

        #region IExceptionCodeGenerator

        public ICodeBlock EmitAssert(ICodeBlock Condition)
        {
            var method = CecilMethod.ImportCecil(typeof(System.Diagnostics.Debug).GetMethod("Assert", new Type[] { typeof(bool) }), (ICecilMember)Method);

            return EmitInvocation(EmitMethod(method, null), new ICodeBlock[] { Condition });
        }

        public ICatchClause EmitCatchClause(ICatchHeader Header, ICodeBlock Body)
        {
            return new CatchClause((CatchHeader)Header, (ICecilBlock)Body);
        }

        public ICatchHeader EmitCatchHeader(IVariableMember ExceptionVariable)
        {
            return new CatchHeader(ExceptionVariable.VariableType, DeclareVariable(ExceptionVariable));
        }

        public ICodeBlock EmitThrow(ICodeBlock Exception)
        {
            return new ThrowBlock(this, (ICecilBlock)Exception);
        }

        public ICodeBlock EmitTryBlock(ICodeBlock TryBody, ICodeBlock FinallyBody, IEnumerable<ICatchClause> CatchClauses)
        {
            return new TryFinallyBlock(new TryCatchBlock((ICecilBlock)TryBody, CatchClauses.Cast<CatchClause>()), (ICecilBlock)FinallyBody);
        }

        #endregion

        #region Variables

        #region Arguments

        public IEmitVariable GetArgument(int Index)
        {
            return GetUnmanagedArgument(Index);
        }

        public IUnmanagedEmitVariable GetUnmanagedArgument(int Index)
        {
            return new ArgumentVariable(this, Method.IsStatic ? Index : Index + 1);
        }

        #endregion

        #region This

        public IUnmanagedEmitVariable GetUnmanagedThis()
        {
            if (Method.IsStatic)
            {
                throw new InvalidOperationException("Static methods do not have a 'this' variable.");
            }
            return new ArgumentVariable(this, 0);
        }

        public IEmitVariable GetThis()
        {
            return GetUnmanagedThis();
        }

        #endregion

        #region Locals

        public IUnmanagedEmitVariable DeclareUnmanagedVariable(IVariableMember VariableMember)
        {
            return new LocalVariable(this, VariableMember);
        }

        public IEmitVariable DeclareVariable(IVariableMember VariableMember)
        {
            return DeclareUnmanagedVariable(VariableMember);
        }

        #endregion

        #region Fields

        public IEmitVariable GetField(IField Field, ICodeBlock Target)
        {
            return GetUnmanagedField(Field, Target);
        }

        public IUnmanagedEmitVariable GetUnmanagedField(IField Field, ICodeBlock Target)
        {
            return new FieldVariable(this, (ICecilBlock)Target, Field);
        }

        #endregion

        #region Elements

        public IEmitVariable GetElement(ICodeBlock Value, IEnumerable<ICodeBlock> Index)
        {
            return GetUnmanagedElement(Value, Index);
        }

        public IUnmanagedEmitVariable GetUnmanagedElement(ICodeBlock Value, IEnumerable<ICodeBlock> Index)
        {
            return new ElementVariable(this, (ICecilBlock)Value, Index.Cast<ICecilBlock>().ToArray());
        }

        #endregion

        #endregion

        #region Unmanaged

        public ICodeBlock EmitDereferencePointer(ICodeBlock Pointer)
        {
            return new IndirectVariable(this, (ICecilBlock)Pointer).EmitGet();
        }

        public ICodeBlock EmitStoreAtAddress(ICodeBlock Pointer, ICodeBlock Value)
        {
            return new IndirectVariable(this, (ICecilBlock)Pointer).EmitSet((ICecilBlock)Value);
        }

        public ICodeBlock EmitSizeOf(IType Type)
        {
            return new SizeOfBlock(this, Type);
        }

        #endregion

        #region Helpers

        #region IsBetween

        public static bool IsBetween(int Value, int Min, int Max)
        {
            return Value >= Min && Value <= Max;
        }

        public static bool IsBetween(uint Value, uint Min, uint Max)
        {
            return Value >= Min && Value <= Max;
        }

        #endregion

        #region UseVirtualCall

        public static bool UseVirtualCall(IMethod Method)
        {
            return !Method.IsStatic && (Method.get_IsVirtual() || (Method.DeclaringType != null && Method.DeclaringType.get_IsInterface()));
        }

        #endregion

        #region IsPossibleValueType

        public static bool IsPossibleValueType(IType Type)
        {
            return IsCLRValueType(Type) || (Type.get_IsGenericParameter() && !Type.get_IsReferenceType());
        }

        public static bool IsCLRValueType(IType Type)
        {
            return Type.get_IsValueType() || Type.get_IsEnum() || (Type.get_IsPrimitive() && Type.GetPrimitiveMagnitude() > 0);
        }

        #endregion

        #region GetExtendedParameterTypes

        public static IType[] GetExtendedParameterTypes(IMethod Method)
        {
            var realParams = Method.GetParameters();
            bool instance = !Method.IsStatic;
            int offset = instance ? 1 : 0;
            IType[] parameterTypes = new IType[realParams.Length + offset];
            if (instance)
            {
                var declType = Method.DeclaringType;
                var genInst = declType.get_IsGeneric() ? declType.MakeGenericType(declType.GenericParameters) : declType;
                if (declType.get_IsValueType())
                {
                    parameterTypes[0] = genInst.MakePointerType(PointerKind.ReferencePointer);
                }
                else
                {
                    parameterTypes[0] = genInst;
                }
            }
            for (int i = 0; i < realParams.Length; i++)
            {
                parameterTypes[i + offset] = realParams[i].ParameterType;
            }
            return parameterTypes;
        }

        public static IType[] GetExtendedParameterTypes(ICodeGenerator CodeGenerator)
        {
            return GetExtendedParameterTypes(CodeGenerator.Method);
        }

        #endregion

        #region Calls

        #region Arguments

        public static IEnumerable<IType> EmitArguments(IEnumerable<ICecilBlock> Arguments, IMethod Method, IEmitContext Context)
        {
            return EmitArguments(Arguments, Method.GetParameters().GetTypes(), Context);
        }

        public static IEnumerable<IType> EmitArguments(IEnumerable<ICecilBlock> Arguments, IEnumerable<IType> ParameterTypes, IEmitContext Context)
        {
            List<IType> types = new List<IType>();
            foreach (var item in ParameterTypes.Zip(Arguments, (a, b) => new KeyValuePair<IType, ICecilBlock>(a, b)))
            {
                item.Value.Emit(Context);
                EmitImplicitArgumentCast(item.Key, Context);
                types.Add(Context.Stack.Pop());
            }
            return types;
        }

        public static void EmitImplicitArgumentCast(IType ParameterType, IEmitContext Context)
        {
            var type = Context.Stack.Peek();
            if (IsPossibleValueType(type) && !IsPossibleValueType(ParameterType))
            {
                Context.Stack.Pop();
                Context.Emit(OpCodes.Box, type);
                Context.Stack.Push(ParameterType);
            }
        }

        #endregion

        public static IType GetExpectedCallingType(IMethod Method)
        {
            if (IsCLRValueType(Method.DeclaringType))
            {
                return Method.DeclaringType.MakePointerType(PointerKind.ReferencePointer);
            }
            else
            {
                return Method.DeclaringType;
            }
        }

        public static bool IsExpectedCallingType(IMethod Method, IType Type)
        {
            if (Type.get_IsPointer())
            {
                return Type.AsContainerType().ElementType.Is(Method.DeclaringType);
            }
            else
            {
                return Type.Is(Method.DeclaringType);
            }
        }

        public static void EmitCall(IEmitContext Context, IMethod Method, IType CallerType)
        {
            if (UseVirtualCall(Method))
            {
                if (CallerType != null && CallerType.get_IsPointer())
                {
                    Context.Emit(OpCodes.Constrained, CallerType.AsContainerType().ElementType);
                }
                Context.Emit(OpCodes.Callvirt, Method);
            }
            else
            {
                Context.Emit(OpCodes.Call, Method);
            }
        }

        #endregion

        #region Culture

        /// <summary>
        /// Gets a boolean value that indicates whether the given method is a culture-specific primitive method, i.e., it relies on the local culture to perform an operation that is defined by primitives.
        /// </summary>
        /// <param name="Method"></param>
        /// <returns></returns>
        public static bool IsCultureSpecific(IMethod Method)
        {
            if (Method.DeclaringType.get_IsPrimitive())
            {
                if (Method.Name == "Parse" && Method.IsStatic)
                {
                    return Method.GetParameters().Length == 1;
                }
                else if (Method.Name == "ToString" && !Method.IsStatic)
                {
                    return Method.GetParameters().Length == 0;
                }
            }
            return false;
        }

        public static void EmitCultureInvariantCall(IEmitContext Context, IMethod Method, IType CallerType, CecilModule Module)
        {
            var cecilDeclType = Module.ConvertStrict(CecilTypeImporter.Import(Module, Context.Processor.Body.Method, Method.DeclaringType));
            var paramTypes = Method.GetParameters().GetTypes().Concat(new IType[] { CecilType.ImportCecil(typeof(System.IFormatProvider), Module) }).ToArray();
            var newMethod = cecilDeclType.GetMethod(Method.Name, Method.IsStatic, Method.ReturnType, paramTypes);
            if (newMethod == null)
            {
                EmitCall(Context, Method, CallerType);
            }
            else
            {
                var cultureInfoType = CecilType.ImportCecil(typeof(System.Globalization.CultureInfo), Module);
                var invariantCultureProperty = cultureInfoType.Properties.GetProperty("InvariantCulture", true);
                // Pushes System.Globalization.CultureInfo.InvariantCulture on the stack
                EmitCall(Context, invariantCultureProperty.GetGetAccessor(), null);
                // Makes the invariant call
                EmitCall(Context, newMethod, CallerType);
            }
        }

        #endregion

        #endregion
    }

    public static class ILCodeGeneratorExtensions
    {
        public static CecilModule GetModule(this ICodeGenerator CodeGenerator)
        {
            return ((ICecilMethod)CodeGenerator.Method).Module;
        }
    }
}
