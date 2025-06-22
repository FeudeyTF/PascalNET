using PascalNET.Core.Lexer.Tokens;

namespace PascalNET.Core.Messages.Errors
{
    public class SyntaxError : CompilerError
    {
        public Token? Token { get; }

        public SyntaxError(string message, Token? token, string sourceFragment = "", string suggestion = "")
            : base(message, token?.Line ?? 0, token?.Column ?? 0, sourceFragment, suggestion)
        {
            Token = token;
        }
    }
}