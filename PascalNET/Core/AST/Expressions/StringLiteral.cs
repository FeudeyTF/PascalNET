using PascalNET.Core.AST.BasicNodes;

namespace PascalNET.Core.AST.Expressions
{
    internal class StringLiteral : IExpression
    {
        public string Value { get; set; }

        public StringLiteral(string value)
        {
            Value = value;
        }
    }
}
