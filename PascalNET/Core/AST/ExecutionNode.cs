using PascalNET.Core.AST.BasicNodes;
using PascalNET.Core.AST.Declartions;
using PascalNET.Core.AST.Nodes;

namespace PascalNET.Core.AST
{
    internal class ExecutionNode : INode
    {
        public ProgramDeclaration? ProgramName { get; set; }

        public List<IDeclaration> Declarations { get; set; }

        public IStatement Statement { get; set; }

        public ExecutionNode(List<IDeclaration> declarations, IStatement statement, ProgramDeclaration? programName = null)
        {
            ProgramName = programName;
            Declarations = declarations;
            Statement = statement;
        }
    }
}
