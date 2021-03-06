﻿using Flame.Compiler;
using Flame.Compiler.Build;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public interface ICecilTypeBuilder : ICecilType, ITypeBuilder, INamespaceBuilder, ICecilNamespace
    {
        void AddField(FieldDefinition Field);
        void AddMethod(MethodDefinition Method);
        void AddProperty(PropertyDefinition Property);
        void AddEvent(EventDefinition Event);
    }
}
