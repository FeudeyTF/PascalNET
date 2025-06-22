namespace PascalNET.Core.Messages
{
    internal class WarningMessage : CompilerMessage
    {
        public WarningMessage(string message, int line, int column, string sourceFragment = "", string suggestion = "")
            : base(message, line, column, sourceFragment, suggestion)
        {
        }

        public override ConsoleColor GetColor()
        {
            return ConsoleColor.Yellow;
        }
    }
}
