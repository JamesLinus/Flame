﻿using Flame.Compiler.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.TextContract
{
    public class ContractAccessor : ContractMethod, IAccessor
    {
        public ContractAccessor(IProperty DeclaringProperty, AccessorType Kind, IMethodSignatureTemplate Template)
            : base(DeclaringProperty.DeclaringType, Template)
        {
            this.AccessorType = Kind;
            this.DeclaringProperty = DeclaringProperty;
        }

        public AccessorType AccessorType { get; private set; }
        public IProperty DeclaringProperty { get; private set; }

        public override UnqualifiedName Name
        {
            get
            {
                if (AccessorType.Equals(AccessorType.GetAccessor))
                {
                    return new SimpleName("get");
                }
                else if (AccessorType.Equals(AccessorType.SetAccessor))
                {
                    return new SimpleName("set");
                }
                else
                {
                    return new SimpleName(AccessorType.ToString().ToLower());
                }
            }
        }
    }
}
