using PascalNET.Core.AST.BasicNodes;
using PascalNET.Core.AST.Nodes;

namespace PascalNET.Core.AST.Declartions
{
    internal class Parameter
    {
        public string Name { get; set; }

        public string TypeName { get; set; }

        public Parameter(string name, string typeName)
        {
            Name = name;
            TypeName = typeName;
        }
    }

    internal class FunctionDeclaration : IDeclaration
    {
        public string Name { get; set; }

        public List<Parameter> Parameters { get; set; }

        public string? ReturnType { get; set; }

        public List<IDeclaration> LocalDeclarations { get; set; }

        public IStatement Body { get; set; }

        public bool IsProcedure => ReturnType == null;

        public FunctionDeclaration(string name, List<Parameter> parameters, string? returnType, List<IDeclaration> localDeclarations, IStatement body)
        {
            Name = name;
            Parameters = parameters;
            ReturnType = returnType;
            LocalDeclarations = localDeclarations;
            Body = body;
        }
    }
}
