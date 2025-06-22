using PascalNET.Errors;

namespace PascalNET.Compiler.Messages.Errors
{
    internal class ScopeError : CompilerError
    {
        public ScopeError(string message, int line, int column, string sourceFragment = "", string suggestion = "")
            : base($"Ошибка области видимости в строке {line}, столбец {column}: {message}", line, column, sourceFragment, suggestion)
        {
        }
    }
}
