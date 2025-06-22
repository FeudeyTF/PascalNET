using PascalNET.Core.AST.BasicNodes;

namespace PascalNET.Core.AST.Statements
{
    internal class CompoundStatement : IStatement
    {
        public List<IStatement> Statements { get; set; }

        public CompoundStatement(List<IStatement> statements)
        {
            Statements = statements;
        }
    }
}
