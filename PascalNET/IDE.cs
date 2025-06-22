using PascalNET.Core;
using PascalNET.Core.AST;
using PascalNET.Core.Lexer;
using PascalNET.Core.Lexer.Tokens;
using PascalNET.Core.Parser;

namespace PascalNET
{
    internal class IDE
    {
        private readonly ConsoleMessageFormatter _errorReporter;

        public IDE()
        {
            _errorReporter = new ConsoleMessageFormatter();
        }

        public ExecutionNode? Compile(string sourceCode)
        {
            if (string.IsNullOrEmpty(sourceCode))
            {
                _errorReporter.ReportLexicalError("Пустой исходный код", 1, 1, suggestion: "Введите код программы");
                return null;
            }

            ConsoleMessageFormatter errorReporter = new(sourceCode);

            try
            {
                // Этап 1: Лексический анализ
                Console.WriteLine("=== Этап 1: Лексический анализ ===");
                var lexer = new Lexer(sourceCode, errorReporter);
                var tokens = lexer.Tokenize();

                if (errorReporter.HasErrors && !errorReporter.CanContinueCompilation())
                {
                    Console.WriteLine("Критические лексические ошибки. Компиляция прервана.");
                    errorReporter.PrintAllErrors();
                    return null;
                }

                // Этап 2: Синтаксический анализ
                Console.WriteLine("\n=== Этап 2: Синтаксический анализ ===");
                var parser = new Parser(tokens, errorReporter);
                var ast = parser.ParseProgram();

                // Этап 3: Семантический анализ
                if (ast != null && errorReporter.CanPerformSemanticAnalysis())
                {
                    Console.WriteLine("\n=== Этап 3: Семантический анализ ===");
                    var semanticAnalyzer = new SemanticAnalyzer(errorReporter);
                    semanticAnalyzer.AnalyzeProgram(ast, parser.PositionTracker);
                }
                else if (ast == null)
                {
                    Console.WriteLine("\nСемантический анализ пропущен: не удалось построить AST.");
                }
                else if (!errorReporter.CanPerformSemanticAnalysis())
                {
                    Console.WriteLine("\nСемантический анализ пропущен: критические лексические ошибки.");
                }

                Console.WriteLine("\n=== Результаты компиляции ===");

                if (!errorReporter.HasErrors && !errorReporter.HasWarnings)
                {
                    if (ast != null)
                        PrintAstStructure(ast);
                    Console.WriteLine("Компиляция завершена успешно!");
                    Console.WriteLine("Программа не содержит ошибок и предупреждений.");
                }
                else
                {
                    errorReporter.PrintAllErrors();

                    var stats = errorReporter.GetErrorStatistics();
                    Console.WriteLine("\n=== Статистика ===");
                    foreach (var kvp in stats)
                    {
                        Console.WriteLine($"{kvp.Key}: {kvp.Value}");
                    }
                }

                return ast;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Критическая ошибка компилятора: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return null;
            }
        }

        public void LexicAnalyze(string sourceCode)
        {
            ConsoleMessageFormatter errorReporter = new(sourceCode);
            var lexer = new Lexer(sourceCode, errorReporter);
            var tokens = lexer.Tokenize();

            Console.WriteLine("=== Результаты лексического анализа ===");
            PrintTokens(tokens);

            if (errorReporter.HasErrors || errorReporter.HasWarnings)
            {
                Console.WriteLine("\n=== Ошибки и предупреждения ===");
                errorReporter.PrintAllErrors();
            }
        }

        public ExecutionNode? SyntaxAnalyze(string sourceCode)
        {
            var lexer = new Lexer(sourceCode, new ConsoleMessageFormatter());
            var tokens = lexer.Tokenize();

            ConsoleMessageFormatter errorReporter = new(sourceCode);
            Parser parser = new(tokens, errorReporter);
            var ast = parser.ParseProgram();

            Console.WriteLine("=== Результаты синтаксического анализа ===");
            if (ast != null)
            {
                PrintAstStructure(ast);
            }

            if (errorReporter.HasErrors || errorReporter.HasWarnings)
            {
                Console.WriteLine("\n=== Ошибки и предупреждения ===");
                errorReporter.PrintAllErrors();
            }

            return ast;
        }

        private void PrintTokens(List<Token> tokens)
        {
            Console.WriteLine("Токены:");
            foreach (var token in tokens.Where(t => t.Type != TokenType.Eof))
            {
                Console.WriteLine($"  {token.Type,-15} | {token.Value,-20} | Строка {token.Line}, Столбец {token.Column}");
            }
        }

        private void PrintAstStructure(ExecutionNode ast)
        {
            var visualizer = new AstVisualizer();

            Console.WriteLine(visualizer.VisualizeProgram(ast));

            Console.WriteLine(visualizer.CreateSummary(ast));
        }

        public CompilationReport GetCompilationReport(string sourceCode)
        {
            ConsoleMessageFormatter errorReporter = new(sourceCode);

            Lexer lexer = new(sourceCode, errorReporter);
            var tokens = lexer.Tokenize();

            Parser parser = new(tokens, errorReporter);
            var ast = parser.ParseProgram();

            if (ast != null)
            {
                var semanticAnalyzer = new SemanticAnalyzer(errorReporter);
                semanticAnalyzer.AnalyzeProgram(ast, parser.PositionTracker);
            }

            return new CompilationReport(sourceCode, tokens.Count, ast, errorReporter.GetErrorStatistics(), errorReporter.HasErrors, errorReporter.HasWarnings, errorReporter.HasErrors);
        }
    }

    internal class CompilationReport
    {
        public string SourceCode { get; set; }

        public int TokenCount { get; set; }

        public ExecutionNode? AST { get; set; }

        public Dictionary<string, int> ErrorStatistics { get; set; }

        public bool HasErrors { get; set; }

        public bool HasWarnings { get; set; }

        public bool CanExecute { get; set; }

        public CompilationReport(string sourceCode, int tokenCount, ExecutionNode? aST, Dictionary<string, int> errorStatistics, bool hasErrors, bool hasWarnings, bool canExecute)
        {
            SourceCode = sourceCode;
            TokenCount = tokenCount;
            AST = aST;
            ErrorStatistics = errorStatistics;
            HasErrors = hasErrors;
            HasWarnings = hasWarnings;
            CanExecute = canExecute;
        }

        public void PrintSummary()
        {
            Console.WriteLine("=== Сводка компиляции ===");
            Console.WriteLine($"Токенов проанализировано: {TokenCount}");
            Console.WriteLine($"AST построено: {(AST != null ? "Да" : "Нет")}");
            Console.WriteLine($"Имеются ошибки: {(HasErrors ? "Да" : "Нет")}");
            Console.WriteLine($"Имеются предупреждения: {(HasWarnings ? "Да" : "Нет")}");
            Console.WriteLine($"Можно выполнить: {(CanExecute ? "Да" : "Нет")}");

            if (ErrorStatistics.Any())
            {
                Console.WriteLine("\nСтатистика проблем:");
                foreach (var kvp in ErrorStatistics)
                {
                    Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                }
            }
        }
    }
}
