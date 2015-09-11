﻿using Flame.Compiler;
using Flame.Compiler.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Target
{
    public struct PassInfo<TIn, TOut> : IEquatable<PassInfo<TIn, TOut>>
    {
        public PassInfo(IPass<TIn, TOut> Pass, string Name, Func<OptimizationInfo, bool, bool> UsePass)
        {
            this = default(PassInfo<TIn, TOut>);
            this.Pass = Pass;
            this.Name = Name;
            this.UsePass = UsePass;
        }
        public PassInfo(IPass<TIn, TOut> Pass, string Name, bool IsDefault)
            : this(Pass, Name, (optInfo, preferred) => IsDefault)
        {
        }
        public PassInfo(IPass<TIn, TOut> Pass, string Name)
            : this(Pass, Name, (optInfo, preferred) => preferred)
        {
        }

        public IPass<TIn, TOut> Pass { get; private set; }
        public string Name { get; private set; }
        public Func<OptimizationInfo, bool, bool> UsePass { get; private set; }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            return obj is PassInfo<TIn, TOut> && Equals((PassInfo<TIn, TOut>)obj);
        }

        public bool Equals(PassInfo<TIn, TOut> other)
        {
            return Name == other.Name;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class PassPreferences
    {
        public PassPreferences()
            : this(Enumerable.Empty<string>())
        {
        }
        public PassPreferences(params string[] PreferredPasses)
            : this((IEnumerable<string>)PreferredPasses)
        {
        }
        public PassPreferences(IEnumerable<string> PreferredPasses)
            : this(PreferredPasses,
                   Enumerable.Empty<PassInfo<Tuple<IStatement, IMethod>, Tuple<IStatement, IMethod>>>())
        {
        }
        public PassPreferences(IEnumerable<string> PreferredPasses,
                       IEnumerable<PassInfo<BodyPassArgument, IStatement>> AdditionalMethodPasses)
            : this(PreferredPasses, 
                   Enumerable.Empty<PassInfo<Tuple<IStatement, IMethod>, Tuple<IStatement, IMethod>>>(),
                   AdditionalMethodPasses)
        {
        }
        public PassPreferences(IEnumerable<string> PreferredPasses,
                               IEnumerable<PassInfo<Tuple<IStatement, IMethod>, Tuple<IStatement, IMethod>>> AdditionalAnalysisPasses)
            : this(PreferredPasses, AdditionalAnalysisPasses,
                   Enumerable.Empty<PassInfo<BodyPassArgument, IStatement>>())
        {
        }
        public PassPreferences(IEnumerable<string> PreferredPasses,
                               IEnumerable<PassInfo<Tuple<IStatement, IMethod>, Tuple<IStatement, IMethod>>> AdditionalAnalysisPasses,
                               IEnumerable<PassInfo<BodyPassArgument, IStatement>> AdditionalMethodPasses)
            : this(PreferredPasses, AdditionalAnalysisPasses, AdditionalMethodPasses,
                   Enumerable.Empty<PassInfo<IStatement, IStatement>>())
        {
        }
        public PassPreferences(IEnumerable<string> PreferredPasses,
                               IEnumerable<PassInfo<Tuple<IStatement, IMethod>, Tuple<IStatement, IMethod>>> AdditionalAnalysisPasses,
                               IEnumerable<PassInfo<BodyPassArgument, IStatement>> AdditionalMethodPasses,
                               IEnumerable<PassInfo<IStatement, IStatement>> AdditionalPasses)
        {
            this.PreferredPasses = PreferredPasses;
            this.AdditionalAnalysisPasses = AdditionalAnalysisPasses;
            this.AdditionalMethodPasses = AdditionalMethodPasses;
            this.AdditionalStatementPasses = AdditionalPasses;
        }

        public IEnumerable<string> PreferredPasses { get; private set; }
        public IEnumerable<PassInfo<Tuple<IStatement, IMethod>, Tuple<IStatement, IMethod>>> AdditionalAnalysisPasses { get; private set; }
        public IEnumerable<PassInfo<BodyPassArgument, IStatement>> AdditionalMethodPasses { get; private set; }
        public IEnumerable<PassInfo<IStatement, IStatement>> AdditionalStatementPasses { get; private set; }

        public PassPreferences Union(PassPreferences Other)
        {
            return new PassPreferences(PreferredPasses.Union(Other.PreferredPasses),
                AdditionalAnalysisPasses.Union(Other.AdditionalAnalysisPasses),
                AdditionalMethodPasses.Union(Other.AdditionalMethodPasses),
                AdditionalStatementPasses.Union(Other.AdditionalStatementPasses));
        }
    }
}
