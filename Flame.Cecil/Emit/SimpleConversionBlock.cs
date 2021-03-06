﻿using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class SimpleConversionBlock : ICecilBlock
    {
        public SimpleConversionBlock(ICecilBlock Value, IType TargetType, OpCode Op, IType OpType)
        {
            this.Value = Value;
            this.TargetType = TargetType;
            this.Op = Op;
            this.OpType = OpType;
        }

        public ICecilBlock Value { get; private set; }
        public IType TargetType { get; private set; }
        public OpCode Op { get; private set; }
        public IType OpType { get; private set; }

        public ICodeGenerator CodeGenerator
        {
            get { return Value.CodeGenerator; }
        }

        public void Emit(IEmitContext Context)
        {
            Value.Emit(Context);
            Context.Stack.Pop();
            
            Context.Emit(Op, OpType);

            Context.Stack.Push(TargetType);
        }

        public IType BlockType
        {
            get { return TargetType; }
        }
    }
}
