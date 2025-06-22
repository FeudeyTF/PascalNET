using PascalNET.Core.AST;
using PascalNET.Core.AST.BasicNodes;
using PascalNET.Core.AST.Declartions;
using PascalNET.Core.AST.Expressions;
using PascalNET.Core.AST.Nodes;
using PascalNET.Core.AST.Statements;
using PascalNET.Core.Messages;
using PascalNET.Core.Parser;

namespace PascalNET.Core
{
    internal class SemanticAnalyzer
    {
        private static readonly HashSet<string> _builtInTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "integer", "real", "boolean", "string"
        };

        private readonly List<Dictionary<string, VariableInfo>> _scopeStack;

        private readonly Dictionary<string, FunctionInfo> _functions;

        private readonly IMessageFormatter _messageFormatter;

        private PositionTracker _positionTracker;

        public SemanticAnalyzer(IMessageFormatter messageFormatter)
        {
            _messageFormatter = messageFormatter;
            _scopeStack =
            [
                new Dictionary<string, VariableInfo>(StringComparer.OrdinalIgnoreCase)
            ];
            _positionTracker = new();
            _functions = new Dictionary<string, FunctionInfo>(StringComparer.OrdinalIgnoreCase);
        }

        public void EnterScope()
        {
            _scopeStack.Add(new Dictionary<string, VariableInfo>(StringComparer.OrdinalIgnoreCase));
        }

        public void ExitScope()
        {
            if (_scopeStack.Count > 1)
            {
                var leavingScope = _scopeStack.Last();

                CheckUnusedVariablesInScope(leavingScope);

                _scopeStack.RemoveAt(_scopeStack.Count - 1);
            }
        }

        public void DeclareVariable(string name, string varType, int line, int column)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            if (string.IsNullOrEmpty(varType))
            {
                _messageFormatter.ReportTypeError(
                    $"Не указан тип для переменной '{name}'",
                    line, column,
                    "Укажите тип переменной"
                );
                return;
            }

            var currentScope = _scopeStack.Last();

            if (currentScope.TryGetValue(name, out var existingVar))
            {
                _messageFormatter.ReportSemanticError(
                    $"Переменная '{name}' уже объявлена в данной области видимости (строка {existingVar.Line})",
                    line, column,
                    "Используйте другое имя или измените существующее объявление"
                );
                return;
            }

            if (!_builtInTypes.Contains(varType))
            {
                _messageFormatter.ReportTypeError(
                    $"Неизвестный тип '{varType}'",
                    line, column,
                    "Используйте один из стандартных типов: integer, real, boolean, string"
                );
                return;
            }

            currentScope[name] = new VariableInfo(name, varType, line, column);
        }

        public VariableInfo? UseVariable(string name, int line, int column)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            for (int i = _scopeStack.Count - 1; i >= 0; i--)
            {
                if (_scopeStack[i].TryGetValue(name, out var variable))
                {
                    variable.IsUsed = true;
                    return variable;
                }
            }
            _messageFormatter.ReportSemanticError(
                $"Переменная '{name}' не объявлена",
                line, column,
                "Объявите переменную перед использованием"
            );

            return null;
        }

        public void DeclareFunction(string name, List<Parameter> parameters, string? returnType, int line, int column)
        {
            if (string.IsNullOrEmpty(name))
                return;

            if (_functions.TryGetValue(name, out var existingFunc))
            {
                _messageFormatter.ReportSemanticError(
                    $"{(existingFunc.IsProcedure ? "Процедура" : "Функция")} '{name}' уже объявлена (строка {existingFunc.Line})",
                    line, column,
                    "Используйте другое имя или измените существующее объявление"
                );
                return;
            }

            if (returnType != null && !_builtInTypes.Contains(returnType))
            {
                _messageFormatter.ReportTypeError(
                    $"Неизвестный тип возвращаемого значения '{returnType}'",
                    line, column,
                    "Используйте один из стандартных типов: integer, real, boolean, string"
                );
                return;
            }

            var parameterInfos = from p in parameters select new ParameterInfo(p.Name, p.TypeName);

            foreach (var param in parameterInfos)
            {
                if (!_builtInTypes.Contains(param.Type))
                {
                    _messageFormatter.ReportTypeError(
                        $"Неизвестный тип параметра '{param.Type}' в {(returnType != null ? "функции" : "процедуре")} '{name}'",
                        line, column,
                        "Используйте один из стандартных типов: integer, real, boolean, string"
                    );
                }
            }

            _functions[name] = new FunctionInfo(name, [.. parameterInfos], returnType, line, column);
        }

        public FunctionInfo? UseFunction(string name, List<string> argumentTypes, int line, int column)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            if (!_functions.TryGetValue(name, out var function))
            {
                _messageFormatter.ReportSemanticError(
                    $"Функция или процедура '{name}' не объявлена",
                    line, column,
                    "Объявите функцию/процедуру перед использованием"
                );
                return null;
            }

            function.IsUsed = true;

            if (argumentTypes.Count != function.Parameters.Count)
            {
                _messageFormatter.ReportSemanticError(
                    $"Неверное количество аргументов для {(function.IsProcedure ? "процедуры" : "функции")} '{name}': ожидается {function.Parameters.Count}, передано {argumentTypes.Count}",
                    line, column,
                    "Проверьте количество передаваемых аргументов"
                );
                return function;
            }

            for (int i = 0; i < argumentTypes.Count; i++)
            {
                if (!AreTypesCompatible(argumentTypes[i], function.Parameters[i].Type))
                {
                    _messageFormatter.ReportTypeError(
                        $"Несовместимый тип аргумента {i + 1} в вызове {(function.IsProcedure ? "процедуры" : "функции")} '{name}': ожидается '{function.Parameters[i].Type}', передан '{argumentTypes[i]}'",
                        line, column,
                        "Убедитесь в совместимости типов аргументов"
                    );
                }
            }

            return function;
        }

        public bool AreTypesCompatible(string type1, string type2)
        {
            if (string.IsNullOrEmpty(type1) || string.IsNullOrEmpty(type2))
                return false;

            type1 = type1.ToLower();
            type2 = type2.ToLower();

            if (type1 == type2)
                return true;

            if (type1 == "integer" && type2 == "real")
                return true;

            return false;
        }

        public void CheckAssignment(string variableName, string expressionType, int line, int column)
        {
            var variable = UseVariable(variableName, line, column);
            if (variable == null)
                return;

            if (!AreTypesCompatible(expressionType, variable.Type))
            {
                _messageFormatter.ReportTypeError(
                    $"Несовместимые типы: невозможно присвоить значение типа '{expressionType}' переменной '{variableName}' типа '{variable.Type}'",
                    line, column,
                    "Убедитесь в совместимости типов или выполните явное преобразование"
                );
            }
        }

        public string? CheckBinaryOperation(string leftType, string operator_, string rightType, int line, int column)
        {
            if (string.IsNullOrEmpty(leftType) || string.IsNullOrEmpty(rightType))
                return null;

            leftType = leftType.ToLower();
            rightType = rightType.ToLower();

            switch (operator_)
            {
                case "+":
                case "-":
                case "*":
                case "/":
                    return CheckArithmeticOperation(leftType, rightType, operator_, line, column);

                case "=":
                case "<>":
                case "<":
                case "<=":
                case ">":
                case ">=":
                    return CheckComparisonOperation(leftType, rightType, operator_, line, column);

                case "and":
                case "or":
                    return CheckLogicalOperation(leftType, rightType, operator_, line, column);

                case "mod":
                    return CheckModOperation(leftType, rightType, line, column);

                default:
                    _messageFormatter.ReportSemanticError(
                        $"Неизвестная операция: {operator_}",
                        line, column
                    );
                    return null;
            }
        }

        private string? CheckArithmeticOperation(string leftType, string rightType, string operator_, int line, int column)
        {
            var leftIsNumeric = leftType == "integer" || leftType == "real";
            var rightIsNumeric = rightType == "integer" || rightType == "real";

            if (!leftIsNumeric || !rightIsNumeric)
            {
                _messageFormatter.ReportTypeError(
                    $"Арифметическая операция '{operator_}' неприменима к типам '{leftType}' и '{rightType}'",
                    line, column,
                    "Используйте числовые типы (integer или real)"
                );
                return null;
            }

            if (leftType == "real" || rightType == "real")
                return "real";

            return "integer";
        }

        private string? CheckComparisonOperation(string leftType, string rightType, string operator_, int line, int column)
        {
            if (!AreTypesCompatible(leftType, rightType) && !AreTypesCompatible(rightType, leftType))
            {
                _messageFormatter.ReportTypeError(
                    $"Сравнение '{operator_}' неприменимо к типам '{leftType}' и '{rightType}'",
                    line, column,
                    "Сравнивайте значения совместимых типов"
                );
                return null;
            }

            return "boolean";
        }

        private string? CheckLogicalOperation(string leftType, string rightType, string operator_, int line, int column)
        {
            if (leftType != "boolean" || rightType != "boolean")
            {
                _messageFormatter.ReportTypeError(
                    $"Логическая операция '{operator_}' применима только к типу boolean",
                    line, column,
                    "Используйте логические выражения"
                );
                return null;
            }

            return "boolean";
        }

        private string? CheckModOperation(string leftType, string rightType, int line, int column)
        {
            if (leftType != "integer" || rightType != "integer")
            {
                _messageFormatter.ReportTypeError(
                    "Операция 'mod' применима только к целым числам",
                    line, column,
                    "Используйте целочисленные операнды"
                );
                return null;
            }

            return "integer";
        }

        public string? CheckUnaryOperation(string operator_, string operandType, int line, int column)
        {
            if (string.IsNullOrEmpty(operandType))
                return null;

            operandType = operandType.ToLower();

            switch (operator_)
            {
                case "+":
                case "-":
                    if (operandType != "integer" && operandType != "real")
                    {
                        _messageFormatter.ReportTypeError(
                            $"Унарная операция '{operator_}' неприменима к типу '{operandType}'",
                            line, column,
                            "Используйте числовой тип"
                        );
                        return null;
                    }
                    return operandType;

                case "not":
                    if (operandType != "boolean")
                    {
                        _messageFormatter.ReportTypeError(
                            "Операция 'not' применима только к типу boolean",
                            line, column,
                            "Используйте логическое выражение"
                        );
                        return null;
                    }
                    return "boolean";

                default:
                    _messageFormatter.ReportSemanticError(
                        $"Неизвестная унарная операция: {operator_}",
                        line, column
                    );
                    return null;
            }
        }

        public void AnalyzeProgram(ExecutionNode programNode, PositionTracker tracker)
        {
            if (programNode == null)
                return;

            _positionTracker = tracker;

            foreach (var declaration in programNode.Declarations)
            {
                var position = tracker.GetPosition(declaration);
                AnalyzeDeclaration(declaration, position.Line, position.Column);
            }

            if (programNode.Statement != null)
            {
                AnalyzeStatement(programNode.Statement);
            }

            CheckUnusedVariablesInScope(_scopeStack.First());

            CheckUnusedFunctions();
        }

        private void AnalyzeDeclaration(IDeclaration declaration, int line, int column)
        {
            if (declaration is VariableDeclaration varDecl)
            {
                foreach (var varName in varDecl.Identifiers)
                {
                    DeclareVariable(varName, varDecl.TypeName, line, column);
                }
            }
            else if (declaration is FunctionDeclaration funcDecl)
            {
                DeclareFunction(funcDecl.Name, funcDecl.Parameters, funcDecl.ReturnType, line, column);

                EnterScope();

                try
                {
                    foreach (var param in funcDecl.Parameters)
                    {
                        DeclareVariable(param.Name, param.TypeName, line, column);
                    }

                    foreach (var localDecl in funcDecl.LocalDeclarations)
                    {
                        AnalyzeDeclaration(localDecl, line, column);
                    }

                    AnalyzeStatement(funcDecl.Body);
                }
                finally
                {
                    ExitScope();
                }
            }
        }

        private void AnalyzeStatement(IStatement statement)
        {
            switch (statement)
            {
                case AssignmentStatement assignment:
                    AnalyzeAssignment(assignment);
                    break;
                case CompoundStatement compound:
                    AnalyzeCompoundStatement(compound);
                    break;
                case ConditionStatement condition:
                    AnalyzeConditionStatement(condition);
                    break;
                case CycleStatement cycle:
                    AnalyzeCycleStatement(cycle);
                    break;
                case ProcedureCallStatement procedureCall:
                    AnalyzeProcedureCall(procedureCall);
                    break;
            }
        }

        private void AnalyzeAssignment(AssignmentStatement assignment)
        {
            var expressionType = AnalyzeExpression(assignment.Expression);
            if (expressionType != null)
            {
                var position = _positionTracker.GetPosition(assignment);
                CheckAssignment(assignment.Variable, expressionType, position.Line, position.Column);
            }
        }

        private void AnalyzeCompoundStatement(CompoundStatement compound)
        {
            EnterScope();
            foreach (var statement in compound.Statements)
                AnalyzeStatement(statement);
            ExitScope();
        }

        private void AnalyzeConditionStatement(ConditionStatement condition)
        {
            var conditionType = AnalyzeExpression(condition.Condition);
            if (conditionType != null && conditionType != "boolean")
            {
                var position = _positionTracker.GetPosition(condition);
                _messageFormatter.ReportTypeError(
                    $"Условие должно иметь тип boolean, получен {conditionType}",
                    position.Line, position.Column,
                    "Используйте логическое выражение в условии"
                );
            }

            AnalyzeStatement(condition.ThenStatement);

            if (condition.ElseStatement != null)
            {
                AnalyzeStatement(condition.ElseStatement);
            }
        }

        private void AnalyzeCycleStatement(CycleStatement cycle)
        {
            var conditionType = AnalyzeExpression(cycle.Condition);
            if (conditionType != null && conditionType != "boolean")
            {
                var position = _positionTracker.GetPosition(cycle);
                _messageFormatter.ReportTypeError(
                    $"Условие цикла должно иметь тип boolean, получен {conditionType}",
                    position.Line, position.Column,
                    "Используйте логическое выражение в условии цикла"
                );
            }
            AnalyzeStatement(cycle.Statement);
        }

        private void AnalyzeProcedureCall(ProcedureCallStatement procedureCall)
        {
            List<string> argumentTypes = [];
            foreach (var argument in procedureCall.Arguments)
            {
                var argType = AnalyzeExpression(argument);
                if (argType != null)
                {
                    argumentTypes.Add(argType);
                }
            }

            var position = _positionTracker.GetPosition(procedureCall);
            var function = UseFunction(procedureCall.Name, argumentTypes, position.Line, position.Column);

            if (function != null && !function.IsProcedure)
            {
                _messageFormatter.ReportSemanticError(
                    $"'{procedureCall.Name}' является функцией, а не процедурой. Используйте её в выражении",
                    position.Line, position.Column,
                    "Функции должны использоваться в выражениях, а процедуры - как отдельные операторы"
                );
            }
        }

        private string? AnalyzeExpression(IExpression expression)
        {
            var position = _positionTracker.GetPosition(expression);
            return expression switch
            {
                Identifier identifier => AnalyzeIdentifier(identifier, position.Line, position.Column),
                IntegerLiteral _ => "integer",
                RealLiteral _ => "real",
                StringLiteral _ => "string",
                BinaryOperation binary => AnalyzeBinaryOperation(binary, position.Line, position.Column),
                UnaryOperation unary => AnalyzeUnaryOperation(unary, position.Line, position.Column),
                FunctionCall functionCall => AnalyzeFunctionCall(functionCall, position.Line, position.Column),
                _ => null
            };
        }

        private string? AnalyzeIdentifier(Identifier identifier, int line, int column)
        {
            var variable = UseVariable(identifier.Name, line, column);
            return variable?.Type;
        }

        private string? AnalyzeBinaryOperation(BinaryOperation binary, int line, int column)
        {
            var leftType = AnalyzeExpression(binary.Left);
            var rightType = AnalyzeExpression(binary.Right);

            if (leftType != null && rightType != null)
            {
                return CheckBinaryOperation(leftType, binary.Operator, rightType, line, column);
            }

            return null;
        }

        private string? AnalyzeUnaryOperation(UnaryOperation unary, int line, int column)
        {
            var operandType = AnalyzeExpression(unary.Operand);

            if (operandType != null)
            {
                return CheckUnaryOperation(unary.Operator, operandType, line, column);
            }

            return null;
        }
        private string? AnalyzeFunctionCall(FunctionCall functionCall, int line, int column)
        {
            List<string> argumentTypes = [];
            foreach (var argument in functionCall.Arguments)
            {
                var argType = AnalyzeExpression(argument);
                if (argType != null)
                {
                    argumentTypes.Add(argType);
                }
            }

            return UseFunction(functionCall.Name, argumentTypes, line, column)?.ReturnType;
        }

        private void CheckUnusedVariablesInScope(Dictionary<string, VariableInfo> scope)
        {
            foreach (var kvp in scope)
            {
                var variable = kvp.Value;
                if (!variable.IsUsed)
                {
                    _messageFormatter.ReportWarning(
                        $"Переменная '{variable.Name}' объявлена, но не используется",
                        variable.Line,
                        variable.Column,
                        "Удалите неиспользуемую переменную или используйте её в коде"
                    );
                }
            }
        }

        private void CheckUnusedFunctions()
        {
            foreach (var kvp in _functions)
            {
                var function = kvp.Value;
                if (!function.IsUsed)
                {
                    _messageFormatter.ReportWarning(
                        $"{(function.IsProcedure ? "Процедура" : "Функция")} '{function.Name}' объявлена, но не используется",
                        function.Line,
                        function.Column,
                        $"Удалите неиспользуемую {(function.IsProcedure ? "процедуру" : "функцию")} или используйте её в коде"
                    );
                }
            }
        }
    }

    public class ParameterInfo
    {
        public string Name { get; set; }

        public string Type { get; set; }

        public ParameterInfo(string name, string type)
        {
            Name = name;
            Type = type;
        }
    }

    public class FunctionInfo
    {
        public string Name { get; set; }

        public List<ParameterInfo> Parameters { get; set; }

        public string? ReturnType { get; set; }

        public int Line { get; set; }

        public int Column { get; set; }

        public bool IsUsed { get; set; }

        public bool IsDeclared { get; set; }

        public bool IsProcedure => ReturnType == null;

        public FunctionInfo(string name, List<ParameterInfo> parameters, string? returnType, int line, int column)
        {
            Name = name;
            Parameters = parameters;
            ReturnType = returnType;
            Line = line;
            Column = column;
            IsUsed = false;
            IsDeclared = true;
        }
    }

    public class VariableInfo
    {
        public string Name { get; set; }

        public string Type { get; set; }

        public int Line { get; set; }

        public int Column { get; set; }

        public bool IsUsed { get; set; }

        public bool IsDeclared { get; set; }

        public object? Value { get; set; }

        public VariableInfo(string name, string type, int line, int column)
        {
            Name = name;
            Type = type;
            Line = line;
            Column = column;
            IsUsed = false;
            IsDeclared = true;
        }
    }
}
