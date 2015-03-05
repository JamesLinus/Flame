﻿using Flame.Build;
using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class CppType : ITypeBuilder, ICppTemplateMember, IGenericResolverType, INamespaceBranch, INamespaceBuilder, IEquatable<IType>
    {
        public CppType(INamespace DeclaringNamespace, IType Template, ICppEnvironment Environment)
        {
            this.DeclaringNamespace = DeclaringNamespace;
            this.Template = Template;
            this.Templates = new CppTemplateDefinition(this, Template);
            this.Environment = new TemplatedMemberCppEnvironment(Environment, this);
            CreateMemberCache();
        }

        public INamespace DeclaringNamespace { get; private set; }
        public IType Template { get; private set; }
        public CppTemplateDefinition Templates { get; private set; }
        public ICppEnvironment Environment { get; private set; }

        public Func<INamespace, IConverter<IType, string>> TypeNamer { get { return Environment.TypeNamer; } }

        public IType ResolveTypeParameter(IGenericParameter TypeParameter)
        {
            return TypeParameter;
        }

        #region Member Cache

        private void CreateMemberCache()
        {
            this.fields = new List<CppField>();
            this.methods = new List<CppMethod>();
            this.properties = new List<CppProperty>();
            this.types = new List<CppType>();
            this.friendMethods = new List<CppMethod>();
        }

        private List<CppField> fields;
        private List<CppMethod> methods;
        private List<CppProperty> properties;
        private List<CppType> types;
        private List<CppMethod> friendMethods;

        #endregion

        #region Type properties

        private string cachedFullName;
        public string FullName
        {
            get
            {
                if (cachedFullName == null)
                {
                    cachedFullName = MemberExtensions.CombineNames(DeclaringNamespace.FullName, Name);
                }
                return cachedFullName;
            }
        }

        public IEnumerable<IAttribute> GetAttributes()
        {
            return Template.GetAttributes();
        }

        private string cachedName;
        public string Name
        {
            get
            {
                if (cachedName == null)
                {
                    cachedName = Template.GetGenericFreeName();
                }
                return cachedName;
            }
        }

        public IContainerType AsContainerType()
        {
            return null;
        }

        public bool IsContainerType
        {
            get { return false; }
        }

        public IType[] BaseTypes { get { return GetBaseTypes(); } }
        public IType[] GetBaseTypes()
        {
            return Template.GetBaseTypes().Select(this.ConvertValueType).ToArray();
        }

        public IBoundObject GetDefaultValue()
        {
            throw new NotImplementedException();
        }

        public IArrayType MakeArrayType(int Rank)
        {
            return new DescribedArrayType(this, Rank);
        }

        public IPointerType MakePointerType(PointerKind PointerKind)
        {
            return new DescribedPointerType(this, PointerKind);
        }

        public IVectorType MakeVectorType(int[] Dimensions)
        {
            return new DescribedVectorType(this, Dimensions);
        }

        #endregion

        #region C++ type properties

        /// <summary>
        /// Gets a boolean value that indicates whether the given type should be displayed as a struct.
        /// </summary>
        public bool IsStruct
        {
            get
            {
                return GetMembers().All((item) => item.get_Access() == AccessModifier.Public);
            }
        }

        /// <summary>
        /// Gets a boolean flag that tells if this type is a static singleton.
        /// </summary>
        public bool IsStaticSingleton
        {
            get
            {
                if (DeclaringNamespace is IType)
                {
                    return this.get_IsSingleton() && this.Name == "Static_Singleton";
                }
                else
                {
                    return false;
                }
            }
        }

        public bool IsEmptyStaticSingleton
        {
            get
            {
                if (IsStaticSingleton)
                {
                    return fields.Count == 1 && methods.Count == 1 && properties.Count == 1;
                }
                return false;
            }
        }

        public bool EmitType
        {
            get
            {
                return !IsEmptyStaticSingleton;
            }
        }

        #endregion

        #region Members

        private bool ShouldCompileToParentFriend(IMethod Method)
        {
            return Method.get_IsOperator() && IsStaticSingleton;
        }

        private IMethodBuilder CompileToParentFriend(IMethod Method)
        {
            var declParent = (CppType)((IType)DeclaringNamespace).GetGenericDeclaration();
            return declParent.DeclareFriendMethod(Method);
        }

        public IFieldBuilder DeclareField(IField Template)
        {
            var field = new CppField(this, Template, Environment);
            fields.Add(field);
            return field;
        }

        public IMethodBuilder DeclareMethod(IMethod Template)
        {
            if (ShouldCompileToParentFriend(Template))
            {
                return CompileToParentFriend(Template);
            }
            else
            {
                var method = new CppMethod(this, Template, Environment);
                methods.Add(method);
                return method;
            }
        }

        public IMethodBuilder DeclareFriendMethod(IMethod Template)
        {
            var method = new CppMethod(this, Template, Environment, true, true);
            friendMethods.Add(method);
            return method; 
        }

        public IPropertyBuilder DeclareProperty(IProperty Template)
        {
            var property = new CppProperty(this, Template, Environment);
            properties.Add(property);
            return property;
        }

        public IField[] GetFields()
        {
            return fields.ToArray();
        }

        public IMethod[] GetConstructors()
        {
            return methods.Where((item) => item.IsConstructor).With<IMethod>(new ImplicitCopyConstructor(this)).ToArray();
        }

        public IMethod[] GetMethods()
        {
            return methods.Where((item) => !item.IsConstructor).ToArray();
        }

        public IProperty[] GetProperties()
        {
            return properties.ToArray();
        }

        public ITypeMember[] GetMembers()
        {
            return GetCppMembers().OfType<ITypeMember>().ToArray();
        }

        public IEnumerable<ICppMember> GetCppMembers()
        {
            return fields.Concat<ICppMember>(methods).Concat(properties).Concat(types);
        }

        #endregion

        #region Nested types

        public IEnumerable<INamespaceBranch> GetNamespaces()
        {
            return types;
        }

        public IAssembly DeclaringAssembly
        {
            get { return DeclaringNamespace.DeclaringAssembly; }
        }

        public IType[] GetTypes()
        {
            return types.ToArray();
        }

        public INamespaceBuilder DeclareNamespace(string Name)
        {
            return (INamespaceBuilder)DeclareType(new DescribedType(Name, this));
        }

        public ITypeBuilder DeclareType(IType Template)
        {
            var type = new CppType((INamespace)this.MakeGenericType(this.GetGenericParameters()), Template, Environment);
            types.Add(type);
            return type;
        }

        #endregion

        #region Generics

        public IType GetGenericDeclaration()
        {
            return this;
        }

        public IEnumerable<IType> GetGenericArguments()
        {
            return new IType[0];
        }

        public IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return this.Templates.GetGenericParameters();
        }

        public IType MakeGenericType(IEnumerable<IType> TypeArguments)
        {
            if (!TypeArguments.Any())
            {
                return this;
            }
            return new DescribedGenericTypeInstance(this, TypeArguments);
        }

        #endregion

        public IType Build()
        {
            return this;
        }

        INamespace IMemberBuilder<INamespace>.Build()
        {
            return this;
        }

        #region ICppMember Implementation

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return GetMembers().GetDependencies().MergeDependencies(GetBaseTypes().GetDependencies()); }
        }

        #region MemberToAccessGroup

        private static void MemberToAccessGroup(ICppMember Member, IDictionary<string, IList<ICppMember>> AccessGroups)
        {
            string modifier;
            switch (Member.get_Access())
            {
                case AccessModifier.Private:
                    modifier = "private";
                    break;
                case AccessModifier.Protected:
                case AccessModifier.ProtectedAndAssembly:
                    modifier = "protected";
                    break;
                default:
                    modifier = "public";
                    break;
            }
            if (!AccessGroups.ContainsKey(modifier))
            {
                AccessGroups[modifier] = new List<ICppMember>();
            }
            AccessGroups[modifier].Add(Member);
        }

        #endregion

        private CodeBuilder GetInheritanceCode()
        {
            var bTypes = GetBaseTypes();
            if (bTypes.Length == 0)
            {
                return new CodeBuilder();
            }
            else
            {
                var cb = new CodeBuilder();
                cb.Append(" : public ");
                cb.Append(TypeNamer.Name(bTypes[0], (IType)this));
                for (int i = 1; i < bTypes.Length; i++)
                {
                    cb.Append(", public ");
                    cb.Append(TypeNamer.Name(bTypes[i], (IType)this));
                }
                return cb;
            }
        }

        public CodeBuilder GetDeclarationCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.AddCodeBuilder(Templates.GetHeaderCode());
            cb.AddLine(IsStruct ? "struct " : "class ");
            cb.Append(this.GetGenericFreeName());
            cb.Append(GetInheritanceCode());
            return cb;
        }

        public CodeBuilder GetHeaderCode()
        {
            CodeBuilder cb = new CodeBuilder();

            if (EmitType)
            {
                cb.AddCodeBuilder(this.GetDocumentationComments());
                cb.AddCodeBuilder(GetDeclarationCode());
                cb.AddLine("{");

                var groups = new Dictionary<string, IList<ICppMember>>();
                foreach (var item in GetCppMembers())
                {
                    MemberToAccessGroup(item, groups);
                }
                bool isStruct = IsStruct;
                foreach (var group in groups.OrderByDescending((item) => item.Key, new AccessStringComparer()))
                {
                    if (!isStruct)
                    {
                        cb.AddLine(group.Key + ":");
                    }
                    cb.IncreaseIndentation();
                    foreach (var item in Environment.TypeDefinitionPacker.Pack(group.Value).Where(item => !item.IsEmpty))
                    {
                        cb.AddCodeBuilder(item.GetHeaderCode());
                        cb.AddEmptyLine();
                    }
                    cb.TrimEnd(); // Gets rid of excessive whitespace
                    cb.DecreaseIndentation();
                }

                cb.AddLine("};");   
            }

            foreach (var item in friendMethods)
            {
                cb.TrimEnd();
                cb.AddEmptyLine();
                cb.AddCodeBuilder(item.GetHeaderCode());
            }

            return cb;
        }

        public bool HasSourceCode
        {
            get { return GetCppMembers().Any(item => item.HasSourceCode); }
        }

        public CodeBuilder GetSourceCode()
        {
            CodeBuilder cb = new CodeBuilder();
            if (EmitType)
            {
                foreach (var item in GetCppMembers().Where(item => item.HasSourceCode))
                {
                    cb.AddCodeBuilder(item.GetSourceCode());
                    cb.AddEmptyLine();
                }
            }
            foreach (var item in friendMethods)
            {
                cb.TrimEnd();
                cb.AddEmptyLine();
                cb.AddCodeBuilder(item.GetSourceCode());
            }
            return cb;
        }

        public override string ToString()
        {
            return GetDeclarationCode().ToString();
        }

        #endregion

        #region Equals/GetHashCode

        public override bool Equals(object obj)
        {
            if (obj is IType)
            {
                return Equals((IType)obj);
            }
            else
            {
                return false;
            }
        }

        public bool Equals(IType other)
        {
            return this.FullName == other.FullName;
        }

        public override int GetHashCode()
        {
            return FullName.GetHashCode();
        }

        #endregion
    }
}
