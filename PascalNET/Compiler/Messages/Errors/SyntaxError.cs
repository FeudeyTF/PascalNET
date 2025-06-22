using PascalNET.Core.Lexer.Tokens;
using PascalNET.Errors;

namespace PascalNET.Compiler.Messages.Errors
{
    public class SyntaxError : CompilerError
    {
        public Token? Token { get; }

        public SyntaxError(string message, Token? token, string sourceFragment = "", string suggestion = "")
            : base($"Синтаксическая ошибка в строке {token?.Line ?? 0}, столбец {token?.Column ?? 0}: {message}", token?.Line ?? 0, token?.Column ?? 0, sourceFragment, suggestion)
        {
            Token = token;
        }
    }
}