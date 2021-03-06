﻿using Flame.Compiler;
using Flame.Compiler.Statements;
using Flame.Compiler.Visitors;
using Flame.Recompilation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Passes
{
    /// <summary>
    /// A pass (two passes, actually) that creates
    /// forwarding wrappers around static extension
    /// properties.
    /// </summary>
    /// <remarks>
    /// This works around an `mcs` bootstrapping issue:
    /// `mcs` will not allow us to call (static) property
    /// accessors directly, and it doesn't offer us
    /// any way to call those properties.
    /// </remarks>
    public sealed class WrapExtensionPropertiesPass
    {
        private sealed class RootPass : IPass<BodyPassArgument, IEnumerable<IMember>>
        {
            public IEnumerable<IMember> Apply(BodyPassArgument Value)
            {
                if (IsApplicable(Value.DeclaringMethod))
                {
                    return new IMember[] { CreateForwardingMethod((IAccessor)Value.DeclaringMethod) };
                }
                else
                {
                    return Enumerable.Empty<IMember>();
                }
            }

            /// <summary>
            /// Creates a static method that forwards all calls
            /// it itself to the given static method.
            /// </summary>
            /// <param name="GetSingletonExpression"></param>
            /// <param name="OwnerType"></param>
            /// <param name="Method"></param>
            /// <returns></returns>
            private static IMethod CreateForwardingMethod(IAccessor Method)
            {
                var descMethod = new DescribedBodyMethod(Rename(Method), Method.DeclaringType, Method.ReturnType, true);
                foreach (var attr in Method.Attributes)
                {
                    descMethod.AddAttribute(attr);
                }
                foreach (var param in Method.GetParameters())
                {
                    descMethod.AddParameter(param);
                }

                descMethod.Body = new ReturnStatement(GenerateStaticPass.CreateForwardingCall(null, descMethod, Method));

                return descMethod;
            }
        }

        private sealed class SignaturePass : IPass<MemberSignaturePassArgument<IMember>, MemberSignaturePassResult>
        {
            public const string WrappedAccessorPrefix = "__acc$";

            public MemberSignaturePassResult Apply(MemberSignaturePassArgument<IMember> Value)
            {
                // If this pass is applicable to the given member, then
                // we want to prefix the original member's name, to make
                // sure that it:
                //   1. is well-hidden;
                //   2. does not clash with the newly generated method's name.

                if (IsApplicable(Value.Member))
                {
                    return new MemberSignaturePassResult(
                        WrappedAccessorPrefix + Value.Member.Name, 
                        new IAttribute[] { PrimitiveAttributes.Instance.HiddenAttribute });
                }
                else
                {
                    return new MemberSignaturePassResult();
                }
            }
        }

        /// <summary>
        /// Gets this pass' name.
        /// </summary>
        public const string WrapExtensionPropertiesPassName = "wrap-extension-properties";

        /// <summary>
        /// Gets the root pass component of this transformation.
        /// </summary>
        public static readonly IPass<BodyPassArgument, IEnumerable<IMember>> RootPassInstance = new RootPass();

        /// <summary>
        /// Gets the signature pass component of this transformation.
        /// </summary>
        public static readonly IPass<MemberSignaturePassArgument<IMember>, MemberSignaturePassResult> SignaturePassInstance = new SignaturePass();

        /// <summary>
        /// Checks if this pass is applicable to the given member.
        /// </summary>
        /// <param name="Member"></param>
        /// <returns></returns>
        public static bool IsApplicable(IMember Member)
        {
            // Only forward static, public, extension,
            // non-generic accessors.

            var acc = Member as IAccessor;
            return acc != null && acc.IsStatic && acc.GetIsExtension() && 
                   acc.GetAccess() == AccessModifier.Public &&
                   !acc.GetIsGenericInstance() && !acc.GetIsGeneric();
        }

        /// <summary>
        /// Converts the first character of the given
        /// string to upper-case, and converts the rest
        /// of the string to lower-case.
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        private static string FirstCharToUpper(string Name)
        {
            int firstIndex = Name.Length > 0 ? 1 : 0;
            return Name.Substring(0, firstIndex).ToUpper() + Name.Substring(firstIndex).ToLower();
        }

        /// <summary>
        /// Creates a new name for the given 
        /// static extension property accessor.
        /// </summary>
        /// <param name="Accessor"></param>
        /// <returns></returns>
        public static string Rename(IAccessor Accessor)
        {
            var declProp = Accessor.DeclaringProperty;
            string accKind = FirstCharToUpper(Accessor.AccessorType.Name);
            return accKind + declProp.Name;
        }
    }
}
