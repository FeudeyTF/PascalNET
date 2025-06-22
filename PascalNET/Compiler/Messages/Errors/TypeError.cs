using PascalNET.Errors;

namespace PascalNET.Compiler.Messages.Errors
{
    internal class TypeError : CompilerError
    {
        public TypeError(string message, int line, int column, string sourceFragment = "", string suggestion = "")
            : base($"Ошибка типов в строке {line}, столбец {column}: {message}", line, column, sourceFragment, suggestion)
        {
        }
    }
}
