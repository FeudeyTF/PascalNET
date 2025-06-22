using PascalNET.Core.Lexer.Tokens;
using PascalNET.Core.Messages;
using PascalNET.Core.Messages.Errors;

namespace PascalNET
{
    public class ConsoleMessageFormatter : IMessageFormatter
    {
        public bool HasErrors => _messages.Any(e => e is CompilerError);

        public bool HasWarnings => _messages.Any(e => e is WarningMessage);

        public int ErrorCount => _messages.Count(e => e is CompilerError);

        public int WarningCount => _messages.Count(e => e is WarningMessage);

        public bool TooManyErrors => ErrorCount >= _maxErrors;

        private readonly List<CompilerMessage> _messages;

        private readonly string _sourceCode;

        private readonly string[] _sourceLines;

        private readonly int _maxErrors;

        public ConsoleMessageFormatter(string sourceCode = "", int maxErrors = 20)
        {
            _messages = [];
            _sourceCode = sourceCode;
            _sourceLines = sourceCode.Split('\n');
            _maxErrors = maxErrors;
        }

        public void ReportMessage(CompilerMessage error)
        {
            if (string.IsNullOrEmpty(error.SourceFragment) && !string.IsNullOrEmpty(_sourceCode))
            {
                error.SourceFragment = GetSourceFragment(error.Line, error.Column);
            }
            _messages.Add(error);
        }

        public void ReportLexicalError(string message, int line, int column, string suggestion = "")
        {
            ReportMessage(new LexicalError(message, line, column, suggestion));
        }

        public void ReportSyntaxError(string message, Token? token, string suggestion = "")
        {
            ReportMessage(new SyntaxError(message, token, suggestion: suggestion));
        }

        public void ReportSemanticError(string message, int line, int column, string suggestion = "")
        {
            ReportMessage(new SemanticError(message, line, column, suggestion: suggestion));
        }

        public void ReportTypeError(string message, int line, int column, string suggestion = "")
        {
            ReportMessage(new TypeError(message, line, column, suggestion));
        }

        public void ReportWarning(string message, int line, int column, string suggestion = "")
        {
            ReportMessage(new WarningMessage(message, line, column, suggestion));
        }

        public void ReportMessage(string message, int line, int column, string sourceFragment = "", string suggestion = "")
        {
            ReportMessage(new CompilerMessage(message, line, column, sourceFragment, suggestion));
        }

        private string GetSourceFragment(int line, int column)
        {
            if (line <= 0 || line > _sourceLines.Length)
                return string.Empty;

            var sourceLine = _sourceLines[line - 1];

            var result = sourceLine;
            if (column > 0 && column <= sourceLine.Length)
            {
                result += "\n" + new string(' ', Math.Max(0, column - 1)) + "^";
            }
            return result;
        }

        public void PrintAllErrors()
        {
            if (!_messages.Any())
            {
                Console.WriteLine("Ошибок не найдено.");
                return;
            }

            var sortedErrors = _messages.OrderBy(e => e.Line).ThenBy(e => e.Column);

            foreach (var error in sortedErrors)
            {
                PrintMessage(error);
            }

            Console.WriteLine($"\nВсего ошибок: {ErrorCount}");
            Console.WriteLine($"Всего предупреждений: {WarningCount}");
        }

        private void PrintMessage(CompilerMessage message)
        {
            Console.ForegroundColor = message.GetColor();
            Console.WriteLine(message.ToString());
            Console.ResetColor();
        }

        public void Clear()
        {
            _messages.Clear();
        }

        public Dictionary<string, int> GetErrorStatistics()
        {
            return _messages.GroupBy(e => e.GetType().Name)
                         .ToDictionary(g => g.Key, g => g.Count());
        }

        public bool CanContinueCompilation()
        {
            return !_messages.Any(e => e is LexicalError);
        }

        public bool CanPerformSemanticAnalysis()
        {
            return !_messages.Any(e => e is LexicalError);
        }
    }
}