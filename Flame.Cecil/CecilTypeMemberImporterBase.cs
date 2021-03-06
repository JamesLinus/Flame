﻿using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public abstract class CecilTypeMemberImporterBase<TMember, TReference> : IConverter<TMember, TReference>
        where TMember : ITypeMember
    {
        public CecilTypeMemberImporterBase(CecilModule Module, IGenericParameterProvider Context, IConverter<IType, TypeReference> TypeImporter)
        {
            this.Module = Module;
            this.TypeImporter = TypeImporter;
            this.Context = Context;
        }
        public CecilTypeMemberImporterBase(CecilModule Module, IGenericParameterProvider Context)
            : this(Module, Context, new CecilTypeImporter(Module, Context))
        {
        }
        public CecilTypeMemberImporterBase(CecilModule Module)
            : this(Module, null)
        {
        }

        public CecilModule Module { get; private set; }
        public IGenericParameterProvider Context { get; private set; }
        public IConverter<IType, TypeReference> TypeImporter { get; private set; }

        protected TypeReference ConvertType(IType Type)
        {
            return TypeImporter.Convert(Type);
        }

        protected abstract TReference ConvertDeclaration(TMember Member);
        protected abstract TReference ConvertInstanceGeneric(TypeReference DeclaringType, TMember Member);
        protected virtual TReference ConvertPrimitive(TMember Member)
        {
            return ConvertDeclaration(Member);
        }

        public virtual TReference Convert(TMember Value)
        {
            if (Value.DeclaringType.GetAllGenericArguments().Any())
            {
                return ConvertInstanceGeneric(ConvertType(Value.DeclaringType), Value);
            }
            else if (Value.DeclaringType.GetIsPrimitive())
            {
                return ConvertPrimitive(Value);
            }
            else
            {
                return ConvertDeclaration(Value);
            }
        }

        public IEnumerable<TReference> Convert(IEnumerable<TMember> Values)
        {
            return Values.Select(Convert);
        }
    }
}
