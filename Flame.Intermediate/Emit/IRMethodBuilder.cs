﻿using Flame.Compiler;
using Flame.Compiler.Build;
using Flame.Intermediate.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Emit
{
    public class IRMethodBuilder : IRMethod, IMethodBuilder
    {
        public IRMethodBuilder(IRAssemblyBuilder Assembly, IType DeclaringType, IMethodSignatureTemplate Template)
            : base(DeclaringType, new IRSignature(Template.Name), Template.IsStatic, Template.IsConstructor)
        {
            this.Assembly = Assembly;
            this.Template = new MethodSignatureInstance(Template, this);
            this.codeGen = new IRCodeGenerator(Assembly, this);
        }

        private IRCodeGenerator codeGen;

        public IRAssemblyBuilder Assembly { get; private set; }
        public MethodSignatureInstance Template { get; private set; }

        public ICodeGenerator GetBodyGenerator()
        {
            return codeGen;
        }

        public void SetMethodBody(ICodeBlock Body)
        {
            var processedNode = NodeBlock.ToNode(codeGen.Postprocess(Body));
            this.BodyNode = new LazyNodeStructure<IStatement>(processedNode, () => { throw new InvalidOperationException("IR method builders cannot be decompiled."); });
        }

        public IMethod Build()
        {
            return this;
        }

        public void Initialize()
        {
            this.Signature = IREmitHelpers.CreateSignature(Assembly, Template.Name, Template.Attributes.Value);
            this.GenericParameterNodes = 
                IREmitHelpers.ConvertGenericParameters(Assembly, this, Template.GenericParameters.Value);
            var visitor = new IRGenericMemberTypeVisitor(Assembly, this);
            this.ParameterNodes = IREmitHelpers.ConvertParameters(Assembly, visitor.GetTypeReference, Template.Parameters.Value);
            this.ReturnTypeNode = new ConstantNodeStructure<IType>(visitor.GetTypeReference(Template.ReturnType.Value), Template.ReturnType.Value);
            this.BaseMethodNodes = new NodeList<IMethod>(
                Template.BaseMethods.Value.Select(Assembly.MethodTable.GetReferenceStructure).ToArray());
        }
    }
}
