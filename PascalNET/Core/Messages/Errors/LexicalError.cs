using PascalNET.Core.Messages;

namespace PascalNET.Core.Messages.Errors
{
    public class LexicalError : CompilerError
    {
        public LexicalError(string message, int line, int column, string sourceFragment = "", string suggestion = "")
            : base($"Лексическая ошибка в строке {line}, столбец {column}: {message}", line, column, sourceFragment, suggestion)
        {
        }
    }
}