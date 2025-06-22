using PascalNET.Core.AST.Nodes;

namespace PascalNET.Core.AST.Declartions
{
    internal class VariableDeclaration : IDeclaration
    {
        public List<string> Identifiers { get; set; }

        public string TypeName { get; set; }

        public VariableDeclaration(List<string> identifiers, string typeName)
        {
            Identifiers = identifiers;
            TypeName = typeName;
        }
    }
}
