namespace PascalNET.Core.Messages
{
    /// <summary>
    /// Класс для представления сообщения компилятора
    /// </summary>
    public class CompilerMessage
    {
        public string Message { get; set; }

        public int Line { get; set; }

        public int Column { get; set; }

        public string SourceFragment { get; set; }

        public string Suggestion { get; set; }

        public CompilerMessage(string message, int line, int column, string sourceFragment = "", string suggestion = "")
        {
            Message = message;
            Line = line;
            Column = column;
            SourceFragment = sourceFragment;
            Suggestion = suggestion;
        }

        public virtual ConsoleColor GetColor()
        {
            return ConsoleColor.White;
        }
    }
}