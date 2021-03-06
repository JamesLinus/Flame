﻿using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilMethod : CecilMethodBase, ICecilMethod
    {
        public CecilMethod(MethodReference Method, CecilModule Module)
            : this((ICecilType)Module.ConvertStrict(Method.DeclaringType), Method)
        {
        }
        public CecilMethod(ICecilType DeclaringType, MethodReference Method)
            : base(DeclaringType)
        {
            this.Method = Method;
            this.baseMethods = new Lazy<IMethod[]>(FindBaseMethods);
        }

        public MethodReference Method { get; private set; }

        public override MethodReference GetMethodReference()
        {
            return Method;
        }
        public MethodDefinition GetResolvedMethod()
        {
            return Method.Resolve();
        }

        public override bool IsStatic
        {
            get
            {
                var resolved = GetResolvedMethod();
                return resolved == null ? !Method.HasThis : resolved.IsStatic;
            }
        }

        public override bool IsConstructor
        {
            get { return GetResolvedMethod().IsConstructor; }
        }

        public AccessModifier Access
        {
            get
            {
                var resolvedMethod = GetResolvedMethod();
                if (resolvedMethod.IsPublic)
                {
                    return AccessModifier.Public;
                }
                else if (resolvedMethod.IsAssembly)
                {
                    return AccessModifier.Assembly;
                }
                else if (resolvedMethod.IsFamilyOrAssembly)
                {
                    return AccessModifier.ProtectedOrAssembly;
                }
                else if (resolvedMethod.IsFamily)
                {
                    return AccessModifier.Protected;
                }
                else if (resolvedMethod.IsFamilyAndAssembly)
                {
                    return AccessModifier.ProtectedAndAssembly;
                }
                else
                {
                    return AccessModifier.Private;
                }
            }
        }

        protected override IEnumerable<IAttribute> GetMemberAttributes()
        {
            var resolvedMethod = GetResolvedMethod();
            List<IAttribute> attrs = new List<IAttribute>();
            attrs.Add(new AccessAttribute(Access));
            if (resolvedMethod.IsAbstract)
            {
                attrs.Add(PrimitiveAttributes.Instance.AbstractAttribute);
            }
            else if (resolvedMethod.IsVirtual && !resolvedMethod.IsFinal && !resolvedMethod.DeclaringType.IsValueType)
            {
                attrs.Add(PrimitiveAttributes.Instance.VirtualAttribute);
            }
            if (resolvedMethod.IsSpecialName)
            {
                var op = CecilOperatorNames.ParseOperatorName(resolvedMethod.Name);
                if (op.IsDefined)
                    attrs.Add(new OperatorAttribute(op));
            }
            else if (resolvedMethod.IsStatic && resolvedMethod.Name == "Concat" 
                && resolvedMethod.Parameters.Count == 2 
                && resolvedMethod.DeclaringType.Equals(resolvedMethod.Module.TypeSystem.String))
            {
                attrs.Add(new OperatorAttribute(Operator.Concat));
            }
            return attrs;
        }

        protected override IList<CustomAttribute> GetCustomAttributes()
        {
            return GetResolvedMethod().CustomAttributes;
        }

        private IMethod[] FindBaseMethods()
        {
            var cecilOverrides = GetResolvedMethod().Overrides;
            var overrides = new HashSet<IMethod>();
            for (int i = 0; i < cecilOverrides.Count; i++)
            {
                overrides.Add(Module.Convert(cecilOverrides[i]));
            }
            foreach (var bType in DeclaringType.BaseTypes)
            {
                foreach (var item in bType.GetAllMethods())
                {
                    if (item.HasSameSignature(this) && item.GetIsVirtual())
                    {
                        overrides.Add(item);
                    }
                }
            }
            if (this.DeclaringType.GetIsRootType())
            {
                var nameStr = this.Name.ToString();
                if (nameStr == "GetHashCode")
                {
                    overrides.Add(PrimitiveMethods.Instance.GetHashCode);
                }
                else if (nameStr == "Equals")
                {
                    overrides.Add(PrimitiveMethods.Instance.Equals);
                }
            }

            return overrides.ToArray();
        }

        private Lazy<IMethod[]> baseMethods;
        public override IEnumerable<IMethod> BaseMethods
        {
            get { return baseMethods.Value; }
        }
    }
}
