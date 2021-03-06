﻿using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Parsing
{
    /// <summary>
    /// A class that parses references to values, which may be resolved lazily.
    /// </summary>
    public struct ReferenceParser<T>
    {
        private ReferenceParser(ValueParser<INodeStructure<T>> innerParser)
        {
            this.innerParser = innerParser;
        }
        public ReferenceParser(IReadOnlyDictionary<string, Func<ParserState, LNode, INodeStructure<T>>> Parsers)
        {
            this.innerParser = new ValueParser<INodeStructure<T>>(Parsers);
        }

        private ValueParser<INodeStructure<T>> innerParser;

        /// <summary>
        /// Gets a dictionary of node tags to parsers.
        /// </summary>
        /// <value>The parser dictionary.</value>
        public IReadOnlyDictionary<string, Func<ParserState, LNode, INodeStructure<T>>> Parsers
        {
            get { return innerParser.Parsers; }
        }

        public ReferenceParser<T> WithParser(
            string Name, Func<ParserState, LNode, INodeStructure<T>> Parser)
        {
            return new ReferenceParser<T>(innerParser.WithParser(Name, Parser));
        }

        public ReferenceParser<T> WithParsers(
            IEnumerable<KeyValuePair<string, Func<ParserState, LNode, INodeStructure<T>>>> Parsers)
        {
            return new ReferenceParser<T>(innerParser.WithParsers(Parsers));
        }

        /// <summary>
        /// Figures out if the given node has a known node type,
        /// and can therefore be parsed.
        /// </summary>
        /// <param name="Node"></param>
        /// <returns></returns>
        public bool CanParse(LNode Node)
        {
            return innerParser.CanParse(Node);
        }

        /// <summary>
        /// Parse the specified node according to the given state.
        /// </summary>
        /// <param name="State">The state to parse the node with.</param>
        /// <param name="Node">The node to parse.</param>
        public INodeStructure<T> Parse(ParserState State, LNode Node)
        {
            Func<ParserState, LNode, INodeStructure<T>> parser;
            if (Parsers.TryGetValue(Node.Name.Name, out parser))
            {
                return parser(State, Node);
            }
            else if (Node.IsLiteral && Node.Value == null)
            {
                return new ConstantNodeStructure<T>(Node, default(T));
            }
            else
            {
                throw new InvalidOperationException(
                    "Could not handle the given '" + 
                    (Node.IsLiteral ? Node.Print() : Node.Name.Name) + 
                    "' node because its type was unknown.");
            }
        }
    }
}
