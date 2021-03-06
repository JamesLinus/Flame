﻿using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class PythonCodeGenerator : ICodeGenerator, IYieldCodeGenerator, IInitializingCodeGenerator, IForeachCodeGenerator, IWhileCodeGenerator
    {
        public PythonCodeGenerator(IMethod Method)
        {
            this.Method = Method;
            this.generatedNames = new List<string>();
        }

        public IMethod Method { get; private set; }

        #region Blocks

        public ICodeBlock EmitTagged(UniqueTag Tag, ICodeBlock Contents)
        {
            return null;
        }

        public ICodeBlock EmitBreak(UniqueTag Tag)
        {
            return new KeywordBlock(this, "break", PrimitiveTypes.Void);
        }

        public ICodeBlock EmitContinue(UniqueTag Tag)
        {
            return new KeywordBlock(this, "continue", PrimitiveTypes.Void);
        }

        public ICodeBlock EmitIfElse(ICodeBlock Condition, ICodeBlock IfBody, ICodeBlock ElseBody)
        {
            return new IfElseBlock(this, (IPythonBlock)Condition, (IPythonBlock)IfBody, (IPythonBlock)ElseBody);
        }

        public ICodeBlock EmitPop(ICodeBlock Value)
        {
            return Value;
        }

        public ICodeBlock EmitReturn(ICodeBlock Value)
        {
            return new ReturnBlock(this, Value as IPythonBlock);
        }

        public ICodeBlock EmitSequence(ICodeBlock First, ICodeBlock Second)
        {
            return new SequenceBlock((IPythonBlock)First, (IPythonBlock)Second);
        }

        public ICodeBlock EmitVoid()
        {
            return new EmptyBlock(this);
        }

        public ICodeBlock EmitWhile(UniqueTag Tag, ICodeBlock Condition, ICodeBlock Body)
        {
            return new WhileBlock(this, (IPythonBlock)Condition, (IPythonBlock)Body);
        }

        #endregion

        #region Foreach

        public ICollectionBlock EmitCollectionBlock(IVariableMember Member, ICodeBlock Collection)
        {
            var pyColl = (IPythonBlock)Collection;
            if (pyColl.Type.GetIsContainerType())
            {
                return new ListCollectionBlock(this, pyColl);
            }
            else
            {
                return new CollectionBlock(this, Member, pyColl);
            }
        }

        public ICodeBlock EmitForeachBlock(IForeachBlockHeader Header, ICodeBlock Body)
        {
            return new ForeachBlock((ForeachBlockHeader)Header, (IPythonBlock)Body);
        }

        public IForeachBlockHeader EmitForeachHeader(UniqueTag Tag, IEnumerable<ICollectionBlock> Collections)
        {
            return new ForeachBlockHeader(this, Collections.Cast<IPythonCollectionBlock>());
        }

        #endregion

        #region Math

        public ICodeBlock EmitBinary(ICodeBlock A, ICodeBlock B, Operator Op)
        {
            return new BinaryOperation(this, (IPythonBlock)A, Op, (IPythonBlock)B);
        }

        public ICodeBlock EmitUnary(ICodeBlock Value, Operator Op)
        {
            return new UnaryOperation(this, (IPythonBlock)Value, Op);
        }

        #endregion

        #region Constants

        public ICodeBlock EmitBit16(ushort Value)
        {
            return new IntConstant(this, Value);
        }

        public ICodeBlock EmitBit32(uint Value)
        {
            return new IntConstant(this, Value);
        }

        public ICodeBlock EmitBit64(ulong Value)
        {
            return new IntConstant(this, Value);
        }

        public ICodeBlock EmitBit8(byte Value)
        {
            return new IntConstant(this, Value);
        }

        public ICodeBlock EmitBoolean(bool Value)
        {
            return new KeywordBlock(this, Value ? "True" : "False", PrimitiveTypes.Boolean);
        }

        public ICodeBlock EmitInt16(short Value)
        {
            return new IntConstant(this, Value);
        }

        public ICodeBlock EmitInt32(int Value)
        {
            return new IntConstant(this, Value);
        }

        public ICodeBlock EmitInt64(long Value)
        {
            return new IntConstant(this, Value);
        }

        public ICodeBlock EmitInt8(sbyte Value)
        {
            return new IntConstant(this, Value);
        }

        public ICodeBlock EmitUInt16(ushort Value)
        {
            return new IntConstant(this, Value);
        }

        public ICodeBlock EmitUInt32(uint Value)
        {
            return new IntConstant(this, Value);
        }

        public ICodeBlock EmitUInt64(ulong Value)
        {
            return new IntConstant(this, Value);
        }

        public ICodeBlock EmitUInt8(byte Value)
        {
            return new IntConstant(this, Value);
        }

        public ICodeBlock EmitNull()
        {
            return new KeywordBlock(this, "None", PrimitiveTypes.Null);
        }

        public ICodeBlock EmitChar(char Value)
        {
            return new StringConstant(this, Value.ToString());
        }

        public ICodeBlock EmitFloat32(float Value)
        {
            return new FloatConstant(this, Value);
        }

        public ICodeBlock EmitFloat64(double Value)
        {
            return new FloatConstant(this, Value);
        }

        public ICodeBlock EmitString(string Value)
        {
            return new StringConstant(this, Value);
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

        #region Invocations

        public ICodeBlock EmitNewObject(IMethod Constructor, IEnumerable<ICodeBlock> Arguments)
        {
            return new PythonIdentifierBlock(this, GetName(Constructor.DeclaringType), MethodType.Create(Constructor), ModuleDependency.FromType(Constructor.DeclaringType));
        }

        public ICodeBlock EmitInvocation(ICodeBlock Method, IEnumerable<ICodeBlock> Arguments)
        {
            var target = (IPythonBlock)Method;
            if (target is PythonNonexistantBlock)
            {
                return target;
            }
            var pyArgs = Arguments.Cast<IPythonBlock>().ToArray();
            if (target is IPartialBlock)
            {
                return ((IPartialBlock)target).Complete(pyArgs);
            }
            else
            {
                var deleg = ((IMethod)target.Type);
                IType retType = deleg.IsConstructor ? deleg.DeclaringType : deleg.ReturnType;
                return new InvocationBlock(this, target, pyArgs, retType);
            }
        }

        public ICodeBlock EmitMethod(IMethod Method, ICodeBlock Caller, Operator Op)
        {
            if (Op.Equals(Operator.GetDelegate))
            {
                return EmitDirectDelegate(Method, Caller);
            }
            else
            {
                return EmitDelegate(Method, Caller);
            }
        }

        public ICodeBlock EmitTypeBinary(ICodeBlock Value, IType Type, Operator Op)
        {
            if (Op.Equals(Operator.IsInstance))
            {
                return EmitIsOfType(Type, Value);
            }
            else if (Op.Equals(Operator.StaticCast))
            {
                return EmitStaticCast(Value, Type);
            }
            else if (Op.Equals(Operator.DynamicCast) || Op.Equals(Operator.ReinterpretCast) || Op.Equals(Operator.AsInstance))
            {
                return EmitReinterpretCast(Value, Type);
            }
            else
            {
                return null;
            }
        }

        private ICodeBlock EmitDirectDelegate(IMethod Method, ICodeBlock Caller)
        {
            if (Method.Equals(PythonObjectType.Instance.GetConstructor(new IType[0])))
            {
                return new PythonNonexistantBlock(this);
            }
            else if (Method.IsConstructor)
            {
                if (Caller == null)
                {
                    return new PythonIdentifierBlock(this, GetName(Method.DeclaringType), MethodType.Create(Method), ModuleDependency.FromType(Method.DeclaringType));
                }
                else
                {
                    var ctorName = new MemberAccessBlock(this, new PythonIdentifierBlock(this, GetName(Method.DeclaringType), MethodType.Create(Method), ModuleDependency.FromType(Method.DeclaringType)), "__init__", PrimitiveTypes.Void);
                    return new PartialInvocationBlock(this, ctorName, PrimitiveTypes.Void, (IPythonBlock)Caller);
                }
            }
            else
            {
                return EmitDelegate(Method, Caller);
            }
        }

        private ICodeBlock EmitDelegate(IMethod Method, ICodeBlock Caller)
        {
            if (PythonPrimitiveMap.IsPrimitiveMethod(Method))
            {
                return PythonPrimitiveMap.CreatePrimitiveMethodAccess(this, Caller as IPythonBlock, Method);
            }
            else if (Method is IAccessor)
            {
                var acc = (IAccessor)Method;
                if ((Method.DeclaringType.GetIsArray() || Method.DeclaringType.GetIsVector() || Method.DeclaringType.Equals(PrimitiveTypes.String)) && acc.DeclaringProperty.Name.ToString() == "Length")
                {
                    return new PartialInvocationBlock(this, new PythonIdentifierBlock(this, "len", PythonObjectType.Instance), Method.ReturnType, (IPythonBlock)Caller);
                }
                else if (acc.DeclaringProperty.GetIsIndexer() && acc.DeclaringProperty.Name.ToString() == "this")
                {
                    return new PartialIndexedBlock(this, (IPythonBlock)Caller, acc.AccessorType, Method.ReturnType);
                }
                else
                {
                    return new PartialPropertyAccess(this, (IPythonBlock)Caller, acc);
                }
            }
            else
            {
                return new MemberAccessBlock(this, (IPythonBlock)Caller, GetName(Method), MethodType.Create(Method));
            }
        }

        #endregion

        #region GetName

        protected IMemberNamer GetMemberNamer()
        {
            return Method.DeclaringType.DeclaringNamespace.DeclaringAssembly.GetMemberNamer();
        }

        public string GetName(IMethod Method)
        {
            return GetMemberNamer().Name(Method);
        }

        public string GetName(IType Type)
        {
            return GetMemberNamer().Name(Type);
        }

        #endregion

        #region Conversions

        public static bool AreEquivalent(IType Source, IType Target)
        {
            if (Source.Equals(PrimitiveTypes.Char) && Target.Equals(PrimitiveTypes.String))
            {
                return true;
            }
            else if ((Source.GetIsBit() || Source.GetIsInteger()) && (Target.GetIsBit() || Target.GetIsInteger()))
            {
                return true;
            }
            else if (Source.GetIsFloatingPoint() && Target.GetIsFloatingPoint())
            {
                return true;
            }
            else if ((!Source.Equals(PrimitiveTypes.String) && Target.Equals(PrimitiveTypes.String)) || (Source.Equals(PrimitiveTypes.String) && !Target.Equals(PrimitiveTypes.String)))
            {
                return false;
            }
            else if (Source.GetIsReferenceType() && Target.GetIsReferenceType())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private ICodeBlock EmitReinterpretCast(ICodeBlock Value, IType Type)
        {
            return new ImplicitlyConvertedBlock(this, (IPythonBlock)Value, Type); // No conversion necessary
        }

        private ICodeBlock EmitStaticCast(ICodeBlock Value, IType Type)
        {
            var pythonVal = (IPythonBlock)Value;
            var tVal = pythonVal.Type;
            string name;
            if (AreEquivalent(tVal, Type))
            {
                return new ImplicitlyConvertedBlock(this, pythonVal, Type); // No conversion necessary
            }
            else if ((tVal.GetIsInteger() || tVal.GetIsBit()) && Type.Equals(PrimitiveTypes.Char))
            {
                name = "chr";
            }
            else if ((Type.GetIsInteger() || Type.GetIsBit()) && tVal.Equals(PrimitiveTypes.Char))
            {
                name = "ord";
            }
            else if ((tVal.GetIsFloatingPoint() && Type.GetIsBit()) || (tVal.GetIsBit() && Type.GetIsFloatingPoint()))
            {
                return new FloatBitwiseConversionBlock(this, pythonVal, Type);
            }
            else
            {
                name = GetName(Type);
            }
            return new InvocationBlock(this, new PythonIdentifierBlock(this, name, PythonObjectType.Instance), new IPythonBlock[] { pythonVal }, Type);
        }

        #endregion

        #region Object Model

        public ICodeBlock EmitDefaultValue(IType Type)
        {
            if (Type.GetIsInteger() || Type.GetIsBit())
            {
                return EmitInt32(0);
            }
            else if (Type.GetIsFloatingPoint())
            {
                return EmitFloat64(0);
            }
            else if (Type.Equals(PrimitiveTypes.Boolean))
            {
                return EmitBoolean(false);
            }
            else if (Type.GetIsValueType())
            {
                throw new NotImplementedException();
            }
            else
            {
                return EmitNull();
            }
        }

        public ICodeBlock EmitIsOfType(IType Type, ICodeBlock Value)
        {
            return new InvocationBlock(this, new PythonIdentifierBlock(this, "isinstance", PythonObjectType.Instance), new IPythonBlock[] { (IPythonBlock)Value, new PythonIdentifierBlock(this, GetName(Type), PythonObjectType.Instance) }, PrimitiveTypes.Boolean);
        }

        public ICodeBlock EmitNewArray(IType ElementType, IEnumerable<ICodeBlock> Dimensions)
        {
            var dimArr = Dimensions.Cast<IPythonBlock>().ToArray();
            var arrType = ElementType.MakeArrayType(dimArr.Length);
            if (dimArr.Length == 1)
            {
                return new BinaryOperation(this, new NewListBlock(this, arrType, (IPythonBlock)EmitDefaultValue(ElementType)), Operator.Multiply, dimArr[0]);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public ICodeBlock EmitNewVector(IType ElementType, IReadOnlyList<int> Dimensions)
        {
            return EmitNewArray(ElementType, Dimensions.Select((item) => EmitInt32(item)));
        }

        #endregion

        #region Local Name Generation

        private string GetSuggestedName(IType Type, params string[] NameSuggestions)
        {
            foreach (var item in NameSuggestions)
            {
                if (!LocalNameExists(item))
                {
                    return GetLocalName(item);
                }
            }
            return GetLocalName(Type);
        }

        private string GetIntegerName()
        {
            return GetSuggestedName(PrimitiveTypes.Int32, "i", "j", "k");
        }

        private string GetFloatName()
        {
            return GetSuggestedName(PrimitiveTypes.Float64, "x", "y", "z", "u", "v", "w");
        }

        public bool LocalNameExists(string Name)
        {
            return generatedNames.Contains(Name);
        }
        public string GetLocalName(string RequestedName)
        {
            return GetLocalName(RequestedName, false);
        }
        private string GetLocalName(string RequestedName, bool PreferNumbered)
        {
            int count = 0;
            string newName = PreferNumbered ? RequestedName + "_0" : RequestedName;
            while (LocalNameExists(newName))
            {
                newName = RequestedName + "_" + count;
                count++;
            }
            generatedNames.Add(newName);
            return newName;
        }
        public string GetLocalName(IType Type)
        {
            if (Type.GetIsInteger())
            {
                return GetIntegerName();
            }
            else if (Type.GetIsFloatingPoint())
            {
                return GetFloatName();
            }
            else if (Type.Equals(PrimitiveTypes.Boolean))
            {
                return GetLocalName("flag");
            }
            return GetLocalName(PythonifyingMemberNamer.Pythonify(GetName(Type)));
        }
        public void ReleaseLocalName(string Name)
        {
            generatedNames.Remove(Name);
        }

        private List<string> generatedNames;

        #endregion

        #region Variables

        private Dictionary<UniqueTag, PythonLocalVariable> locals = new Dictionary<UniqueTag, PythonLocalVariable>();

        public IEmitVariable GetElement(ICodeBlock Value, IEnumerable<ICodeBlock> Index)
        {
            return new PythonIndexedVariable(this, (IPythonBlock)Value, Index.Cast<IPythonBlock>().ToArray());
        }

        public IEmitVariable GetField(IField Field, ICodeBlock Target)
        {
            return new PythonFieldVariable(this, (IPythonBlock)Target, Field);
        }

        public IEmitVariable GetLocal(UniqueTag Tag)
        {
            PythonLocalVariable result;
            if (locals.TryGetValue(Tag, out result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        public IEmitVariable DeclareLocal(UniqueTag Tag, IVariableMember VariableMember)
        {
            var result = new PythonLocalVariable(this, VariableMember);
            locals.Add(Tag, result);
            return result;
        }

        public IEmitVariable GetArgument(int Index)
        {
            return new PythonArgumentVariable(this, Index);
        }

        public IEmitVariable GetThis()
        {
            return new PythonThisVariable(this);
        }

        #endregion

        #region IYieldCodeGenerator Implementation

        public ICodeBlock EmitYieldBreak()
        {
            throw new NotImplementedException();
        }

        public ICodeBlock EmitYieldReturn(ICodeBlock Value)
        {
            return new YieldBlock(this, (IPythonBlock)Value);
        }

        #endregion

        #region IInitializingCodeGenerator Implementation

        public ICodeBlock EmitInitializedArray(IType ElementType, ICodeBlock[] Elements)
        {
            return new NewListBlock(this, ElementType.MakeArrayType(1), Elements.Cast<IPythonBlock>().ToArray());
        }

        public ICodeBlock EmitInitializedVector(IType ElementType, ICodeBlock[] Elements)
        {
            return new NewListBlock(this, ElementType.MakeVectorType(new int[] { Elements.Length }), Elements.Cast<IPythonBlock>().ToArray());
        }

        #endregion

    }
}
