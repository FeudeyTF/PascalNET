using PascalNET.Core.AST.BasicNodes;

namespace PascalNET.Core.AST.Expressions
{
    internal class RealLiteral : IExpression
    {
        public double Value { get; set; }

        public RealLiteral(double value)
        {
            Value = value;
        }
    }
}
