﻿using Flame.Build;
using Flame.Compiler.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public abstract class PythonPrimitiveType : IType
    {
        protected PythonPrimitiveType()
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

        public virtual IEnumerable<IField> Fields
        {
            get { return new IField[0]; }
        }

        public virtual IEnumerable<IMethod> Methods
        {
            get
            {
                var paramlessCtor = new DescribedMethod("__init__", this);
                paramlessCtor.ReturnType = PrimitiveTypes.Void;
                paramlessCtor.IsConstructor = true;
                return new IMethod[]
                {
                    paramlessCtor
                };
            }
        }

        public virtual IEnumerable<IProperty> Properties
        {
            get { return new IProperty[0]; }
        }

        public QualifiedName FullName
        {
            get { return new QualifiedName(Name); }
        }

        public virtual AttributeMap Attributes
        {
            get { return AttributeMap.Empty; }
        }

        public abstract UnqualifiedName Name { get; }

        public IEnumerable<IGenericParameter> GenericParameters
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
