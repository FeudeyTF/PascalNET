using PascalNET.Core.AST.Nodes;

namespace PascalNET.Core.AST.Declartions
{
    /// <summary>
    /// AST узел для объявления программы (Program name)
    /// </summary>
    internal class ProgramDeclaration : IDeclaration
    {
        public string Name { get; set; }

        public ProgramDeclaration(string name)
        {
            Name = name;
        }
    }
}
