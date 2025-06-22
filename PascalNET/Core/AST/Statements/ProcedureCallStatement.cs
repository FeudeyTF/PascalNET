using PascalNET.Core.AST.BasicNodes;
using PascalNET.Core.AST.Expressions;

namespace PascalNET.Core.AST.Statements
{
    /// <summary>
    /// AST узел для вызова процедуры
    /// </summary>
    internal class ProcedureCallStatement : IStatement
    {
        public string Name { get; set; }

        public List<IExpression> Arguments { get; set; }

        public ProcedureCallStatement(string name, List<IExpression> arguments)
        {
            Name = name;
            Arguments = arguments;
        }
    }
}
