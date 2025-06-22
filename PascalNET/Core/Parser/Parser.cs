using PascalNET.Compiler;
using PascalNET.Core.AST;
using PascalNET.Core.AST.BasicNodes;
using PascalNET.Core.AST.Declartions;
using PascalNET.Core.AST.Expressions;
using PascalNET.Core.AST.Nodes;
using PascalNET.Core.AST.Statements;
using PascalNET.Core.Lexer.Tokens;

namespace PascalNET.Core.Parser
{
    /// <summary>
    /// Синтаксический анализатор с обработкой ошибок
    /// </summary>
    internal class Parser
    {
        private readonly List<Token> _tokens;

        private readonly ErrorReporter _errorReporter;

        private int _position;

        private Token? _currentToken;

        private bool _panicMode = false;

        public Parser(List<Token> tokens, ErrorReporter errorReporter)
        {
            _tokens = tokens;
            _errorReporter = errorReporter;
            _position = 0;
            _currentToken = tokens.Count > 0 ? tokens[0] : null;
        }

        private void Move()
        {
            if (_position < _tokens.Count - 1)
            {
                _position++;
                _currentToken = _tokens[_position];
            }
        }

        private bool Match(TokenType expectedType)
        {
            return _currentToken?.Type == expectedType;
        }

        private bool Match(params TokenType[] expectedTypes)
        {
            if (_currentToken == null) return false;
            return expectedTypes.Contains(_currentToken.Type);
        }

        private Token? Consume(TokenType expectedType, string errorMessage = "")
        {
            if (Match(expectedType))
            {
                var token = _currentToken;
                Move();
                _panicMode = false;
                return token;
            }
            else
            {
                if (string.IsNullOrEmpty(errorMessage))
                {
                    errorMessage = $"Ожидался {GetTokenDescription(expectedType)}, получен {GetTokenDescription(_currentToken?.Type ?? TokenType.Eof)}";
                }

                _errorReporter.ReportSyntaxError(
                    errorMessage,
                    _currentToken,
                    GetSuggestionForExpectedToken(expectedType)
                );

                _panicMode = true;
                return null;
            }
        }

        private string GetTokenDescription(TokenType tokenType)
        {
            return tokenType switch
            {
                TokenType.Begin => "'begin'",
                TokenType.End => "'end'",
                TokenType.If => "'if'",
                TokenType.Then => "'then'",
                TokenType.Else => "'else'",
                TokenType.While => "'while'",
                TokenType.Do => "'do'",
                TokenType.Var => "'var'",
                TokenType.Assign => "':='",
                TokenType.Semicolon => "';'",
                TokenType.Colon => "':'",
                TokenType.Comma => "','",
                TokenType.Dot => "'.'",
                TokenType.LeftParen => "'('",
                TokenType.RightParen => "')'",
                TokenType.Identifier => "идентификатор",
                TokenType.IntegerLiteral => "целое число",
                TokenType.RealLiteral => "вещественное число",
                TokenType.StringLiteral => "строковая константа",
                TokenType.IntegerType => "тип integer",
                TokenType.RealType => "тип real",
                TokenType.BooleanType => "тип boolean",
                TokenType.StringType => "тип string",
                TokenType.Eof => "конец файла",
                _ => tokenType.ToString().ToLower()
            };
        }

        private string GetSuggestionForExpectedToken(TokenType expectedType)
        {
            return expectedType switch
            {
                TokenType.Semicolon => "Добавьте ';' для завершения оператора",
                TokenType.Begin => "Добавьте 'begin' для начала блока операторов",
                TokenType.End => "Добавьте 'end' для закрытия блока операторов",
                TokenType.Then => "Добавьте 'then' после условия if",
                TokenType.Do => "Добавьте 'do' после условия while",
                TokenType.Colon => "Добавьте ':' после списка переменных",
                TokenType.Assign => "Используйте ':=' для присваивания",
                TokenType.Dot => "Добавьте '.' в конце программы",
                TokenType.RightParen => "Добавьте закрывающую скобку ')'",
                TokenType.Identifier => "Укажите имя переменной или процедуры",
                _ => "Проверьте синтаксис"
            };
        }

        private void Synchronize()
        {
            _panicMode = false;

            // Синхронизируемся на ключевых токенах
            HashSet<TokenType> syncTokens =
            [
                TokenType.Semicolon,
                TokenType.Begin,
                TokenType.End,
                TokenType.If,
                TokenType.While,
                TokenType.Var,
                TokenType.Eof
            ];

            while (_currentToken != null && !syncTokens.Contains(_currentToken.Type))
            {
                Move();
            }

            // Если мы нашли точку с запятой, пропускаем её
            if (Match(TokenType.Semicolon))
            {
                Move();
            }
        }

        public ExecutionNode? ParseProgram()
        {
            try
            {
                List<IDeclaration> declarations = [];
                IStatement? mainStatement = null;

                // Парсинг объявлений переменных
                if (Match(TokenType.Var))
                {
                    var varDeclarations = ParseVariableDeclarations();
                    if (varDeclarations != null)
                    {
                        declarations.AddRange(varDeclarations);
                    }
                }

                // Парсинг основного оператора
                if (!_panicMode && _currentToken?.Type != TokenType.Eof)
                {
                    mainStatement = ParseStatement();
                }

                // Ожидаем точку в конце программы
                if (!_panicMode)
                {
                    Consume(TokenType.Dot, "Ожидается '.' в конце программы");
                }

                // Даже если были ошибки, пытаемся вернуть частичное дерево
                return new ExecutionNode(declarations, mainStatement ?? new CompoundStatement([]));
            }
            catch (Exception ex)
            {
                _errorReporter.ReportSyntaxError(
                    $"Критическая ошибка парсера: {ex.Message}",
                    _currentToken
                );
                return null;
            }
        }

        private List<VariableDeclaration>? ParseVariableDeclarations()
        {
            List<VariableDeclaration> declarations = [];

            Consume(TokenType.Var);

            while (!Match(TokenType.Begin, TokenType.Eof) && _currentToken != null)
            {
                if (_panicMode)
                {
                    Synchronize();
                    if (!Match(TokenType.Identifier))
                        break;
                }

                try
                {
                    var declaration = ParseSingleVariableDeclaration();
                    if (declaration != null)
                    {
                        declarations.Add(declaration);
                    }
                }
                catch (Exception)
                {
                    // Ошибка уже зарегистрирована, синхронизируемся
                    Synchronize();
                    break;
                }
            }

            return declarations;
        }

        private VariableDeclaration? ParseSingleVariableDeclaration()
        {
            // Парсинг списка идентификаторов
            List<string> identifiers = [];

            var firstIdentifier = Consume(TokenType.Identifier, "Ожидается имя переменной");
            if (firstIdentifier == null) return null;

            identifiers.Add(firstIdentifier.Value);

            while (Match(TokenType.Comma))
            {
                Move();
                var identifier = Consume(TokenType.Identifier, "Ожидается имя переменной после ','");
                if (identifier == null) break;
                identifiers.Add(identifier.Value);
            }

            // Ожидаем двоеточие
            if (Consume(TokenType.Colon, "Ожидается ':' после списка переменных") == null)
                return null;            // Парсинг типа - теперь типы являются ключевыми словами
            var typeToken = ParseTypeToken();
            if (typeToken == null) return null;

            string typeName = typeToken.Value;

            // Проверка корректности типа
            if (!IsValidType(typeName))
            {
                _errorReporter.ReportTypeError(
                    $"Неизвестный тип '{typeName}'",
                    typeToken.Line,
                    typeToken.Column,
                    "Используйте один из стандартных типов: integer, real, boolean, string"
                );
            }

            // Ожидаем точку с запятой
            Consume(TokenType.Semicolon, "Ожидается ';' после объявления переменной");

            return new VariableDeclaration(identifiers, typeName);
        }

        private bool IsValidType(string typeName)
        {
            string[] validTypes = ["integer", "real", "boolean", "string"];
            return validTypes.Contains(typeName.ToLower());
        }

        private IStatement? ParseStatement()
        {
            if (_panicMode)
            {
                Synchronize();
            }

            if (_currentToken == null) return null;

            try
            {
                return _currentToken.Type switch
                {
                    TokenType.Begin => ParseCompoundStatement(),
                    TokenType.Identifier => ParseAssignmentStatement(),
                    TokenType.If => ParseIfStatement(),
                    TokenType.While => ParseWhileStatement(),
                    _ => throw new InvalidOperationException($"Неожиданный токен в начале оператора: {_currentToken.Type}")
                };
            }
            catch (Exception)
            {
                _errorReporter.ReportSyntaxError(
                    "Ошибка при разборе оператора",
                    _currentToken,
                    "Проверьте синтаксис оператора"
                );
                Synchronize();
                return null;
            }
        }

        private CompoundStatement? ParseCompoundStatement()
        {
            if (Consume(TokenType.Begin) == null) return null;

            var statements = new List<IStatement>();

            // Парсинг операторов до 'end'
            while (!Match(TokenType.End) && _currentToken?.Type != TokenType.Eof)
            {
                if (_panicMode)
                {
                    Synchronize();
                    if (Match(TokenType.End)) break;
                }

                var statement = ParseStatement();
                if (statement != null)
                {
                    statements.Add(statement);
                }

                // Проверяем наличие точки с запятой между операторами
                if (Match(TokenType.Semicolon))
                {
                    Move();
                }
                else if (!Match(TokenType.End))
                {
                    _errorReporter.ReportSyntaxError(
                        "Ожидается ';' между операторами",
                        _currentToken,
                        "Добавьте ';' после оператора"
                    );
                    // Пытаемся восстановиться
                    if (!_panicMode)
                    {
                        _panicMode = true;
                    }
                }
            }

            Consume(TokenType.End, "Ожидается 'end' для закрытия блока");

            return new CompoundStatement(statements);
        }

        private AssignmentStatement? ParseAssignmentStatement()
        {
            var identifierToken = Consume(TokenType.Identifier, "Ожидается имя переменной");
            if (identifierToken == null)
                return null;

            if (Consume(TokenType.Assign, "Ожидается ':=' для присваивания") == null) return null;

            var expression = ParseExpression();
            if (expression == null)
                return null;

            return new AssignmentStatement(identifierToken.Value, expression);
        }

        private ConditionStatement? ParseIfStatement()
        {
            if (Consume(TokenType.If) == null)
                return null;

            var condition = ParseExpression();
            if (condition == null)
                return null;

            if (Consume(TokenType.Then, "Ожидается 'then' после условия") == null)
                return null;

            var thenStatement = ParseStatement();
            if (thenStatement == null)
                return null;

            IStatement? elseStatement = null;
            if (Match(TokenType.Else))
            {
                Move();
                elseStatement = ParseStatement();
            }

            return new ConditionStatement(condition, thenStatement, elseStatement);
        }

        private CycleStatement? ParseWhileStatement()
        {
            if (Consume(TokenType.While) == null)
                return null;

            var condition = ParseExpression();
            if (condition == null)
                return null;

            if (Consume(TokenType.Do, "Ожидается 'do' после условия цикла") == null)
                return null;

            var body = ParseStatement();
            if (body == null)
                return null;

            return new CycleStatement(condition, body);
        }

        private IExpression? ParseExpression()
        {
            return ParseOrExpression();
        }

        private IExpression? ParseOrExpression()
        {
            var left = ParseAndExpression();
            if (left == null)
                return null;

            while (Match(TokenType.Or))
            {
                var operatorToken = _currentToken;
                Move();
                var right = ParseAndExpression();
                if (right == null)
                    break;
                left = new BinaryOperation(left, operatorToken!.Value, right);
            }

            return left;
        }

        private IExpression? ParseAndExpression()
        {
            var left = ParseEqualityExpression();
            if (left == null)
                return null;

            while (Match(TokenType.And))
            {
                var operatorToken = _currentToken;
                Move();
                var right = ParseEqualityExpression();
                if (right == null)
                    break;
                left = new BinaryOperation(left, operatorToken!.Value, right);
            }

            return left;
        }

        private IExpression? ParseEqualityExpression()
        {
            var left = ParseRelationalExpression();
            if (left == null)
                return null;

            while (Match(TokenType.Equal, TokenType.NotEqual))
            {
                var operatorToken = _currentToken;
                Move();
                var right = ParseRelationalExpression();
                if (right == null)
                    break;
                left = new BinaryOperation(left, operatorToken!.Value, right);
            }

            return left;
        }

        private IExpression? ParseRelationalExpression()
        {
            var left = ParseAdditiveExpression();
            if (left == null) return
                    null;

            while (Match(TokenType.Less, TokenType.LessEqual, TokenType.Greater, TokenType.GreaterEqual))
            {
                var operatorToken = _currentToken;
                Move();
                var right = ParseAdditiveExpression();
                if (right == null)
                    break;
                left = new BinaryOperation(left, operatorToken!.Value, right);
            }

            return left;
        }

        private IExpression? ParseAdditiveExpression()
        {
            var left = ParseMultiplicativeExpression();
            if (left == null)
                return null;

            while (Match(TokenType.Plus, TokenType.Minus))
            {
                var operatorToken = _currentToken;
                Move();
                var right = ParseMultiplicativeExpression();
                if (right == null)
                    break;
                left = new BinaryOperation(left, operatorToken!.Value, right);
            }

            return left;
        }

        private IExpression? ParseMultiplicativeExpression()
        {
            var left = ParseUnaryExpression();
            if (left == null)
                return null;

            while (Match(TokenType.Multiply, TokenType.Divide, TokenType.Mod))
            {
                var operatorToken = _currentToken;
                Move();
                var right = ParseUnaryExpression();
                if (right == null)
                    break;
                left = new BinaryOperation(left, operatorToken!.Value, right);
            }

            return left;
        }

        private IExpression? ParseUnaryExpression()
        {
            if (Match(TokenType.Minus, TokenType.Plus, TokenType.Not))
            {
                var operatorToken = _currentToken;
                Move();
                var operand = ParseUnaryExpression();
                if (operand == null)
                    return null;
                return new UnaryOperation(operatorToken!.Value, operand);
            }

            return ParsePrimaryExpression();
        }
        private IExpression? ParsePrimaryExpression()
        {
            if (_currentToken == null)
                return null;

            switch (_currentToken.Type)
            {
                case TokenType.IntegerLiteral:
                    var intToken = _currentToken;
                    Move();
                    return new IntegerLiteral(int.Parse(intToken.Value));

                case TokenType.RealLiteral:
                    var realToken = _currentToken;
                    Move();
                    return new RealLiteral(double.Parse(realToken.Value));

                case TokenType.StringLiteral:
                    var stringToken = _currentToken;
                    Move();
                    // Убираем кавычки
                    var stringValue = stringToken.Value[1..^1];
                    return new StringLiteral(stringValue);

                case TokenType.Identifier:
                    var identifierToken = _currentToken;
                    Move();
                    return new Identifier(identifierToken.Value);

                case TokenType.LeftParen:
                    Move();
                    var expression = ParseExpression();
                    if (expression == null) return null;
                    Consume(TokenType.RightParen, "Ожидается ')' после выражения");
                    return expression;

                default:
                    _errorReporter.ReportSyntaxError(
                        $"Неожиданный токен в выражении: {GetTokenDescription(_currentToken.Type)}",
                        _currentToken,
                        "Ожидается переменная, число, строка или выражение в скобках"
                    );
                    return null;
            }
        }

        private Token? ParseTypeToken()
        {
            if (Match(TokenType.IntegerType, TokenType.RealType, TokenType.BooleanType, TokenType.StringType))
            {
                var token = _currentToken;
                Move();
                return token;
            }
            else if (Match(TokenType.Identifier))
            {
                // Пользовательские типы (пока не поддерживаются)
                var token = _currentToken;
                _errorReporter.ReportTypeError(
                    $"Неизвестный тип '{token!.Value}'",
                    token.Line,
                    token.Column,
                    "Используйте один из стандартных типов: integer, real, boolean, string"
                );
                Move();
                return token;
            }
            else
            {
                _errorReporter.ReportSyntaxError(
                    "Ожидается имя типа",
                    _currentToken,
                    "Используйте один из стандартных типов: integer, real, boolean, string"
                );
                return null;
            }
        }
    }
}
