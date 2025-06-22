using PascalNET.Core.AST.Nodes;
using PascalNET.Core.AST.BasicNodes;

namespace PascalNET.Core.AST.Expressions
{
    /// <summary>
    /// AST узел для вызова функции или процедуры
    /// </summary>
    internal class FunctionCall : IExpression
    {
        public string Name { get; set; }
        public List<IExpression> Arguments { get; set; }

        public FunctionCall(string name, List<IExpression> arguments)
        {
            Name = name;
            Arguments = arguments;
        }
    }
}
