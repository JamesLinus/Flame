﻿using Flame.Compiler;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConstructorInfo = System.Reflection.ConstructorInfo;

namespace Flame.Cecil
{
    public class CecilAttribute : IConstructedAttribute
    {
        public CecilAttribute(CustomAttribute Attribute, ICecilMember ImportingMember)
        {
            this.Attribute = Attribute;
            this.importingModule = ImportingMember.Module;
        }
        public CecilAttribute(CustomAttribute Attribute, CecilModule ImportingModule)
        {
            this.Attribute = Attribute;
            this.importingModule = ImportingModule;
        }

        public CustomAttribute Attribute { get; private set; }
        private CecilModule importingModule;

        public IType AttributeType
        {
            get
            {
                return importingModule.Convert(Attribute.AttributeType);
            }
        }

        public IBoundObject Value
        {
            get
            {
                var cecilAttrType = Attribute.AttributeType.Resolve();
                var clrType = Type.GetType(cecilAttrType.FullName + ", " + cecilAttrType.Module.Assembly.FullName);
                if (clrType == null)
                {
                    return null; // No luck today, apparently
                }
                else
                {
                    object instance = Activator.CreateInstance(clrType, Attribute.ConstructorArguments.Select((item) => item.Value).ToArray());
                    foreach (var item in Attribute.Fields)
                    {
                        var field = clrType.GetField(item.Name);
                        field.SetValue(clrType, item.Argument);
                    }
                    foreach (var item in Attribute.Properties)
                    {
                        var property = clrType.GetProperty(item.Name);
                        property.SetValue(clrType, item.Argument);
                    }
                    return new CecilBoundObject(instance, importingModule.ConvertStrict(clrType), importingModule);
                }
            }
        }

        #region Static

        public static AttributeMap GetAttributes(IList<CustomAttribute> CustomAttributes, ICecilMember ImportingMember)
        {
            return GetAttributes(CustomAttributes, ImportingMember.Module);
        }
        public static AttributeMap GetAttributes(IList<CustomAttribute> CustomAttributes, CecilModule ImportingModule)
        {
            var attrs = new AttributeMapBuilder();
            foreach (var item in CustomAttributes)
            {
                var cecilAttribute = new CecilAttribute(item, ImportingModule);
                IAttribute attribute;
                switch (cecilAttribute.AttributeType.FullName.ToString())
                {
                    case "System.Runtime.CompilerServices.ExtensionAttribute":
                        attribute = PrimitiveAttributes.Instance.ExtensionAttribute;
                        break;
                    case "System.ParamArrayAttribute":
                        attribute = PrimitiveAttributes.Instance.VarArgsAttribute;
                        break;
                    case "System.Diagnostics.Contracts.PureAttribute":
                        var args = item.ConstructorArguments;
                        if (args.Count > 0 && !(bool)item.ConstructorArguments[0].Value)
                        {
                            goto default;
                        }
                        else
                        {
                            attribute = PrimitiveAttributes.Instance.ConstantAttribute;
                        }
                        break;
                    case "Flame.RT.IncludeAttribute":
                        attribute = PrimitiveAttributes.Instance.RecompileAttribute;
                        break;
                    default:
                        attribute = cecilAttribute;
                        break;
                }
                attrs.Add(attribute);
            }
            return new AttributeMap(attrs);
        }
        public static Lazy<AttributeMap> GetAttributesLazy(IList<CustomAttribute> CustomAttributes, CecilModule ImportingModule)
        {
            return new Lazy<AttributeMap>(() => GetAttributes(CustomAttributes, ImportingModule));
        }

        public IMethod Constructor
        {
            get { return importingModule.Convert(Attribute.Constructor); }
        }

        public IEnumerable<IBoundObject> GetArguments()
        {
            return Attribute.ConstructorArguments.Select(item => ExpressionExtensions.ToExpression(item.Value).Evaluate());
        }

        public static void DeclareAttributes(Mono.Cecil.ICustomAttributeProvider AttributeProvider, ICecilMember Member, IEnumerable<IAttribute> Templates)
        {
            foreach (var item in Templates)
            {
                DeclareAttributeOrDefault(AttributeProvider, Member, item);
            }
        }

        public static CecilAttribute CreateCecil<TAttribute>(ICecilMember ImportingMember)
            where TAttribute : new()
        {
            var type = CecilTypeBase.ImportCecil<TAttribute>(ImportingMember);
            return CreateCecil(type.GetConstructor(new IType[0]), new IBoundObject[0], ImportingMember);
        }

        public static CecilAttribute CreateCecil(
            IMethod Constructor, IEnumerable<IBoundObject> Arguments, ICecilMember ImportingMember)
        {
            var methodImporter = new CecilMethodImporter(ImportingMember.Module);
            var ctor = methodImporter.Convert(Constructor);
            var attrDef = new CustomAttribute(ctor);
            foreach (var item in Arguments)
            {
                attrDef.ConstructorArguments.Add(
                    ToCustomAttributeArgument(item, methodImporter.TypeImporter));
            }
            return new CecilAttribute(attrDef, ImportingMember);
        }

        private static CustomAttributeArgument ToCustomAttributeArgument(
            IBoundObject BoundObject, IConverter<IType, TypeReference> TypeImporter)
        {
            var type = BoundObject.Type;
            return new CustomAttributeArgument(
                TypeImporter.Convert(type), 
                ToCustomAttributeArgumentValue(type, BoundObject, TypeImporter));
        }

        private static object ToCustomAttributeArgumentValue(
            IType ObjectType, IBoundObject BoundObject, 
            IConverter<IType, TypeReference> TypeImporter)
        {
            if (ObjectType.GetIsInteger())
            {
                IntegerValue intVal = BoundObject.GetValue<IntegerValue>();
                var spec = intVal.Spec;
                if (spec.Equals(IntegerSpec.Int8))
                    return intVal.ToInt8();
                else if (spec.Equals(IntegerSpec.Int16))
                    return intVal.ToInt16();
                else if (spec.Equals(IntegerSpec.Int32))
                    return intVal.ToInt32();
                else if (spec.Equals(IntegerSpec.Int64))
                    return intVal.ToInt64();
                else if (spec.Equals(IntegerSpec.UInt8))
                    return intVal.ToUInt8();
                else if (spec.Equals(IntegerSpec.UInt16))
                    return intVal.ToUInt16();
                else if (spec.Equals(IntegerSpec.UInt32))
                    return intVal.ToUInt32();
                else if (spec.Equals(IntegerSpec.UInt64))
                    return intVal.ToUInt64();
                else
                    throw new NotSupportedException("Unsupported integer spec: " + spec.ToString());
            }
            else if (ObjectType.GetIsBit())
            {
                BitValue bitVal = BoundObject.GetValue<BitValue>();
                switch (bitVal.Size)
                {
                    case 8:
                        return bitVal.ToInteger().ToUInt8();
                    case 16:
                        return bitVal.ToInteger().ToUInt16();
                    case 32:
                        return bitVal.ToInteger().ToUInt32();
                    case 64:
                        return bitVal.ToInteger().ToUInt64();
                    default:
                        throw new NotSupportedException("Unsupported bit size: " + bitVal.Size);
                }
            }
            else if (PrimitiveTypes.Boolean.Equals(ObjectType))
            {
                return BoundObject.GetValue<bool>();
            }
            else if (PrimitiveTypes.Float32.Equals(ObjectType))
            {
                return BoundObject.GetValue<float>();
            }
            else if (PrimitiveTypes.Float64.Equals(ObjectType))
            {
                return BoundObject.GetValue<double>();
            }
            else
            {
                return BoundObject.GetValue<object>();
            }
        }

        private static readonly Dictionary<IType, Lazy<ConstructorInfo>> intrinsicAttrCtors = new Dictionary<IType, Lazy<ConstructorInfo>>()
        {
            { 
                PrimitiveAttributes.Instance.ConstantAttribute.AttributeType, 
                new Lazy<ConstructorInfo>(() => typeof(System.Diagnostics.Contracts.PureAttribute).GetConstructor(new Type[0])) 
            },
            { 
                PrimitiveAttributes.Instance.ExtensionAttribute.AttributeType, 
                new Lazy<ConstructorInfo>(() => typeof(System.Runtime.CompilerServices.ExtensionAttribute).GetConstructor(new Type[0])) 
            },
            { 
                PrimitiveAttributes.Instance.VarArgsAttribute.AttributeType, 
                new Lazy<ConstructorInfo>(() => typeof(System.ParamArrayAttribute).GetConstructor(new Type[0])) 
            },
            { 
                PrimitiveAttributes.Instance.RecompileAttribute.AttributeType, 
                new Lazy<ConstructorInfo>(() => typeof(Flame.RT.IncludeAttribute).GetConstructor(new Type[0])) 
            }
        };

        public static CecilAttribute ImportCecil(IAttribute Template, ICecilMember ImportingMember)
        {
            Lazy<ConstructorInfo> ctorInfo;
            if (Template is IConstructedAttribute)
            {
                var constructedAttr = (IConstructedAttribute)Template;
                return CreateCecil(constructedAttr.Constructor, constructedAttr.GetArguments(), ImportingMember);
            }
            else if (intrinsicAttrCtors.TryGetValue(Template.AttributeType, out ctorInfo))
            {
                return new CecilAttribute(new CustomAttribute(
                    ((ICecilMethod)CecilMethodBase.ImportCecil(ctorInfo.Value, ImportingMember)).GetMethodReference()), 
                    ImportingMember);
            }
            else
            {
                return null;
            }
        }

        public static CecilAttribute DeclareAttributeOrDefault(Mono.Cecil.ICustomAttributeProvider AttributeProvider, ICecilMember Member, IAttribute Template)
        {
            if (Template is IConstructedAttribute)
            {
                var constructedAttr = (IConstructedAttribute)Template;
                var attr = CreateCecil(constructedAttr.Constructor, constructedAttr.GetArguments(), Member);
                AttributeProvider.CustomAttributes.Add(attr.Attribute);
                return attr;
            }

            CustomAttribute attrDef;
            if (Template.AttributeType.Equals(PrimitiveAttributes.Instance.ConstantAttribute.AttributeType))
            {
                attrDef = new CustomAttribute(((ICecilMethod)CecilMethodBase.ImportCecil(typeof(System.Diagnostics.Contracts.PureAttribute).GetConstructor(new Type[0]), Member)).GetMethodReference());
            }
            else if (Template.AttributeType.Equals(PrimitiveAttributes.Instance.ExtensionAttribute.AttributeType))
            {
                attrDef = new CustomAttribute(((ICecilMethod)CecilMethodBase.ImportCecil(typeof(System.Runtime.CompilerServices.ExtensionAttribute).GetConstructor(new Type[0]), Member)).GetMethodReference());
            }
            else
            {
                return null;
            }
            AttributeProvider.CustomAttributes.Add(attrDef);
            return new CecilAttribute(attrDef, Member);
        }

        #endregion
    }
}
