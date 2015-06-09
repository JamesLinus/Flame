﻿using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Compiler.Variables;
using Flame.Compiler.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    /// <summary>
    /// A recompilation pass based on a visitor.
    /// </summary>
    public class VisitorRecompilationPass : IPass<RecompilationPassArguments, INode>
    {
        private VisitorRecompilationPass()
        {

        }

        static VisitorRecompilationPass()
        {
            Instance = new VisitorRecompilationPass();
        }

        public static VisitorRecompilationPass Instance { get; private set; }

        public INode Apply(RecompilationPassArguments Value)
        {
            var visitor = new RecompilingVisitor(Value.Recompiler);

            return Value.Body is IExpression ? (INode)visitor.Visit((IExpression)Value.Body) : (INode)visitor.Visit((IStatement)Value.Body);
        }
    }

    public class DelegateConverter<TIn, TOut> : IConverter<TIn, TOut>
    {
        public DelegateConverter(Func<TIn, TOut> Delegate)
        {
            this.Delegate = Delegate;
        }

        public Func<TIn, TOut> Delegate { get; private set; }

        public TOut Convert(TIn Value)
        {
            return Delegate(Value);
        }
    }

    public class RecompilingVisitor : VariableSubstitutingVisitorBase
    {
        public RecompilingVisitor(AssemblyRecompiler Recompiler)
        {
            this.Converter = new MemberConverter(new DelegateConverter<IType, IType>(Recompiler.GetType),
                                                 new DelegateConverter<IMethod, IMethod>(Recompiler.GetMethod),
                                                 new DelegateConverter<IField, IField>(Recompiler.GetField));
            this.recompiledLocals = new Dictionary<IVariable, IVariable>();
        }
        public RecompilingVisitor(MemberConverter Converter)
        {
            this.Converter = Converter;
            this.recompiledLocals = new Dictionary<IVariable,IVariable>();
        }

        private Dictionary<IVariable, IVariable> recompiledLocals;

        public MemberConverter Converter { get; private set; }

        public override bool Matches(IExpression Value)
        {
            return Value is IMemberNode || base.Matches(Value);
        }

        public override bool Matches(IStatement Value)
        {
            return Value is IMemberNode || base.Matches(Value);
        }

        protected override IExpression Transform(IExpression Expression)
        {
            if (Expression is IMemberNode)
            {
                var memberNode = (IExpression)((IMemberNode)Expression).ConvertMembers(Converter);
                if (base.Matches(memberNode))
                {
                    return base.Transform(memberNode);
                }
                else
                {
                    return memberNode.Accept(this);
                }
            }
            else
            {
                return base.Transform(Expression);
            }
        }

        protected override IStatement Transform(IStatement Statement)
        {
            if (Statement is IMemberNode)
            {
                var memberNode = (IStatement)((IMemberNode)Statement).ConvertMembers(Converter);
                if (base.Matches(memberNode))
                {
                    return base.Transform(memberNode);
                }
                else
                {
                    return memberNode.Accept(this);
                }
            }
            else
            {
                return base.Transform(Statement);
            }
        }

        protected override bool CanSubstituteVariable(IVariable Variable)
        {
            return Variable is LateBoundVariable && !(Variable is ManuallyBoundVariable);
        }

        protected override IVariable SubstituteVariable(IVariable Variable)
        {
            if (recompiledLocals.ContainsKey(Variable))
            {
                return recompiledLocals[Variable];
            }
            else
            {
                var oldVar = (LateBoundVariable)Variable;
                var newVar = new LateBoundVariable(new RetypedVariableMember(oldVar.Member, Converter.Convert(oldVar.Type)));
                recompiledLocals[Variable] = newVar;
                return newVar;
            }
        }
    }
}