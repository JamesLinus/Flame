﻿using System;
using System.Collections.Generic;
using Wasm.Instructions;

namespace Flame.Wasm.Emit
{
    public sealed class DelegateBlock : CodeBlock
    {
        public DelegateBlock(
            WasmCodeGenerator CodeGenerator, CodeBlock Target, 
            IMethod Method, Operator Op)
            : base(CodeGenerator)
        {
            this.Target = Target;
            this.Method = Method;
            this.Op = Op;
        }

        public CodeBlock Target { get; private set; }
        public IMethod Method { get; private set; }
        public Operator Op { get; private set; }

        /// <inheritdoc/>
        public override IType Type { get { return MethodType.Create(Method); } }

        /// <inheritdoc/>
        public override WasmExpr ToExpression(BlockContext Context, WasmFileBuilder File)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// A code block that performs a direct call.
    /// </summary>
    public sealed class CallBlock : CodeBlock
    {
        public CallBlock(
            WasmCodeGenerator CodeGenerator,
            WasmMethod Callee,
            IType Type,
            IReadOnlyList<CodeBlock> Arguments)
            : base(CodeGenerator)
        {
            this.Callee = Callee;
            this.ty = Type;
            this.Arguments = Arguments;
        }

        /// <summary>
        /// Gets the method that is called by this block.
        /// </summary>
        /// <returns>The method that is called.</returns>
        public WasmMethod Callee { get; private set; }

        private IType ty;

        /// <summary>
        /// Gets the list of arguments for the call.
        /// </summary>
        /// <returns>The argument list.</returns>
        public IReadOnlyList<CodeBlock> Arguments { get; private set; }

        /// <summary>
        /// Gets the result type of this call block. This is not
        /// necessarily the same type as the callee's return type:
        /// the ABI may change the return type of the WebAssembly
        /// function relative to the original IR function.
        /// </summary>
        /// <returns>The result type of this call block.</returns>
        public override IType Type => ty;

        public override WasmExpr ToExpression(BlockContext Context, WasmFileBuilder File)
        {
            WasmExpr args = null;
            foreach (var item in Arguments)
            {
                var expr = item.ToExpression(Context, File);
                if (args == null)
                    args = expr;
                else
                    args = args.Concat(expr);
            }
            return args.Append(Operators.Call.Create(File.GetMethodIndex(Callee)));
        }
    }
}

