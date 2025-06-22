using PascalNET.Core.AST.BasicNodes;

namespace PascalNET.Core.AST.Statements
{
    internal class CycleStatement : IStatement
    {
        public IExpression Condition { get; set; }
        public IStatement Statement { get; set; }

        public CycleStatement(IExpression condition, IStatement statement)
        {
            Condition = condition;
            Statement = statement;
        }
    }

}
