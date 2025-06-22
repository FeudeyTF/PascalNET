namespace PascalNET.Core.Messages.Errors
{
    internal class SemanticError : CompilerError
    {
        public SemanticError(string message, int line, int column, string sourceFragment = "", string suggestion = "")
            : base(message, line, column, sourceFragment, suggestion)
        {
        }
    }
}
