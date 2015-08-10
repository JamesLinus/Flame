﻿using Flame.Compiler;
using Flame.DSharp.Lexer;
using Flame.DSharp.Parser;
using Flame.Syntax;
using Flame.Syntax.DSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PassTests
{
    [TestClass]
    public class ParserTests
    {
        public static IStatementSyntax ParseStatement(string Code)
        {
            var lexer = new TokenizerStream(new SourceDocument(Code, "test"));
            var log = new TestLog(EmptyCompilerOptions.Instance);
            var parser = new DSharpSyntaxParser(log);
            return parser.ParseStatement(lexer);
        }

        public static IExpressionSyntax ParseExpression(string Code)
        {
            var lexer = new TokenizerStream(new SourceDocument(Code, "test"));
            var log = new TestLog(EmptyCompilerOptions.Instance);
            var parser = new DSharpSyntaxParser(log);
            return parser.ParseExpression(lexer);
        }

        public static ITypeSyntax ParseType(string Code)
        {
            var lexer = new TokenizerStream(new SourceDocument(Code, "test"));
            var log = new TestLog(EmptyCompilerOptions.Instance);
            var parser = new DSharpSyntaxParser(log);
            return parser.ParseType(lexer);
        }

        [TestMethod]
        [TestCategory("D# - Parser")]
        public void PeekDelegate()
        {
            var lexer = new TokenizerStream(new SourceDocument("x(y, z) i ", "test"));
            var log = new TestLog(EmptyCompilerOptions.Instance);
            var parser = new DSharpSyntaxParser(log);
            var pos = lexer.CurrentPosition;
            var result = parser.PeekEntireType(lexer, true, ref pos);
            Assert.IsTrue(result is DelegateTypeSyntax);
            var peek = lexer.PeekNoTrivia(pos);
            Assert.IsTrue(peek.Type == TokenType.Identifier);
            lexer.Seek(pos);
            var next = lexer.NextNoTrivia();
            Assert.IsTrue(next.Type == TokenType.Identifier);
        }

        [TestMethod]
        [TestCategory("D# - Parser")]
        public void ParseDelegate()
        {
            var type = ParseType("x(y, z<w>)");
            Assert.IsTrue(type is DelegateTypeSyntax);
        }

        [TestMethod]
        [TestCategory("D# - Parser")]
        public void ParseDelegateDeclExpr()
        {
            var expr = ParseExpression("x(y u, z<w> v) i ");
            Assert.IsTrue(expr is InlineVariableDeclarationSyntax);
        }

        [TestMethod]
        [TestCategory("D# - Parser")]
        public void ParseInvocationExpr()
        {
            var expr = ParseExpression("x(y, z)");
            Assert.IsTrue(expr is InvocationSyntax);
        }

        [TestMethod]
        [TestCategory("D# - Parser")]
        public void ParseLambda()
        {
            var expr = ParseExpression("bool(int x, int y) => x == y");
            Assert.IsTrue(expr is LambdaSyntax);
        }
    }
}
