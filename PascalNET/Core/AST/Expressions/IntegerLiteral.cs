using PascalNET.Core.AST.BasicNodes;

namespace PascalNET.Core.AST.Expressions
{
    internal class IntegerLiteral : IExpression
    {
        public int Value { get; set; }

        public IntegerLiteral(int value)
        {
            Value = value;
        }
    }
}
