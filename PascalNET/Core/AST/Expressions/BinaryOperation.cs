using PascalNET.Core.AST.BasicNodes;

namespace PascalNET.Core.AST.Expressions
{
    internal class BinaryOperation : IExpression
    {
        public IExpression Left { get; set; }

        public string Operator { get; set; }

        public IExpression Right { get; set; }

        public BinaryOperation(IExpression left, string op, IExpression right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }
    }
}
