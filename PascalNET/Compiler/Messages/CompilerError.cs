namespace PascalNET.Errors
{
    public class CompilerError : CompilerMessage
    {
        public CompilerError(string message, int line, int column, string sourceFragment = "", string suggestion = "") : base(message, line, column, sourceFragment, suggestion)
        {
        }

        public override ConsoleColor GetColor()
        {
            return ConsoleColor.Red;
        }
    }
}