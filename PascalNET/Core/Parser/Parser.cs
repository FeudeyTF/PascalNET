using PascalNET.Core.AST;
using PascalNET.Core.AST.BasicNodes;
using PascalNET.Core.AST.Declartions;
using PascalNET.Core.AST.Expressions;
using PascalNET.Core.AST.Nodes;
using PascalNET.Core.AST.Statements;
using PascalNET.Core.Lexer.Tokens;
using PascalNET.Core.Messages;

namespace PascalNET.Core.Parser
{
    internal class Parser
    {
        private readonly List<Token> _tokens;

        private readonly IMessageFormatter _messageFormatter;

        private int _position;

        private Token? _currentToken;

        private bool _panicMode = false;

        public Parser(List<Token> tokens, IMessageFormatter messageFormatter)
        {
            _tokens = tokens;
            _messageFormatter = messageFormatter;
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
            if (_currentToken == null)
                return false;
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

                _messageFormatter.ReportSyntaxError(
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
                TokenType.Program => "'program'",
                TokenType.Function => "'function'",
                TokenType.Procedure => "'procedure'",
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
            HashSet<TokenType> syncTokens =
            [
                TokenType.Semicolon,
                TokenType.Begin,
                TokenType.End,
                TokenType.If,
                TokenType.While,
                TokenType.Var,
                TokenType.Program,
                TokenType.Function,
                TokenType.Procedure,
                TokenType.Eof
            ];

            while (_currentToken != null && !syncTokens.Contains(_currentToken.Type))
                Move();

            if (Match(TokenType.Semicolon))
                Move();
        }
        public ExecutionNode? ParseProgram()
        {
            try
            {
                ProgramDeclaration? programDeclaration = null;
                List<IDeclaration> declarations = [];
                IStatement? mainStatement = null;

                // Парсинг объявления программы (опционально)
                if (Match(TokenType.Program))
                {
                    programDeclaration = ParseProgramDeclaration();
                }

                // Парсинг всех объявлений (функции, процедуры, переменные)
                while (Match(TokenType.Function, TokenType.Procedure, TokenType.Var))
                {
                    if (Match(TokenType.Function, TokenType.Procedure))
                    {
                        var functionDeclaration = ParseFunctionDeclaration();
                        if (functionDeclaration != null)
                        {
                            declarations.Add(functionDeclaration);
                        }
                    }
                    else if (Match(TokenType.Var))
                    {
                        var varDeclarations = ParseVariableDeclarations();
                        if (varDeclarations != null)
                        {
                            declarations.AddRange(varDeclarations);
                        }
                    }
                }

                if (!_panicMode && _currentToken?.Type != TokenType.Eof && !Match(TokenType.Dot))
                {
                    mainStatement = ParseStatement();
                }

                if (!_panicMode)
                {
                    Consume(TokenType.Dot, "Ожидается '.' в конце программы");
                }

                return new ExecutionNode(declarations, mainStatement ?? new CompoundStatement([]), programDeclaration);
            }
            catch (Exception ex)
            {
                _messageFormatter.ReportSyntaxError(
                    $"Критическая ошибка парсера: {ex.Message}",
                    _currentToken
                );
                return null;
            }
        }

        private List<VariableDeclaration> ParseVariableDeclarations()
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
                    Synchronize();
                    break;
                }
            }

            return declarations;
        }

        private VariableDeclaration? ParseSingleVariableDeclaration()
        {
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

            if (Consume(TokenType.Colon, "Ожидается ':' после списка переменных") == null)
                return null;
            var typeToken = ParseTypeToken();
            if (typeToken == null) return null;

            string typeName = typeToken.Value;

            if (!IsValidType(typeName))
            {
                _messageFormatter.ReportTypeError(
                    $"Неизвестный тип '{typeName}'",
                    typeToken.Line,
                    typeToken.Column,
                    "Используйте один из стандартных типов: integer, real, boolean, string"
                );
            }

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

            if (_currentToken == null)
                return null;

            try
            {
                return _currentToken.Type switch
                {
                    TokenType.Begin => ParseCompoundStatement(),
                    TokenType.Identifier => ParseIdentifierStatement(),
                    TokenType.If => ParseIfStatement(),
                    TokenType.While => ParseWhileStatement(),
                    _ => throw new InvalidOperationException($"Неожиданный токен в начале оператора: {_currentToken.Type}")
                };
            }
            catch (Exception e)
            {
                _messageFormatter.ReportSyntaxError(
                    "Ошибка при разборе оператора: " + e.ToString(),
                    _currentToken,
                    "Проверьте синтаксис оператора"
                );
                Synchronize();
                return null;
            }
        }

        private CompoundStatement? ParseCompoundStatement()
        {
            if (Consume(TokenType.Begin) == null)
                return null;

            var statements = new List<IStatement>();

            // Парсинг операторов до 'end'
            while (!Match(TokenType.End) && _currentToken?.Type != TokenType.Eof)
            {
                if (_panicMode)
                {
                    Synchronize();
                    if (Match(TokenType.End))
                        break;
                }

                var statement = ParseStatement();
                if (statement != null)
                {
                    statements.Add(statement);
                }

                if (Match(TokenType.Semicolon))
                {
                    Move();
                }
                else if (!Match(TokenType.End))
                {
                    _messageFormatter.ReportSyntaxError(
                        "Ожидается ';' между операторами",
                        _currentToken,
                        "Добавьте ';' после оператора"
                    );
                    if (!_panicMode)
                    {
                        _panicMode = true;
                    }
                }
            }

            Consume(TokenType.End, "Ожидается 'end' для закрытия блока");

            return new CompoundStatement(statements);
        }

        private IStatement? ParseIdentifierStatement()
        {
            var identifierToken = Consume(TokenType.Identifier, "Ожидается имя переменной или процедуры");
            if (identifierToken == null)
                return null;

            if (Match(TokenType.Assign))
            {
                Move();

                var expression = ParseExpression();
                if (expression == null)
                    return null;

                return new AssignmentStatement(identifierToken.Value, expression);
            }
            else if (Match(TokenType.LeftParen))
            {
                Move();

                List<IExpression> arguments = [];

                if (!Match(TokenType.RightParen))
                {
                    do
                    {
                        var argument = ParseExpression();
                        if (argument != null)
                        {
                            arguments.Add(argument);
                        }

                        if (Match(TokenType.Comma))
                        {
                            Move();
                        }
                        else
                        {
                            break;
                        }
                    } while (!Match(TokenType.RightParen) && _currentToken != null);
                }

                Consume(TokenType.RightParen, "Ожидается ')' после аргументов процедуры");
                return new ProcedureCallStatement(identifierToken.Value, arguments);
            }
            else
            {
                _messageFormatter.ReportSyntaxError(
                    $"После идентификатора '{identifierToken.Value}' ожидается ':=' или '('",
                    _currentToken,
                    "Используйте ':=' для присваивания или '()' для вызова процедуры"
                );
                return null;
            }
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

            return new ConditionStatement(condition, thenStatement, elseStatement!);
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

            var token = _currentToken;

            switch (_currentToken.Type)
            {
                case TokenType.IntegerLiteral:
                    Move();
                    return new IntegerLiteral(int.Parse(token.Value));

                case TokenType.RealLiteral:
                    Move();
                    return new RealLiteral(double.Parse(token.Value.Replace('.', ',')));

                case TokenType.StringLiteral:
                    Move();
                    var stringValue = token.Value[1..^1];
                    return new StringLiteral(stringValue);
                case TokenType.Identifier:
                    Move();

                    if (Match(TokenType.LeftParen))
                    {
                        Move();

                        List<IExpression> arguments = [];

                        if (!Match(TokenType.RightParen))
                        {
                            do
                            {
                                var argument = ParseExpression();
                                if (argument != null)
                                {
                                    arguments.Add(argument);
                                }

                                if (Match(TokenType.Comma))
                                {
                                    Move();
                                }
                                else
                                {
                                    break;
                                }
                            } while (!Match(TokenType.RightParen) && _currentToken != null);
                        }

                        Consume(TokenType.RightParen, "Ожидается ')' после аргументов функции");
                        return new FunctionCall(token.Value, arguments);
                    }
                    else
                    {
                        return new Identifier(token.Value);
                    }

                case TokenType.LeftParen:
                    Move();
                    var expression = ParseExpression();
                    if (expression == null)
                        return null;
                    Consume(TokenType.RightParen, "Ожидается ')' после выражения");
                    return expression;

                default:
                    _messageFormatter.ReportSyntaxError(
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
                _messageFormatter.ReportTypeError(
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
                _messageFormatter.ReportSyntaxError(
                    "Ожидается имя типа",
                    _currentToken,
                    "Используйте один из стандартных типов: integer, real, boolean, string"
                );
                return null;
            }
        }

        private ProgramDeclaration? ParseProgramDeclaration()
        {
            Consume(TokenType.Program, "Ожидается 'program'");

            var nameToken = Consume(TokenType.Identifier, "Ожидается имя программы");
            if (nameToken == null) return null;

            Consume(TokenType.Semicolon, "Ожидается ';' после имени программы");

            return new ProgramDeclaration(nameToken.Value);
        }

        private FunctionDeclaration? ParseFunctionDeclaration()
        {
            var isFunction = Match(TokenType.Function);
            Move();

            var nameToken = Consume(TokenType.Identifier, $"Ожидается имя {(isFunction ? "функции" : "процедуры")}");
            if (nameToken == null) return null;

            List<Parameter> parameters = [];
            if (Match(TokenType.LeftParen))
            {
                Move();

                if (!Match(TokenType.RightParen))
                {
                    parameters = ParseParameterList();
                }

                Consume(TokenType.RightParen, "Ожидается ')' после списка параметров");
            }

            string? returnType = null;
            if (isFunction)
            {
                Consume(TokenType.Colon, "Ожидается ':' после параметров функции");
                var returnTypeToken = ParseTypeToken();
                if (returnTypeToken != null)
                {
                    returnType = returnTypeToken.Value;
                }
            }

            Consume(TokenType.Semicolon, $"Ожидается ';' после объявления {(isFunction ? "функции" : "процедуры")}");

            List<IDeclaration> localDeclarations = [];
            if (Match(TokenType.Var))
            {
                var varDeclarations = ParseVariableDeclarations();
                localDeclarations.AddRange(varDeclarations);
            }

            var body = ParseStatement() ?? new CompoundStatement([]);

            Consume(TokenType.Semicolon, $"Ожидается ';' после тела {(isFunction ? "функции" : "процедуры")}");

            return new FunctionDeclaration(nameToken.Value, parameters, returnType, localDeclarations, body);
        }

        private List<Parameter> ParseParameterList()
        {
            List<Parameter> parameters = [];

            do
            {
                List<string> parameterNames = [];

                var nameToken = Consume(TokenType.Identifier, "Ожидается имя параметра");
                if (nameToken == null) break;
                parameterNames.Add(nameToken.Value);

                while (Match(TokenType.Comma))
                {
                    Move();
                    nameToken = Consume(TokenType.Identifier, "Ожидается имя параметра после ','");
                    if (nameToken == null) break;
                    parameterNames.Add(nameToken.Value);
                }

                Consume(TokenType.Colon, "Ожидается ':' после имен параметров");

                var typeToken = ParseTypeToken();
                if (typeToken == null) break;

                // Добавляем параметры с указанным типом
                foreach (var name in parameterNames)
                {
                    parameters.Add(new Parameter(name, typeToken.Value));
                }

                // Если есть ';', продолжаем парсинг следующей группы параметров
                if (Match(TokenType.Semicolon))
                {
                    Move();
                }
                else
                {
                    break;
                }
            } while (!Match(TokenType.RightParen) && _currentToken != null);

            return parameters;
        }
    }
}
