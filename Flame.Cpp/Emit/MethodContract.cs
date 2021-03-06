﻿using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class MethodContract
    {
        public MethodContract(ICodeGenerator CodeGenerator, ICppBlock LocalPrecondition, ICppBlock LocalPostcondition)
        {
            this.CodeGenerator = CodeGenerator;
            this.LocalPreconditions = new ICppBlock[] { LocalPrecondition };
            this.LocalPostconditions = new ICppBlock[] { LocalPostcondition };
        }
        public MethodContract(ICodeGenerator CodeGenerator, IEnumerable<ICppBlock> LocalPreconditions, IEnumerable<ICppBlock> LocalPostconditions)
        {
            this.CodeGenerator = CodeGenerator;
            this.LocalPreconditions = LocalPreconditions;
            this.LocalPostconditions = LocalPostconditions;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IEnumerable<ICppBlock> LocalPreconditions { get; private set; }
        public IEnumerable<ICppBlock> LocalPostconditions { get; private set; }

        private ICppBlock[] preconds;
        private ICppBlock[] postconds;
        private ICppBlock invariantCheck;

        public IType DeclaringType { get { return CodeGenerator.Method.DeclaringType; } }
        public IMethod Method { get { return CodeGenerator.Method; } }

        private bool EmitInvariantPrecondition
        {
            get { return !Method.IsConstructor && Method.GetAccess() != AccessModifier.Private; }
        }

        private bool EmitInvariantPostcondition
        {
            get
            {
                return Method.GetAccess() != AccessModifier.Private &&
                       (!Method.GetIsConstant() || Method.IsConstructor ||
                       CodeGenerator.GetEnvironment().Log.Options.GetOption<bool>("ensure-const-invariants", false));
            }
        }

        public IEnumerable<ICppBlock> Preconditions
        {
            get
            {
                if (preconds == null)
                {
                    preconds = WithInvariantsAssertion(LocalPreconditions, EmitInvariantPrecondition, block => new PreconditionBlock(block)).ToArray();
                }
                return preconds;
            }
        }
        public IEnumerable<ICppBlock> Postconditions
        {
            get
            {
                if (postconds == null)
                {
                    postconds = WithInvariantsAssertion(LocalPostconditions, EmitInvariantPostcondition, block => new PostconditionBlock(block)).ToArray();
                }
                return postconds;
            }
        }

        private ICppBlock GetInvariantsCheck()
        {
            if (invariantCheck == null)
            {
                var checkMethod = DeclaringType.GetInvariantsCheckMethod();
                if (checkMethod != null && !checkMethod.Equals(Method) && !DeclaringType.GetInvariantsCheckImplementationMethod().Equals(Method))
                {
                    invariantCheck = (ICppBlock)CodeGenerator.EmitInvocation(checkMethod, CodeGenerator.GetThis().EmitGet(), Enumerable.Empty<ICodeBlock>());
                }
            }
            return invariantCheck;
        }

        private IEnumerable<ICppBlock> WithInvariantsAssertion<T>(IEnumerable<ICppBlock> Assertions, bool EmitInvariant, Func<ICppBlock, T> AssertionBuilder)
            where T : ICppBlock
        {
            if (!EmitInvariant)
            {
                return Assertions;
            }
            var check = GetInvariantsCheck();
            if (check == null)
            {
                return Assertions;
            }
            else
            {
                return Assertions.With(AssertionBuilder(check));
            }
        }

        /// <summary>
        /// Gets a boolean flag that indicates if this method contract has preconditions. 
        /// </summary>
        public bool HasPreconditions
        {
            get
            {
                return Preconditions.Any();
            }
        }

        /// <summary>
        /// Gets a boolean flag that indicates if this method contract has postconditions. 
        /// </summary>
        public bool HasPostconditions
        {
            get
            {
                return Postconditions.Any();
            }
        }

        /// <summary>
        /// Gets a sequence of precondition/postcondition description attributes.
        /// </summary>
        public IEnumerable<DescriptionAttribute> DescriptionAttributes
        {
            get
            {
                List<DescriptionAttribute> attrs = new List<DescriptionAttribute>();
                foreach (var item in Preconditions)
                {
                    attrs.Add(new DescriptionAttribute("pre", item.GetCode().ToString()));
                }
                foreach (var item in Postconditions)
                {
                    attrs.Add(new DescriptionAttribute("post", item.GetCode().ToString()));
                }
                return attrs;
            }
        }
    }
}
