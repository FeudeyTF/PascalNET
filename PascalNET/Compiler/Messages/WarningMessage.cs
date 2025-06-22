using PascalNET.Errors;

namespace PascalNET.Compiler.Messages
{
    internal class WarningMessage : CompilerMessage
    {
        public WarningMessage(string message, int line, int column, string sourceFragment = "", string suggestion = "")
            : base($"Предупреждение в строке {line}, столбец {column}: {message}", line, column, sourceFragment, suggestion)
        {
        }

        public override ConsoleColor GetColor()
        {
            return ConsoleColor.Yellow;
        }
    }
}
