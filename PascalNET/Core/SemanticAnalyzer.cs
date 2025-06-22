using PascalNET.Compiler;
using PascalNET.Core.AST;
using PascalNET.Core.AST.BasicNodes;
using PascalNET.Core.AST.Declartions;
using PascalNET.Core.AST.Expressions;
using PascalNET.Core.AST.Nodes;
using PascalNET.Core.AST.Statements;

namespace PascalNET.Core
{
    internal class SemanticAnalyzer
    {
        private readonly List<Dictionary<string, VariableInfo>> _scopeStack;

        private readonly ErrorReporter _errorReporter;

        private readonly HashSet<string> _builtInTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "integer", "real", "boolean", "string"
        };

        public SemanticAnalyzer(ErrorReporter errorReporter)
        {
            _errorReporter = errorReporter;
            _scopeStack =
            [
                new Dictionary<string, VariableInfo>(StringComparer.OrdinalIgnoreCase)
            ];
        }

        /// <summary>
        /// Входит в новую область видимости
        /// </summary>
        public void EnterScope()
        {
            _scopeStack.Add(new Dictionary<string, VariableInfo>(StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Выходит из текущей области видимости
        /// </summary>
        public void ExitScope()
        {
            if (_scopeStack.Count > 1)
            {
                var leavingScope = _scopeStack.Last();

                // Проверяем неиспользуемые переменные в покидаемой области
                CheckUnusedVariablesInScope(leavingScope);

                _scopeStack.RemoveAt(_scopeStack.Count - 1);
            }
        }

        /// <summary>
        /// Объявляет переменную в текущей области видимости
        /// </summary>
        public void DeclareVariable(string name, string varType, int line, int column)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            if (string.IsNullOrEmpty(varType))
            {
                _errorReporter.ReportTypeError(
                    $"Не указан тип для переменной '{name}'",
                    line, column,
                    "Укажите тип переменной"
                );
                return;
            }

            var currentScope = _scopeStack.Last();

            // Проверяем, не объявлена ли переменная в текущей области видимости
            if (currentScope.ContainsKey(name))
            {
                var existingVar = currentScope[name];
                _errorReporter.ReportSemanticError(
                    $"Переменная '{name}' уже объявлена в данной области видимости (строка {existingVar.Line})",
                    line, column,
                    "Используйте другое имя или измените существующее объявление"
                );
                return;
            }

            // Проверяем корректность типа
            if (!_builtInTypes.Contains(varType))
            {
                _errorReporter.ReportTypeError(
                    $"Неизвестный тип '{varType}'",
                    line, column,
                    "Используйте один из стандартных типов: integer, real, boolean, string"
                );
                return;
            }

            // Добавляем переменную в область видимости
            currentScope[name] = new VariableInfo
            {
                Name = name,
                Type = varType,
                Line = line,
                Column = column,
                IsUsed = false,
                IsDeclared = true
            };
        }

        /// <summary>
        /// Использует переменную и проверяет её объявление
        /// </summary>
        public VariableInfo? UseVariable(string name, int line, int column)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            // Ищем переменную во всех областях видимости (от внутренней к внешней)
            for (int i = _scopeStack.Count - 1; i >= 0; i--)
            {
                if (_scopeStack[i].TryGetValue(name, out var variable))
                {
                    variable.IsUsed = true;
                    return variable;
                }
            }

            // Переменная не найдена
            _errorReporter.ReportSemanticError(
                $"Переменная '{name}' не объявлена",
                line, column,
                "Объявите переменную перед использованием"
            );

            return null;
        }

        /// <summary>
        /// Проверяет совместимость типов
        /// </summary>
        public bool AreTypesCompatible(string type1, string type2)
        {
            if (string.IsNullOrEmpty(type1) || string.IsNullOrEmpty(type2))
                return false;

            type1 = type1.ToLower();
            type2 = type2.ToLower();

            // Точное совпадение
            if (type1 == type2)
                return true;

            // integer может быть присвоен real
            if (type1 == "integer" && type2 == "real")
                return true;

            return false;
        }

        /// <summary>
        /// Проверяет типы в выражении присваивания
        /// </summary>
        public void CheckAssignment(string variableName, string expressionType, int line, int column)
        {
            var variable = UseVariable(variableName, line, column);
            if (variable == null)
                return;

            if (!AreTypesCompatible(expressionType, variable.Type))
            {
                _errorReporter.ReportTypeError(
                    $"Несовместимые типы: невозможно присвоить значение типа '{expressionType}' переменной '{variableName}' типа '{variable.Type}'",
                    line, column,
                    "Убедитесь в совместимости типов или выполните явное преобразование"
                );
            }
        }

        /// <summary>
        /// Проверяет типы в бинарной операции
        /// </summary>
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
                    _errorReporter.ReportSemanticError(
                        $"Неизвестная операция: {operator_}",
                        line, column
                    );
                    return null;
            }
        }

        private string? CheckArithmeticOperation(string leftType, string rightType, string operator_, int line, int column)
        {
            // Арифметические операции допустимы для числовых типов
            bool leftIsNumeric = leftType == "integer" || leftType == "real";
            bool rightIsNumeric = rightType == "integer" || rightType == "real";

            if (!leftIsNumeric || !rightIsNumeric)
            {
                _errorReporter.ReportTypeError(
                    $"Арифметическая операция '{operator_}' неприменима к типам '{leftType}' и '{rightType}'",
                    line, column,
                    "Используйте числовые типы (integer или real)"
                );
                return null;
            }

            // Результат real если хотя бы один операнд real
            if (leftType == "real" || rightType == "real")
                return "real";

            return "integer";
        }

        private string? CheckComparisonOperation(string leftType, string rightType, string operator_, int line, int column)
        {
            // Сравнение возможно между совместимыми типами
            if (!AreTypesCompatible(leftType, rightType) && !AreTypesCompatible(rightType, leftType))
            {
                _errorReporter.ReportTypeError(
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
            // Логические операции применимы только к boolean
            if (leftType != "boolean" || rightType != "boolean")
            {
                _errorReporter.ReportTypeError(
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
            // Операция mod применима только к integer
            if (leftType != "integer" || rightType != "integer")
            {
                _errorReporter.ReportTypeError(
                    "Операция 'mod' применима только к целым числам",
                    line, column,
                    "Используйте целочисленные операнды"
                );
                return null;
            }

            return "integer";
        }

        /// <summary>
        /// Проверяет унарную операцию
        /// </summary>
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
                        _errorReporter.ReportTypeError(
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
                        _errorReporter.ReportTypeError(
                            "Операция 'not' применима только к типу boolean",
                            line, column,
                            "Используйте логическое выражение"
                        );
                        return null;
                    }
                    return "boolean";

                default:
                    _errorReporter.ReportSemanticError(
                        $"Неизвестная унарная операция: {operator_}",
                        line, column
                    );
                    return null;
            }
        }

        /// <summary>
        /// Анализирует дерево синтаксического анализа
        /// </summary>
        public void AnalyzeProgram(ExecutionNode programNode)
        {
            if (programNode == null) return;

            // Обрабатываем объявления переменных
            foreach (var declaration in programNode.Declarations)
            {
                AnalyzeDeclaration(declaration);
            }

            // Обрабатываем основной оператор программы
            if (programNode.Statement != null)
            {
                AnalyzeStatement(programNode.Statement);
            }

            // Проверяем неиспользуемые переменные в глобальной области
            CheckUnusedVariablesInScope(_scopeStack.First());
        }

        private void AnalyzeDeclaration(IDeclaration declaration)
        {
            if (declaration is VariableDeclaration varDecl)
            {
                foreach (var varName in varDecl.Identifiers)
                {
                    DeclareVariable(varName, varDecl.TypeName, 0, 0);
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
            }
        }

        private void AnalyzeAssignment(AssignmentStatement assignment)
        {
            var expressionType = AnalyzeExpression(assignment.Expression);
            if (expressionType != null)
            {
                CheckAssignment(assignment.Variable, expressionType, 0, 0);
            }
        }

        private void AnalyzeCompoundStatement(CompoundStatement compound)
        {
            EnterScope();

            foreach (var statement in compound.Statements)
            {
                AnalyzeStatement(statement);
            }

            ExitScope();
        }

        private void AnalyzeConditionStatement(ConditionStatement condition)
        {
            var conditionType = AnalyzeExpression(condition.Condition);
            if (conditionType != null && conditionType != "boolean")
            {
                _errorReporter.ReportTypeError(
                    $"Условие должно иметь тип boolean, получен {conditionType}",
                    0, 0,
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
                _errorReporter.ReportTypeError(
                    $"Условие цикла должно иметь тип boolean, получен {conditionType}",
                    0, 0,
                    "Используйте логическое выражение в условии цикла"
                );
            }

            AnalyzeStatement(cycle.Statement);
        }
        private string? AnalyzeExpression(IExpression expression)
        {
            return expression switch
            {
                Identifier identifier => AnalyzeIdentifier(identifier),
                IntegerLiteral _ => "integer",
                RealLiteral _ => "real",
                StringLiteral _ => "string",
                BinaryOperation binary => AnalyzeBinaryOperation(binary),
                UnaryOperation unary => AnalyzeUnaryOperation(unary),
                _ => null
            };
        }
        private string? AnalyzeIdentifier(Identifier identifier)
        {
            var variable = UseVariable(identifier.Name, 0, 0);
            return variable?.Type;
        }

        private string? AnalyzeBinaryOperation(BinaryOperation binary)
        {
            var leftType = AnalyzeExpression(binary.Left);
            var rightType = AnalyzeExpression(binary.Right);

            if (leftType != null && rightType != null)
            {
                return CheckBinaryOperation(leftType, binary.Operator, rightType, 0, 0);
            }

            return null;
        }

        private string? AnalyzeUnaryOperation(UnaryOperation unary)
        {
            var operandType = AnalyzeExpression(unary.Operand);

            if (operandType != null)
            {
                return CheckUnaryOperation(unary.Operator, operandType, 0, 0);
            }

            return null;
        }

        private void CheckUnusedVariablesInScope(Dictionary<string, VariableInfo> scope)
        {
            foreach (var kvp in scope)
            {
                var variable = kvp.Value;
                if (!variable.IsUsed)
                {
                    _errorReporter.ReportWarning(
                        $"Переменная '{variable.Name}' объявлена, но не используется",
                        variable.Line,
                        variable.Column,
                        "Удалите неиспользуемую переменную или используйте её в коде"
                    );
                }
            }
        }
    }

    /// <summary>
    /// Информация о переменной
    /// </summary>
    public class VariableInfo
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public int Line { get; set; }
        public int Column { get; set; }
        public bool IsUsed { get; set; }
        public bool IsDeclared { get; set; }
        public object? Value { get; set; }
    }
}
