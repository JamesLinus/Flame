﻿using Flame.Build;
using Flame.Compiler.Build;
using Flame.Compiler.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class RecompiledTypeTemplate : RecompiledMemberTemplate<IType>, ITypeSignatureTemplate
    {
        protected RecompiledTypeTemplate(
            AssemblyRecompiler Recompiler, IType SourceType,
            MemberSignaturePassResult SignaturePassResult)
            : base(Recompiler, SignaturePassResult)
        {
            this.SourceType = SourceType;
        }

        #region Static

        public static RecompiledTypeTemplate GetRecompilerTemplate(
            AssemblyRecompiler Recompiler, IType SourceType)
        {
            return new RecompiledTypeTemplate(
                Recompiler, SourceType,
                Recompiler.Passes.ProcessSignature(Recompiler, SourceType));
        }

        #endregion

        public IType SourceType { get; private set; }
        public override IType GetSourceMember()
        {
            return SourceType;
        }

        public IEnumerable<IType> CreateBaseTypes(IType Type)
        {
            return Recompiler.GetTypes(SourceType.BaseTypes).ToArray();
        }

        public IEnumerable<IGenericParameter> CreateGenericParameters(IType Type)
        {
            return GenericExtensions.CloneGenericParameters(
                SourceType.GenericParameters, Type,
                new WeakTypeRecompilingVisitor(Recompiler, SourceType));
        }
    }
}
