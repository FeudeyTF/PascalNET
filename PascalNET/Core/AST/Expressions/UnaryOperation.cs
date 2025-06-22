using PascalNET.Core.AST.BasicNodes;

namespace PascalNET.Core.AST.Expressions
{
    internal class UnaryOperation : IExpression
    {
        public string Operator { get; set; }

        public IExpression Operand { get; set; }

        public UnaryOperation(string op, IExpression operand)
        {
            Operator = op;
            Operand = operand;
        }
    }
}
