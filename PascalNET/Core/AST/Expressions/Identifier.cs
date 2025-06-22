using PascalNET.Core.AST.BasicNodes;

namespace PascalNET.Core.AST.Expressions
{
    internal class Identifier : IExpression
    {
        public string Name { get; set; }

        public Identifier(string name)
        {
            Name = name;
        }
    }
}
