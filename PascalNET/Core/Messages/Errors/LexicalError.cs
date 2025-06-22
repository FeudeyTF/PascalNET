using PascalNET.Core.Messages;

namespace PascalNET.Core.Messages.Errors
{
    public class LexicalError : CompilerError
    {
        public LexicalError(string message, int line, int column, string sourceFragment = "", string suggestion = "")
            : base(message, line, column, sourceFragment, suggestion)
        {
        }
    }
}