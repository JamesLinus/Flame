﻿using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Compiler.Statements;
using Flame.Compiler.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation.Emit
{
    public class ForeachBlockHeader : IForeachBlockHeader
    {
        public ForeachBlockHeader(RecompiledCodeGenerator CodeGenerator, UniqueTag Tag, IEnumerable<CollectionBlock> Collections)
        {
            var collElems = Collections.Select(item => new CollectionElement(item.Member, item.Collection)).ToArray();
            this.foreachStatement = new ForeachStatement(Tag, collElems);
        }

        public RecompiledCodeGenerator CodeGenerator { get; private set; }
        private ForeachStatement foreachStatement;
        private IReadOnlyList<IEmitVariable> elems;

        public IReadOnlyList<IEmitVariable> Elements
        {
            get
            {
                if (elems == null)
                {
                    var foreachElems = new List<IEmitVariable>();
                    foreach (var item in foreachStatement.Elements)
                    {
                        foreachElems.Add(new RecompiledVariable(CodeGenerator, item));
                    }
                    elems = foreachElems;
                }
                return elems;
            }
        }

        public ForeachStatement ToForeachStatement(IStatement Body)
        {
            foreachStatement.Body = Body;
            return foreachStatement;
        }
    }
}
