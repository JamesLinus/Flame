﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public static class DependencyExtensions
    {
        #region GetDeclarationDependencies

        public static IEnumerable<IHeaderDependency> GetDeclarationDependencies(this IEnumerable<IMember> Members, params IMember[] Exclude)
        {
            return GetDeclarationDependencies(Members, (IEnumerable<IMember>)Exclude);
        }

        public static IEnumerable<IHeaderDependency> GetDeclarationDependencies(this IEnumerable<IMember> Members, IEnumerable<IMember> Exclude)
        {
            return Members.Aggregate(Enumerable.Empty<IHeaderDependency>(), (acc, item) => acc.Union(GetDeclarationDependencies(item, Exclude)));
        }

        public static IEnumerable<IHeaderDependency> GetDeclarationDependencies(this IMember Member, params IMember[] Exclude)
        {
            return GetDeclarationDependencies(Member, (IEnumerable<IMember>)Exclude);
        }

        public static IEnumerable<IHeaderDependency> GetDeclarationDependencies(this IMember Member, IEnumerable<IMember> Exclude)
        {
            if (Member is IMethod)
            {
                return GetDeclarationDependencies((IMethod)Member, Exclude);
            }
            else if (Member is IType)
            {
                return GetDeclarationDependencies((IType)Member, Exclude);
            }
            else if (Member is INamespace)
            {
                return GetDeclarationDependencies((INamespace)Member, Exclude);
            }
            else
            {
                return GetDependencies(Member, Exclude);
            }
        }

        public static IEnumerable<IHeaderDependency> GetDeclarationDependencies(this INamespace Namespace, IEnumerable<IMember> Exclude)
        {
            return Namespace.Types.Aggregate(Enumerable.Empty<IHeaderDependency>(), (a, b) => a.Union(b.GetDeclarationDependencies(Exclude)));
        }

        public static IEnumerable<IHeaderDependency> GetDeclarationDependencies(this IType Type, IEnumerable<IMember> Exclude)
        {
            if (Type is IDeclarationDependencyMember)
            {
                return ((IDeclarationDependencyMember)Type).DeclarationDependencies;
            }
            return Type.GetMethods().GetDeclarationDependencies(Exclude)
                                    .Union(Type.Methods.GetDeclarationDependencies(Exclude))
                                    .Union(Type.Properties.GetDeclarationDependencies(Exclude))
                                    .Union(Type.Fields.GetDeclarationDependencies(Exclude))
                                    .Union(Type.BaseTypes.GetDependencies(Exclude))
                                    .Union(Type is INamespace ? ((INamespace)Type).GetDeclarationDependencies(Exclude) : Enumerable.Empty<IHeaderDependency>());
        }

        public static IEnumerable<IHeaderDependency> GetDeclarationDependencies(this IMethod Method, IEnumerable<IMember> Exclude)
        {
            return Method.ReturnType.GetDependencies(Exclude).Union(Method.GetParameters().GetTypes().GetDependencies(Exclude));
        }

        #endregion

        #region GetDependencies

        public static IEnumerable<IHeaderDependency> GetDependencies(this IMember Member, params IMember[] Exclude)
        {
            return GetDependencies(Member, (IEnumerable<IMember>)Exclude);
        }
        public static IEnumerable<IHeaderDependency> GetDependencies(this IMember Member, IEnumerable<IMember> Exclude)
        {
            if (Exclude.Contains(Member))
            {
                return new IHeaderDependency[0];
            }
            if (Member is IType)
            {
                return GetDependencies((IType)Member, Exclude);
            }
            else if (Member is ICppMember)
            {
                return ((ICppMember)Member).Dependencies;
            }
            else if (Member is IParameter)
            {
                return ((IParameter)Member).ParameterType.GetDependencies();
            }
            else
            {
                return Member.GetAttributeDependencies();
            }
        }

        public static IEnumerable<IHeaderDependency> GetDependencies(this IType Type, IEnumerable<IMember> Exclude)
        {
            return new CppDependencyFinder(Exclude).Convert(Type);
        }

        public static IEnumerable<IHeaderDependency> GetDependencies(this IEnumerable<IMember> Members, params IMember[] Exclude)
        {
            return GetDependencies(Members, (IEnumerable<IMember>)Exclude);
        }

        public static IEnumerable<IHeaderDependency> GetDependencies(this IEnumerable<IMember> Members, IEnumerable<IMember> Exclude)
        {
            var depends = Members.Select((item) => item.GetDependencies(Exclude));
            if (!depends.Any())
            {
                return new IHeaderDependency[0];
            }
            else
            {
                return depends.Aggregate((first, second) => first.MergeDependencies(second));
            }
        }

        public static IEnumerable<IHeaderDependency> MergeDependencies(this IEnumerable<IHeaderDependency> Dependencies, IEnumerable<IHeaderDependency> Others)
        {
            return Dependencies.Union(Others, HeaderComparer.Instance);
        }

        public static IEnumerable<IHeaderDependency> SortDependencies(this IEnumerable<IHeaderDependency> Dependencies)
        {
            return Dependencies.OrderBy(item => item, HeaderComparer.Instance);
        }

        public static IEnumerable<IHeaderDependency> ExcludeDependencies(this IEnumerable<IHeaderDependency> Dependencies, IEnumerable<IHeaderDependency> Exclude)
        {
            return Dependencies.Except(Exclude, HeaderComparer.Instance);
        }

        #endregion
    }
}
