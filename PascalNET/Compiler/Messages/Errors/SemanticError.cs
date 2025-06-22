using PascalNET.Errors;

namespace PascalNET.Compiler.Messages.Errors
{
    internal class SemanticError : CompilerError
    {
        public SemanticError(string message, int line, int column, string sourceFragment = "", string suggestion = "")
            : base($"Семантическая ошибка в строке {line}, столбец {column}: {message}", line, column, sourceFragment, suggestion)
        {
        }

        public override ConsoleColor GetColor()
        {
            return ConsoleColor.DarkRed;
        }
    }
}
