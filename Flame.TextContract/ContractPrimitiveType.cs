﻿using Flame.Build;
using Flame.Compiler.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.TextContract
{
    public abstract class ContractPrimitiveType : IType
    {
        protected ContractPrimitiveType()
        {
        }

        public IContainerType AsContainerType()
        {
            return null;
        }

        public INamespace DeclaringNamespace
        {
            get
            {
                return null;
            }
        }

        public IEnumerable<IType> BaseTypes
        {
            get { return new IType[0]; }
        }

        public IBoundObject GetDefaultValue()
        {
            return NullExpression.Instance;
        }

        public IEnumerable<IField> Fields
        {
            get { return new IField[0]; }
        }

        public IEnumerable<IMethod> Methods
        {
            get
            {
                var paramlessCtor = new DescribedMethod("Create" + char.ToUpper(Name.ToString()[0]).ToString() + Name.ToString().Substring(1), this);
                paramlessCtor.IsConstructor = true;
                return new IMethod[]
                {
                    paramlessCtor
                };
            }
        }

        public IEnumerable<IProperty> Properties
        {
            get { return new IProperty[0]; }
        }

        public virtual QualifiedName FullName
        {
            get { return new QualifiedName(Name); }
        }

        public virtual AttributeMap Attributes
        {
            get { return AttributeMap.Empty; }
        }

        public abstract UnqualifiedName Name { get; }

        public virtual IEnumerable<IGenericParameter> GenericParameters
        {
            get { return new IGenericParameter[0]; }
        }

        public override string ToString()
        {
            return Name.ToString();
        }

        public IAncestryRules AncestryRules
        {
            get { return DefinitionAncestryRules.Instance; }
        }
    }
}
