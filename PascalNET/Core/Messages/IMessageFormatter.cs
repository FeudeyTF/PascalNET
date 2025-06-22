using PascalNET.Core.Lexer.Tokens;

namespace PascalNET.Core.Messages
{
    internal interface IMessageFormatter
    {
        public void ReportLexicalError(string message, int line, int column, string suggestion = "");

        public void ReportSyntaxError(string message, Token? token, string suggestion = "");

        public void ReportSemanticError(string message, int line, int column, string suggestion = "");

        public void ReportTypeError(string message, int line, int column, string suggestion = "");

        public void ReportWarning(string message, int line, int column, string suggestion = "");

        public void ReportMessage(string message, int line, int column, string sourceFragment = "", string suggestion = "");

        public void ReportMessage(CompilerMessage error);
    }
}
