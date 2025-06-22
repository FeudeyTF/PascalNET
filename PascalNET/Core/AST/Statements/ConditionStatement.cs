using PascalNET.Core.AST.BasicNodes;

namespace PascalNET.Core.AST.Statements
{
    internal class ConditionStatement : IStatement
    {
        public IExpression Condition { get; set; }

        public IStatement ThenStatement { get; set; }

        public IStatement ElseStatement { get; set; }


        public ConditionStatement(IExpression condition, IStatement thenStatement, IStatement elseStatement = null)
        {
            Condition = condition;
            ThenStatement = thenStatement;
            ElseStatement = elseStatement;
        }
    }
}
