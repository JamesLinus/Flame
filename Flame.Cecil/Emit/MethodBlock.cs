﻿using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class MethodBlock : ICecilBlock
    {
        public MethodBlock(ICodeGenerator CodeGenerator, IMethod Method, ICecilBlock Caller, bool IsVirtual, ICecilType DelegateType)
        {
            System.Diagnostics.Debug.Assert(CodeGenerator != null);
            System.Diagnostics.Debug.Assert(Method != null);
            this.CodeGenerator = CodeGenerator;
            this.Method = Method;
            this.Caller = Caller;
            this.IsVirtual = IsVirtual;
            this.delegateType = new Lazy<IType>(() => DelegateType);
        }
        public MethodBlock(ICodeGenerator CodeGenerator, IMethod Method, ICecilBlock Caller, bool IsVirtual)
        {
            System.Diagnostics.Debug.Assert(CodeGenerator != null);
            System.Diagnostics.Debug.Assert(Method != null);
            this.CodeGenerator = CodeGenerator;
            this.Method = Method;
            this.Caller = Caller;
            this.IsVirtual = IsVirtual;
            this.delegateType = new Lazy<IType>(() => CodeGenerator.GetModule().TypeSystem.GetCanonicalDelegate(Method));
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IMethod Method { get; private set; }
        public ICecilBlock Caller { get; private set; }
        public bool IsVirtual { get; private set; }

        private Lazy<IType> delegateType;
        public IType DelegateType { get { return delegateType.Value; } }

        public MethodBlock ChangeDelegateType(ICecilType NewDelegateType)
        {
            return new MethodBlock(CodeGenerator, Method, Caller, IsVirtual, NewDelegateType);
        }

        public static void EmitCaller(ICecilBlock Caller, IMethod Target, IEmitContext Context)
        {
            Caller.Emit(Context);
            var type = Context.Stack.Peek();
            if (!type.GetIsPointer() && ILCodeGenerator.IsPossibleValueType(type)) // Sometimes the address of a value type has to be taken
            {
                Context.EmitPushPointerCommands((IUnmanagedCodeGenerator)Caller.CodeGenerator, type, true);
            }
        }

        public void Emit(IEmitContext Context)
        {
            if (Caller == null)
            {
                Context.Emit(OpCodes.Ldnull);
            }
            else
            {
                EmitCaller(Caller, Method, Context);
                var callerType = Context.Stack.Pop();
                if (callerType.GetIsValueType())
                {
                    // Box value types
                    Context.Emit(OpCodes.Box, callerType);
                }
            }
            // Push a function pointer on the stack.
            if (IsVirtual)
            {
                Context.Emit(OpCodes.Dup);
                Context.Emit(OpCodes.Ldvirtftn, Method);
            }
            else
            {
                Context.Emit(OpCodes.Ldftn, Method);
            }

            Context.Emit(OpCodes.Newobj, DelegateType.GetConstructors().Single());

            Context.Stack.Push(DelegateType);
        }

        public IType BlockType
        {
            get { return DelegateType; }
        }
    }
}
