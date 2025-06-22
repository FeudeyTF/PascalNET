using PascalNET.Core.AST.BasicNodes;
using PascalNET.Core.AST.Nodes;

namespace PascalNET.Core.AST
{
    internal class ExecutionNode : INode
    {
        public List<IDeclaration> Declarations { get; set; }

        public IStatement Statement { get; set; }

        public ExecutionNode(List<IDeclaration> declarations, IStatement statement)
        {
            Declarations = declarations;
            Statement = statement;
        }
    }
}
