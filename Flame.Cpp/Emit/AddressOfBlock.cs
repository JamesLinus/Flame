﻿using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class AddressOfBlock : IOpBlock, IPointerBlock
    {
        public AddressOfBlock(ICppBlock Target)
        {
            this.Target = Target;
        }

        public ICppBlock Target { get; private set; }
        public int Precedence { get { return 3; } }

        public IType Type
        {
            get { return Target.Type.MakePointerType(PointerKind.TransientPointer); }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Target.Dependencies; }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Target.LocalsUsed; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Target.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append('&');
            cb.AppendAligned(Target.GetOperandCode(this));
            return cb;
        }

        public ICppBlock StaticDereference()
        {
            return Target;
        }
    }
}
