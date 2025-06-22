namespace PascalNET.Core.Messages.Errors
{
    internal class ScopeError : CompilerError
    {
        public ScopeError(string message, int line, int column, string sourceFragment = "", string suggestion = "")
            : base(message, line, column, sourceFragment, suggestion)
        {
        }
    }
}
