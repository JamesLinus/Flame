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
                                   IExceptionCodeGenerator, IStackCodeGenerator,
                                   IJumpTableCodeGenerator
    {
        public ILCodeGenerator(IMethod Method)
        {
            this.Method = Method;
            this.labelMap = new Dictionary<UniqueTag, ILLabel>();
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
            else if (Op.Equals(Operator.Box))
            {
                return new BoxBlock(val);
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region Blocks

        public ICodeBlock EmitBreak(UniqueTag Target)
        {
            return new BreakBlock(this, Target);
        }

        public ICodeBlock EmitContinue(UniqueTag Target)
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

        public ICodeBlock EmitTagged(UniqueTag Tag, ICodeBlock Contents)
        {
            return new TaggedBlock(this, Tag, (ICecilBlock)Contents);
        }

        #endregion

        #region Branching

        private Dictionary<UniqueTag, ILLabel> labelMap;

        public ICodeBlock EmitGotoLabel(UniqueTag Tag, ICodeBlock Condition)
        {
            return GetLabel(Tag).EmitBranch(Condition);
        }

        public ICodeBlock EmitMarkLabel(UniqueTag Tag)
        {
            return GetLabel(Tag).EmitMark();
        }

        /// <summary>
        /// Creates a code block that jumps to an index in the given list of labels. If the
        /// index is out of range, then control proceeds to the next block.
        /// </summary>
        /// <param name="TableIndex">The index in the table of the label to jump to.</param>
        /// <param name="TableLabels">The table's contents, as a list of labels.</param>
        /// <returns></returns>
        public ICodeBlock EmitJumpTable(ICodeBlock TableIndex, IReadOnlyList<UniqueTag> TableLabels)
        {
            return new JumpTableBlock((ICecilBlock)TableIndex, TableLabels.Select(GetLabel).ToArray());
        }

        public ILLabel GetLabel(UniqueTag Tag)
        {
            ILLabel result;
            if (!labelMap.TryGetValue(Tag, out result))
            {
                result = CreateLabel();
                labelMap[Tag] = result;
            }
            return result;
        }

        public ILLabel CreateLabel()
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

        public ICodeBlock EmitBoolean(bool Value)
        {
            return new OpCodeBlock(this, Value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0, new SinglePushBehavior(PrimitiveTypes.Boolean));
        }

        public ICodeBlock EmitChar(char Value)
        {
            return new RetypedBlock((ICecilBlock)EmitInt16((short)Value), PrimitiveTypes.Char);
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
            if (IsBetween(Value, int.MinValue, int.MaxValue))
            {
                // The following:
                //
                // ldc.i4 xxx
                // conv.i8
                //
                // is always shorter than:
                //
                // ldc.i8 xxx
                //
                return EmitConversion(EmitInt32((int)Value), PrimitiveTypes.Int64);
            }
            else if (IsBetween(Value, uint.MinValue, uint.MaxValue))
            {
                // The following:
                //
                // ldc.i4 -xxx
                // conv.u8
                //
                // is always shorter than:
                //
                // ldc.i8 yyy
                //
                return new RetypedBlock(
                    (ICecilBlock)EmitConversion(
                        EmitUInt32((uint)Value), PrimitiveTypes.UInt64),
                    PrimitiveTypes.Int64);
            }
            else
            {
                // Only do this if we can't avoid it.
                return new OpCodeInt64Block(
                    this, OpCodes.Ldc_I8, Value, new SinglePushBehavior(PrimitiveTypes.Int64));
            }
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
            return new RetypedBlock(
                (ICecilBlock)EmitInt64((long)Value), PrimitiveTypes.UInt64);
        }

        public ICodeBlock EmitUInt8(byte Value)
        {
            return EmitLoadInt32Command((int)(sbyte)Value, PrimitiveTypes.UInt8);
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

        public ICodeBlock EmitTypeBinary(ICodeBlock Value, IType Type, Operator Op)
        {
            var val = (ICecilBlock)Value;
            if (Op.Equals(Operator.IsInstance))
            {
                return new TypeCheckBlock(val, Type);
            }
            else if (Op.Equals(Operator.AsInstance))
            {
                return new AsInstanceBlock(val, Type);
            }
            else if (Op.Equals(Operator.ReinterpretCast))
            {
                return new RetypedBlock(val, Type);
            }
            else if (Op.Equals(Operator.DynamicCast))
            {
                return new DynamicCastBlock(val, Type);
            }
            else if (Op.Equals(Operator.StaticCast))
            {
                return new ConversionBlock(this, val, Type);
            }
            else if (Op.Equals(Operator.UnboxValue))
            {
                return new SimpleConversionBlock(val, Type, OpCodes.Unbox_Any, Type);
            }
            else if (Op.Equals(Operator.UnboxReference))
            {
                return new SimpleConversionBlock(val, Type, OpCodes.Unbox, Type.AsContainerType().ElementType);
            }
            else
            {
                return null;
            }
        }

        #region Default Value

        public ICodeBlock EmitDefaultValue(IType Type)
        {
            return new DefaultValueBlock(this, Type);
        }

        #endregion

        #region Method Calls

        public ICodeBlock EmitMethod(IMethod Method, ICodeBlock Caller, Operator Op)
        {
            return new MethodBlock(this, Method, (ICecilBlock)Caller, Op.Equals(Operator.GetVirtualDelegate));
        }

        public ICodeBlock EmitInvocation(ICodeBlock Method, IEnumerable<ICodeBlock> Arguments)
        {
            return new InvocationBlock((ICecilBlock)Method, Arguments.Cast<ICecilBlock>());
        }

        public ICodeBlock EmitNewObject(IMethod Constructor, IEnumerable<ICodeBlock> Arguments)
        {
            return new NewObjectBlock(this, Constructor, Arguments.Cast<ICecilBlock>());
        }

        #endregion

        #region EmitConversion

        public ICodeBlock EmitConversion(ICodeBlock Value, IType Type)
        {
            var ilVal = (ICecilBlock)Value;
            if (ilVal.IsInt32Literal() && Type.GetIsInteger() && Type.GetPrimitiveSize() <= 4)
            {
                return new RetypedBlock(ilVal, Type);
            }
            else
            {
                return new ConversionBlock(this, ilVal, Type);
            }
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

        private ICodeBlock VirtualPop(ICodeBlock Block)
        {
            // Replaces a non-canonical `void` type by the
            // canonical `void` type. Methods imported from
            // reflection seem to have a `System.Void` type
            // which is not equal to `TypeSystem.Void`, and
            // this works around that.
            return new VirtualPopBlock((ICecilBlock)Block);
        }

        public ICodeBlock EmitAssert(ICodeBlock Condition)
        {
            var method = CecilMethod.ImportCecil(typeof(System.Diagnostics.Debug).GetMethod(
                "Assert", new Type[] { typeof(bool) }), (ICecilMember)Method);

            return VirtualPop(EmitInvocation(
                    EmitMethod(method, null, Operator.GetDelegate),
                    new ICodeBlock[] { Condition }));
        }

        public ICodeBlock EmitAssert(ICodeBlock Condition, ICodeBlock Message)
        {
            if (Message == null)
            {
                return EmitAssert(Condition);
            }
            else
            {
                var method = CecilMethod.ImportCecil(typeof(System.Diagnostics.Debug).GetMethod(
                    "Assert", new Type[] { typeof(bool), typeof(string) }), (ICecilMember)Method);

                return VirtualPop(EmitInvocation(
                        EmitMethod(method, null, Operator.GetDelegate),
                        new ICodeBlock[] { Condition, Message }));
            }
        }

        public ICatchClause EmitCatchClause(ICatchHeader Header, ICodeBlock Body)
        {
            return new CatchClause((CatchHeader)Header, (ICecilBlock)Body);
        }

        public ICatchHeader EmitCatchHeader(IVariableMember ExceptionVariable)
        {
            return new CatchHeader(ExceptionVariable.VariableType, DeclareLocal(new UniqueTag(), ExceptionVariable));
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

        public IEmitVariable GetThis()
        {
            if (Method.IsStatic)
            {
                throw new InvalidOperationException("Static methods do not have a 'this' variable.");
            }
            return new ArgumentVariable(this, 0);
        }

        #endregion

        #region Locals

        private Dictionary<UniqueTag, LocalVariable> locals = new Dictionary<UniqueTag, LocalVariable>();

        public IUnmanagedEmitVariable GetUnmanagedLocal(UniqueTag Tag)
        {
            LocalVariable result;
            if (locals.TryGetValue(Tag, out result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        public IUnmanagedEmitVariable DeclareUnmanagedLocal(UniqueTag Tag, IVariableMember VariableMember)
        {
            var result = new LocalVariable(this, VariableMember);
            locals.Add(Tag, result);
            return result;
        }

        public IEmitVariable GetLocal(UniqueTag Tag)
        {
            return GetUnmanagedLocal(Tag);
        }

        public IEmitVariable DeclareLocal(UniqueTag Tag, IVariableMember VariableMember)
        {
            return DeclareUnmanagedLocal(Tag, VariableMember);
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

		#region Stack intrinsics

		public ICodeBlock EmitPush(ICodeBlock Value)
		{
			return new StackPushBlock(this, (ICecilBlock)Value);
		}

		public ICodeBlock EmitPeek(IType Type)
		{
			return new StackPeekBlock(this, Type);
		}

		public ICodeBlock EmitPop(IType Type)
		{
			return new StackPopBlock(this, Type);
		}

		#endregion

        #region Helpers

        #region IsBetween

        public static bool IsBetween(int Value, int Min, int Max)
        {
            return Value >= Min && Value <= Max;
        }

        public static bool IsBetween(long Value, long Min, long Max)
        {
            return Value >= Min && Value <= Max;
        }

        public static bool IsBetween(uint Value, uint Min, uint Max)
        {
            return Value >= Min && Value <= Max;
        }

        public static bool IsBetween(ulong Value, ulong Min, ulong Max)
        {
            return Value >= Min && Value <= Max;
        }

        #endregion

        #region IsPossibleValueType

        public static bool IsPossibleValueType(IType Type)
        {
            return IsCLRValueType(Type) || (Type.GetIsGenericParameter() && !Type.GetIsReferenceType());
        }

        public static bool IsCLRValueType(IType Type)
        {
            return Type.GetIsValueType() || Type.GetIsEnum() || (Type.GetIsPrimitive() && Type.GetPrimitiveSize() > 0);
        }

        #endregion

        #region GetExtendedParameterTypes

        public static IType GetThisParameterType(IMethod Method)
        {
            return Flame.Compiler.Variables.ThisVariable.GetThisType(Method.DeclaringType);
        }

        public static IType[] GetExtendedParameterTypes(IMethod Method)
        {
            var realParams = Method.GetParameters();
            bool instance = !Method.IsStatic;
            int offset = instance ? 1 : 0;
            IType[] parameterTypes = new IType[realParams.Length + offset];
            if (instance)
            {
                parameterTypes[0] = GetThisParameterType(Method);
            }
            for (int i = 0; i < realParams.Length; i++)
            {
                parameterTypes[i + offset] = realParams[i].ParameterType;
            }
            return parameterTypes;
        }

        public static IType GetExtendedParameterType(IMethod Method, int Index)
        {
            int offset = Method.IsStatic ? 0 : 1;
            if (Index == 0 && offset == 1)
            {
                // "this" parameter
                return GetThisParameterType(Method);
            }
            else
            {
                // Regular parameter
                return Method.Parameters.ElementAt(Index - offset).ParameterType;
            }
        }

        public static IType[] GetExtendedParameterTypes(ICodeGenerator CodeGenerator)
        {
            return GetExtendedParameterTypes(CodeGenerator.Method);
        }

        public static IType GetExtendedParameterType(ICodeGenerator CodeGenerator, int Index)
        {
            return GetExtendedParameterType(CodeGenerator.Method, Index);
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
            if (Type.GetIsPointer())
            {
                return Type.AsContainerType().ElementType.Is(Method.DeclaringType);
            }
            else
            {
                return Type.Is(Method.DeclaringType);
            }
        }

        public static void EmitCall(IEmitContext Context, IMethod Method, IType CallerType, bool IsVirtual)
        {
            if (CallerType != null && CallerType.GetIsPointer() && Method.DeclaringType.GetIsReferenceType())
            {
                Context.Emit(OpCodes.Constrained, CallerType.AsContainerType().ElementType);
                Context.Emit(OpCodes.Callvirt, Method);
            }
            else if (IsVirtual)
            {
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
            if (Method.DeclaringType.GetIsPrimitive())
            {
                if (Method.Name.ToString() == "Parse" && Method.IsStatic)
                {
                    return Method.GetParameters().Length == 1;
                }
                else if (Method.Name.ToString() == "ToString" && !Method.IsStatic)
                {
                    return Method.GetParameters().Length == 0;
                }
            }
            return false;
        }

        public static void EmitCultureInvariantCall(IEmitContext Context, IMethod Method, IType CallerType, bool IsVirtual, CecilModule Module)
        {
            var cecilDeclType = Module.ConvertStrict(CecilTypeImporter.Import(Module, Context.Processor.Body.Method, Method.DeclaringType));
            var paramTypes = Method.GetParameters().GetTypes().Concat(new IType[] { CecilType.ImportCecil(typeof(System.IFormatProvider), Module) }).ToArray();
            var newMethod = cecilDeclType.GetMethod(Method.Name, Method.IsStatic, Method.ReturnType, paramTypes);
            if (newMethod == null)
            {
                EmitCall(Context, Method, CallerType, IsVirtual);
            }
            else
            {
                var cultureInfoType = CecilType.ImportCecil(typeof(System.Globalization.CultureInfo), Module);
                var invariantCultureProperty = cultureInfoType.Properties.GetProperty(new SimpleName("InvariantCulture"), true);
                // Pushes System.Globalization.CultureInfo.InvariantCulture on the stack
                EmitCall(Context, invariantCultureProperty.GetGetAccessor(), null, false);
                // Makes the invariant call
                EmitCall(Context, newMethod, CallerType, newMethod.GetIsVirtual());
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
