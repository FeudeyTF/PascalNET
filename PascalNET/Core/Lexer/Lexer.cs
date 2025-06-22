using PascalNET.Core.Lexer.Tokens;
using PascalNET.Core.Messages;
using System.Text;
using System.Text.RegularExpressions;

namespace PascalNET.Core.Lexer
{
    internal class Lexer
    {
        private static readonly Dictionary<string, TokenType> _keywords = new()
        {
            {"if", TokenType.If},
            {"then", TokenType.Then},
            {"else", TokenType.Else},
            {"while", TokenType.While},
            {"do", TokenType.Do},
            {"begin", TokenType.Begin},
            {"end", TokenType.End},
            {"var", TokenType.Var},
            {"mod", TokenType.Mod},
            {"and", TokenType.And},
            {"or", TokenType.Or},
            {"not", TokenType.Not},
            {"program", TokenType.Program},
            {"function", TokenType.Function},
            {"procedure", TokenType.Procedure},
            {"const", TokenType.Const},
            {"integer", TokenType.IntegerType},
            {"real", TokenType.RealType},
            {"boolean", TokenType.BooleanType},
            {"string", TokenType.StringType}
        };

        private static readonly List<(Regex Pattern, TokenType Type)> _tokenPatterns =
        [
            (new Regex(@"^\s+"), TokenType.Whitespace),
            (new Regex(@"^\r?\n"), TokenType.Newline),
            (new Regex(@"^//.*"), TokenType.Comment),
            (new Regex(@"^/\*.*?\*/", RegexOptions.Singleline), TokenType.Comment),
            (new Regex(@"^\{.*?\}", RegexOptions.Singleline), TokenType.Comment),
            (new Regex(@"^:="), TokenType.Assign),
            (new Regex(@"^:"), TokenType.Colon),
            (new Regex(@"^<="), TokenType.LessEqual),
            (new Regex(@"^>="), TokenType.GreaterEqual),
            (new Regex(@"^<>"), TokenType.NotEqual),
            (new Regex(@"^<"), TokenType.Less),
            (new Regex(@"^>"), TokenType.Greater),
            (new Regex(@"^="), TokenType.Equal),
            (new Regex(@"^\+"), TokenType.Plus),
            (new Regex(@"^-"), TokenType.Minus),
            (new Regex(@"^\*"), TokenType.Multiply),
            (new Regex(@"^/"), TokenType.Divide),
            (new Regex(@"^;"), TokenType.Semicolon),
            (new Regex(@"^,"), TokenType.Comma),
            (new Regex(@"^\."), TokenType.Dot),
            (new Regex(@"^\("), TokenType.LeftParen),
            (new Regex(@"^\)"), TokenType.RightParen),
            (new Regex(@"^\["), TokenType.LeftBracket),
            (new Regex(@"^\]"), TokenType.RightBracket),
            (new Regex(@"^""([^""]|"""")*"""), TokenType.StringLiteral),
            (new Regex(@"^'([^']|'')*'"), TokenType.StringLiteral),
            (new Regex(@"^[0-9]+\.[0-9]+"), TokenType.RealLiteral),
            (new Regex(@"^[0-9]+"), TokenType.IntegerLiteral),
            (new Regex(@"^[a-zA-Z_][a-zA-Z0-9_]*"), TokenType.Identifier)
        ];

        private char? CurrentChar => _position >= _sourceCode.Length ? null : _sourceCode[_position];

        private readonly string _sourceCode;

        private readonly IMessageFormatter _messageFormatter;

        private int _position;

        private int _line;

        private int _column;

        private int _startColumn;

        public Lexer(string sourceCode, IMessageFormatter messageFormatter)
        {
            _sourceCode = sourceCode;
            _messageFormatter = messageFormatter;
            _position = 0;
            _line = 1;
            _column = 1;
            _startColumn = 1;
        }

        public List<Token> Tokenize()
        {
            List<Token> tokens = [];

            while (_position < _sourceCode.Length)
            {
                _startColumn = _column;
                Token? token = TryMatchToken();

                if (token != null)
                {
                    if (token.Type != TokenType.Whitespace && token.Type != TokenType.Comment && token.Type != TokenType.Newline)
                    {
                        tokens.Add(token);
                    }
                }
                else
                {
                    var problematicChar = CurrentChar ?? '\0';

                    if (problematicChar == '"' || problematicChar == '\'')
                    {
                        HandleUnterminatedString(problematicChar);
                    }
                    else
                    {
                        _messageFormatter.ReportLexicalError(
                            $"Неожиданный символ '{problematicChar}'",
                            _line,
                            _column,
                            "Проверьте правильность написания символов и операторов"
                        );
                        Move();
                    }
                }
            }

            tokens.Add(new Token(TokenType.Eof, "", _line, _column));
            return tokens;
        }

        private Token? TryMatchToken()
        {
            if (_position >= _sourceCode.Length)
                return new Token(TokenType.Eof, "", _line, _column);

            var remainingText = _sourceCode[_position..];

            foreach (var (pattern, tokenType) in _tokenPatterns)
            {
                Match match = pattern.Match(remainingText);
                if (match.Success)
                {
                    var value = match.Value;
                    Token token = new(tokenType, value, _line, _startColumn);

                    if (tokenType == TokenType.Identifier)
                    {
                        var lowerValue = value.ToLower();
                        if (_keywords.TryGetValue(lowerValue, out var keyword))
                        {
                            token.Type = keyword;
                        }
                    }

                    if (!ValidateToken(token))
                        return null;

                    for (int i = 0; i < value.Length; i++)
                        Move();

                    return token;
                }
            }

            return null;
        }

        private bool ValidateToken(Token token)
        {
            switch (token.Type)
            {
                case TokenType.StringLiteral:
                    return ValidateStringLiteral(token);
                case TokenType.Comment:
                    return ValidateComment(token);
                case TokenType.Identifier:
                    return ValidateIdentifier(token);
                default:
                    return true;
            }
        }

        private bool ValidateStringLiteral(Token token)
        {
            var value = token.Value;

            if (value.Length < 2)
            {
                _messageFormatter.ReportLexicalError(
                    "Незакрытая строковая константа",
                    token.Line,
                    token.Column,
                    "Добавьте закрывающую кавычку"
                );
                return false;
            }

            char quote = value[0];
            if (value[^1] != quote)
            {
                _messageFormatter.ReportLexicalError(
                    "Незакрытая строковая константа",
                    token.Line,
                    token.Column,
                    "Добавьте закрывающую кавычку"
                );
                return false;
            }

            if (value.Length > 257)
            {
                _messageFormatter.ReportWarning(
                    "Строковая константа слишком длинная (максимум 255 символов)",
                    token.Line,
                    token.Column,
                    "Сократите строку или разбейте на части"
                );
            }

            return true;
        }

        private bool ValidateIdentifier(Token token)
        {
            if (token.Value.Length > 63)
            {
                _messageFormatter.ReportWarning(
                    "Идентификатор слишком длинный (рекомендуется не более 63 символов)",
                    token.Line,
                    token.Column,
                    "Сократите имя идентификатора"
                );
            }

            return true;
        }

        private bool ValidateComment(Token token)
        {
            var value = token.Value;
            if (value.StartsWith("/*") && !value.EndsWith("*/"))
            {
                _messageFormatter.ReportLexicalError(
                    "Незакрытый блочный комментарий",
                    token.Line,
                    token.Column,
                    "Добавьте закрывающий '*/')"
                );
                return false;
            }

            if (value.StartsWith('{') && !value.EndsWith('}'))
            {
                _messageFormatter.ReportLexicalError(
                    "Незакрытый комментарий",
                    token.Line,
                    token.Column,
                    "Добавьте закрывающую '}'"
                );
                return false;
            }

            return true;
        }

        private void Move()
        {
            if (_position < _sourceCode.Length)
            {
                if (_sourceCode[_position] == '\n')
                {
                    _line++;
                    _column = 1;
                }
                else
                {
                    _column++;
                }
                _position++;
            }
        }

        /// <summary>
        /// Обрабатывает незакрытую строку, пропуская символы до конца строки или до закрывающей кавычки
        /// </summary>
        private void HandleUnterminatedString(char quoteChar)
        {
            var startLine = _line;
            var startColumn = _column;
            StringBuilder content = new();

            content.Append(CurrentChar);
            Move();

            while (_position < _sourceCode.Length && CurrentChar != '\n' && CurrentChar != '\r')
            {
                if (CurrentChar == quoteChar)
                {
                    content.Append(CurrentChar);
                    Move();
                    return;
                }

                content.Append(CurrentChar);
                Move();
            }

            _messageFormatter.ReportLexicalError(
                "Незакрытая строковая константа",
                startLine,
                startColumn,
                $"Добавьте закрывающую кавычку '{quoteChar}' или завершите строку"
            );
        }
    }
}