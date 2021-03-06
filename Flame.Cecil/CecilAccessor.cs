﻿using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilAccessor : CecilMethodBase, IAccessor
    {
        public CecilAccessor(ICecilProperty DeclaringProperty, ICecilMethod Method, AccessorType AccessorType)
            : base((ICecilType)DeclaringProperty.DeclaringType)
        {
            this.DeclaringProperty = DeclaringProperty;
            this.Method = Method;
            this.AccessorType = AccessorType;
        }
        public CecilAccessor(ICecilProperty DeclaringProperty, MethodDefinition Method, AccessorType AccessorType)
            : this(DeclaringProperty, new CecilMethod((ICecilType)DeclaringProperty.DeclaringType, Method), AccessorType)
        {
        }

        public AccessorType AccessorType { get; private set; }
        public ICecilProperty DeclaringProperty { get; private set; }
        IProperty IAccessor.DeclaringProperty
        {
            get { return DeclaringProperty; }
        }
        public ICecilMethod Method { get; private set; }

        public override MethodReference GetMethodReference()
        {
            return Method.GetMethodReference();
        }

        public override bool IsConstructor
        {
            get { return Method.IsConstructor; }
        }

        public override bool IsStatic
        {
            get { return Method.IsStatic; }
        }

        public override IType ReturnType
        {
            get
            {
                return Method.ReturnType;
            }
        }

        public override IParameter[] GetParameters()
        {
            return Method.GetParameters();
        }

        public override UnqualifiedName Name
        {
            get
            {
                return Method.Name;
            }
        }

        public override QualifiedName FullName
        {
            get
            {
                return Method.FullName;
            }
        }

        protected override AttributeMap GetAttributes()
        {
            return Method.Attributes;
        }

        protected override IEnumerable<IAttribute> GetMemberAttributes()
        {
            return AttributeMap.Empty;
        }

        protected override IList<CustomAttribute> GetCustomAttributes()
        {
            return new CustomAttribute[0];
        }

        public override IEnumerable<IMethod> BaseMethods
        {
            get { return Method.BaseMethods; }
        }
    }
}
