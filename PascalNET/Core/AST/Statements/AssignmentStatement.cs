using PascalNET.Core.AST.BasicNodes;

namespace PascalNET.Core.AST.Statements
{
    internal class AssignmentStatement : IStatement
    {
        public string Variable { get; set; }

        public IExpression Expression { get; set; }

        public AssignmentStatement(string variable, IExpression expression)
        {
            Variable = variable;
            Expression = expression;
        }
    }
}
