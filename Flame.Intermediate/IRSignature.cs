﻿using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public class IRSignature
    {
        public IRSignature(string Name, IEnumerable<INodeStructure<IAttribute>> AttributeNodes)
        {
            this.Name = Name;
            this.AttributeNodes = AttributeNodes;
            this.cachedAttrs = new Lazy<IAttribute[]>(() => AttributeNodes.Select(item => item.Value).ToArray());
        }
        public IRSignature(string Name)
        {
            this.Name = Name;
            this.AttributeNodes = Enumerable.Empty<INodeStructure<IAttribute>>();
            this.cachedAttrs = new Lazy<IAttribute[]>(() => new IAttribute[0]);
        }

        public string Name { get; private set; }
        public IEnumerable<INodeStructure<IAttribute>> AttributeNodes { get; private set; }

        public const string MemberNodeName = "#member";

        public LNode Node
        {
            get
            {
                var args = new List<LNode>();
                args.Add(NodeFactory.IdOrLiteral(Name));
                args.AddRange(AttributeNodes.Select(item => item.Node));
                return NodeFactory.Call(MemberNodeName, args);
            }
        }

        private Lazy<IAttribute[]> cachedAttrs;
        public IEnumerable<IAttribute> Attributes
        {
            get { return cachedAttrs.Value; }
        }

        public static readonly IRSignature Empty = new IRSignature("", Enumerable.Empty<INodeStructure<IAttribute>>());
    }
}