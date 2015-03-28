﻿using Flame.Compiler;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class InvocationBlock : ICecilBlock
    {
        public InvocationBlock(ICecilBlock Method, IEnumerable<ICecilBlock> Arguments)
        {
            this.Method = Method;
            this.Arguments = Arguments;
        }

        public ICecilBlock Method { get; private set; }
        public IEnumerable<ICecilBlock> Arguments { get; private set; }

        public void Emit(IEmitContext Context)
        {
            if (Method is MethodBlock)
            {
                var log = CodeGenerator.Method.GetLog();
                var mBlock = (MethodBlock)Method;
                var method = mBlock.Method;
                bool isCtorCall = mBlock.Caller == null && method.IsConstructor;
                IType callerType = null;
                if (!method.IsStatic && !isCtorCall)
                {
                    if (mBlock.Caller == null)
                    {
                        System.Diagnostics.Debugger.Break();
                    }
                    MethodBlock.EmitCaller(mBlock.Caller, method, Context);
                    callerType = Context.Stack.Pop();
                    if (!ILCodeGenerator.IsExpectedCallingType(method, callerType))
                    {
                        log.LogError(new LogEntry("IL emit error", "Invalid calling type on stack. Expected '" + ILCodeGenerator.GetExpectedCallingType(method).FullName + "', got '" + callerType.FullName + "'"));
                    }
                }
                ILCodeGenerator.EmitArguments(Arguments, method, Context);
                if (isCtorCall)
                {
                    Context.Emit(OpCodes.Newobj, method);
                    Context.Stack.Push(method.DeclaringType);
                    return;
                }
                else if ((method.DeclaringType.get_IsArray() || method.DeclaringType.get_IsVector()) && method is IAccessor && (((IAccessor)method).DeclaringProperty).Name == "Length" && ((IAccessor)method).AccessorType.Equals(AccessorType.GetAccessor))
                {
                    Context.Emit(OpCodes.Ldlen);
                }
                else if (log.Options.UseInvariantCulture() && ILCodeGenerator.IsCultureSpecific(method))
                    // Fix culture-specific calls if necessary
                {
                    ILCodeGenerator.EmitCultureInvariantCall(Context, method, callerType, CodeGenerator.GetModule());
                }
                else
                {
                    ILCodeGenerator.EmitCall(Context, method, callerType);
                }
                Context.Stack.Push(method.ReturnType);
            }
            else
            {
                // Call function pointer
                Method.Emit(Context);
                var method = (IMethod)Context.Stack.Pop();
                var cecilMethod = (ICecilMethod)CodeGenerator.Method;
                var methodRef = cecilMethod.GetMethodReference();
                var module = CodeGenerator.GetModule();
                var callSite = new CallSite(method.ReturnType.GetImportedReference(module, methodRef));
                foreach (var item in method.GetParameters())
                {
                    var paramDef = new ParameterDefinition(item.ParameterType.GetImportedReference(module, methodRef));
                    callSite.Parameters.Add(paramDef);
                }
                ILCodeGenerator.EmitArguments(Arguments, method, Context);
                Context.Emit(OpCodes.Calli, callSite);
                Context.Stack.Push(method.ReturnType);
            }
        }

        public IStackBehavior StackBehavior
        {
            get 
            {
                return new InvocationStackBehavior(Method.StackBehavior, Arguments.Select((item) => item.StackBehavior));
            }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Method.CodeGenerator; }
        }
    }
}
