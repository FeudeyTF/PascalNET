namespace PascalNET.Core.Messages.Errors
{
    internal class TypeError : CompilerError
    {
        public TypeError(string message, int line, int column, string sourceFragment = "", string suggestion = "")
            : base(message, line, column, sourceFragment, suggestion)
        {
        }
    }
}
