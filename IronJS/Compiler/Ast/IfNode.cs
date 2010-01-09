﻿using System;
using System.Text;

namespace IronJS.Compiler.Ast
{
    using Et = System.Linq.Expressions.Expression;

    class IfNode : Node
    {
        public readonly Node Test;
        public readonly Node TrueBranch;
        public readonly Node ElseBranch;
        public readonly bool HasElseBranch;

        public IfNode(Node test, Node trueBranch, Node elseBranch)
            : base(NodeType.If)
        {
            Test = test;
            TrueBranch = trueBranch;
            ElseBranch = elseBranch;
            HasElseBranch = elseBranch != null;
        }

        public override void Print(StringBuilder writer, int indent = 0)
        {
            var indentStr = new String(' ', indent * 2);

            writer.AppendLine(indentStr + "(" + Type);

            TrueBranch.Print(writer, indent + 1);
            ElseBranch.Print(writer, indent + 1);

            writer.AppendLine(indentStr + ")");
        }

        public override Et Walk(EtGenerator etgen)
        {
            throw new NotImplementedException();
        }
    }
}