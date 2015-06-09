﻿using Flame.Compiler;
using Flame.Compiler.Visitors;
using Flame.Optimization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Target
{
    public class InliningPass : IPass<BodyPassArgument, IStatement>
    {
        private InliningPass()
        {

        }

        static InliningPass()
        {
            Instance = new InliningPass();
        }

        public static InliningPass Instance { get; private set; }

        private static int ApproximateSize(IType Type)
        {
            int primSize = Type.GetPrimitiveSize();
            if (primSize > 0)
            {
                return primSize;
            }

            if (Type.get_IsReferenceType() || Type.get_IsPointer() || Type.get_IsArray())
            {
                return 4;
            }

            if (Type.get_IsVector())
            {
                return ApproximateSize(Type.GetEnumerableElementType()) * Type.AsContainerType().AsVectorType().GetDimensions().Aggregate(1, (aggr, val) => aggr * val);
            }

            return Type.GetFields().Aggregate(0, (aggr, field) => aggr + ApproximateSize(field.FieldType));
        }

        private static int RateArgument(IType ParameterType, IExpression Argument)
        {
            var argType = Argument.Type;

            int inheritanceBoost = !argType.Equals(ParameterType) ? 4 : 0;  // This is interesting, because it may allow us to
                                                                            // replace indirect calls with direct calls

            int constantBoost = Argument.IsConstant ? 4 : 0;                // Constants may allow us to eliminate branches

            int delegateBoost = ParameterType.get_IsDelegate() ? 4 : 0;     // Delegates can be sometimes be replaced with direct or indirect calls.

            return ApproximateSize(argType) + inheritanceBoost + constantBoost + delegateBoost;
        }

        public bool ShouldInline(BodyPassArgument Args, DissectedCall Call, int Tolerance)
        {
            var body = Args.PassEnvironment.GetMethodBody(Call.Method);
            if (body == null)
            {
                return false;
            }

            int pro = Call.ThisValue != null ? RateArgument(Call.Method.DeclaringType, Call.ThisValue) : 0;
            foreach (var item in Call.Method.GetParameters().Zip(Call.Arguments, (first, second) => Tuple.Create(first, second)))
            {
                pro += RateArgument(item.Item1.ParameterType, item.Item2);
            }

            int con = SizeVisitor.Static_Singleton.Instance.ApproximateSize(body, true, 2);

            return con - pro < Tolerance;
        }

        public IStatement Apply(BodyPassArgument Value)
        {
            int maxRecursion = Value.PassEnvironment.Log.Options.GetOption<int>("max-inline-recursion", 3);
            int inlineTolerance = Value.PassEnvironment.Log.Options.GetOption<int>("inline-tolerance", 0);

            var inliner = new InliningVisitor(Value.Method, call => ShouldInline(Value, call, inlineTolerance), 
                                              Value.PassEnvironment.GetMethodBody, stmt => stmt.Optimize(), maxRecursion);
            var result = inliner.Visit(Value.Body);
            if (inliner.HasInlined)
            {
                result = result.Optimize();
            }
            return result;
        }
    }
}