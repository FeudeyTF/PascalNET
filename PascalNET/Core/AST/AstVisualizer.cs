using PascalNET.Core.AST.BasicNodes;
using PascalNET.Core.AST.Declartions;
using PascalNET.Core.AST.Expressions;
using PascalNET.Core.AST.Nodes;
using PascalNET.Core.AST.Statements;
using System.Text;

namespace PascalNET.Core.AST
{
    internal class AstVisualizer
    {
        private readonly StringBuilder _output;

        private const string BranchLine = "├─";

        private const string LastBranchLine = "└─";

        private const string Spacer = "  ";

        private const string VerticalSpacer = "│ ";

        public AstVisualizer()
        {
            _output = new StringBuilder();
        }

        public string VisualizeProgram(ExecutionNode program)
        {
            _output.Clear();


            if (program.ProgramName != null)
            {
                _output.AppendLine($"Программа: {program.ProgramName.Name}");
                _output.AppendLine();
            }

            _output.AppendLine("ExecutionNode (Корень программы)");

            var hasDeclarations = program.Declarations.Any();
            var hasStatements = program.Statement != null;

            if (hasDeclarations)
            {
                var prefix = hasStatements ? BranchLine : LastBranchLine;
                _output.AppendLine($"{prefix} Declarations ({program.Declarations.Count})");

                for (int i = 0; i < program.Declarations.Count; i++)
                {
                    var isLast = i == program.Declarations.Count - 1;
                    var declarationPrefix = hasStatements ? VerticalSpacer : Spacer;
                    VisualizeDeclaration(program.Declarations[i], declarationPrefix, isLast);
                }
            }

            if (program.Statement != null)
            {
                _output.AppendLine($"{LastBranchLine}  Statements");
                VisualizeStatement(program.Statement, Spacer, true);
            }

            return _output.ToString();
        }

        private void VisualizeDeclaration(IDeclaration declaration, string prefix, bool isLast)
        {
            var branchSymbol = isLast ? LastBranchLine : BranchLine;

            switch (declaration)
            {
                case VariableDeclaration varDecl:
                    _output.AppendLine($"{prefix}{branchSymbol}  VariableDeclaration");
                    var nextPrefix = prefix + (isLast ? Spacer : VerticalSpacer);
                    _output.AppendLine($"{nextPrefix}├─ Identifiers: [{string.Join(", ", varDecl.Identifiers)}]");
                    _output.AppendLine($"{nextPrefix}└─ Type: {varDecl.TypeName}");
                    break;

                case FunctionDeclaration funcDecl:
                    _output.AppendLine($"{prefix}{branchSymbol} FunctionDeclaration: {funcDecl.Name}");
                    break;

                default:
                    _output.AppendLine($"{prefix}{branchSymbol} {declaration.GetType().Name}");
                    break;
            }
        }

        private void VisualizeStatement(IStatement statement, string prefix, bool isLast)
        {
            var branchSymbol = isLast ? LastBranchLine : BranchLine;

            switch (statement)
            {
                case CompoundStatement compound:
                    _output.AppendLine($"{prefix}{branchSymbol} CompoundStatement ({compound.Statements.Count})");
                    var nextPrefix = prefix + (isLast ? Spacer : VerticalSpacer);

                    for (int i = 0; i < compound.Statements.Count; i++)
                    {
                        var isLastStmt = i == compound.Statements.Count - 1;
                        VisualizeStatement(compound.Statements[i], nextPrefix, isLastStmt);
                    }
                    break;

                case AssignmentStatement assignment:
                    _output.AppendLine($"{prefix}{branchSymbol}  AssignmentStatement");
                    var assignPrefix = prefix + (isLast ? Spacer : VerticalSpacer);
                    _output.AppendLine($"{assignPrefix}├─ Variable: {assignment.Variable}");
                    _output.AppendLine($"{assignPrefix}└─ Expression:");
                    VisualizeExpression(assignment.Expression, assignPrefix + "   ", true);
                    break;

                case ConditionStatement condition:
                    _output.AppendLine($"{prefix}{branchSymbol} ConditionStatement (if-then-else)");
                    var condPrefix = prefix + (isLast ? Spacer : VerticalSpacer);

                    _output.AppendLine($"{condPrefix}├─ Condition:");
                    VisualizeExpression(condition.Condition, condPrefix + "│  ", true);

                    _output.AppendLine($"{condPrefix}├─ Then:");
                    VisualizeStatement(condition.ThenStatement, condPrefix + "│  ", true);

                    if (condition.ElseStatement != null)
                    {
                        _output.AppendLine($"{condPrefix}└─ Else:");
                        VisualizeStatement(condition.ElseStatement, condPrefix + "   ", true);
                    }
                    else
                    {
                        _output.AppendLine($"{condPrefix}└─ Else: (отсутствует)");
                    }
                    break;

                case CycleStatement cycle:
                    _output.AppendLine($"{prefix}{branchSymbol} CycleStatement (while)");
                    var cyclePrefix = prefix + (isLast ? Spacer : VerticalSpacer);

                    _output.AppendLine($"{cyclePrefix}├─ Condition:");
                    VisualizeExpression(cycle.Condition, cyclePrefix + "│  ", true);

                    _output.AppendLine($"{cyclePrefix}└─ Body:");
                    VisualizeStatement(cycle.Statement, cyclePrefix + "   ", true);
                    break;
                case ProcedureCallStatement procCall:
                    _output.AppendLine($"{prefix}{branchSymbol} ProcedureCallStatement");
                    var procPrefix = prefix + (isLast ? Spacer : VerticalSpacer);
                    _output.AppendLine($"{procPrefix}├─ Procedure: {procCall.Name}");
                    if (procCall.Arguments.Any())
                    {
                        _output.AppendLine($"{procPrefix}└─ Arguments ({procCall.Arguments.Count}):");
                        for (int i = 0; i < procCall.Arguments.Count; i++)
                        {
                            var isLastArg = i == procCall.Arguments.Count - 1;
                            VisualizeExpression(procCall.Arguments[i], procPrefix + "   ", isLastArg);
                        }
                    }
                    else
                    {
                        _output.AppendLine($"{procPrefix}└─ Arguments: (отсутствуют)");
                    }
                    break;

                default:
                    _output.AppendLine($"{prefix}{branchSymbol} {statement.GetType().Name}");
                    break;
            }
        }

        private void VisualizeExpression(IExpression expression, string prefix, bool isLast)
        {
            var branchSymbol = isLast ? LastBranchLine : BranchLine;

            switch (expression)
            {
                case BinaryOperation binary:
                    _output.AppendLine($"{prefix}{branchSymbol} BinaryOperation ({binary.Operator})");
                    var binPrefix = prefix + (isLast ? Spacer : VerticalSpacer);

                    _output.AppendLine($"{binPrefix}├─  Left:");
                    VisualizeExpression(binary.Left, binPrefix + "│  ", true);

                    _output.AppendLine($"{binPrefix}└─  Right:");
                    VisualizeExpression(binary.Right, binPrefix + "   ", true);
                    break;

                case UnaryOperation unary:
                    _output.AppendLine($"{prefix}{branchSymbol} UnaryOperation ({unary.Operator})");
                    var unaryPrefix = prefix + (isLast ? Spacer : VerticalSpacer);
                    _output.AppendLine($"{unaryPrefix}└─ Operand:");
                    VisualizeExpression(unary.Operand, unaryPrefix + "   ", true);
                    break;

                case Identifier identifier:
                    _output.AppendLine($"{prefix}{branchSymbol}  Identifier: '{identifier.Name}'");
                    break;

                case IntegerLiteral intLiteral:
                    _output.AppendLine($"{prefix}{branchSymbol} IntegerLiteral: {intLiteral.Value}");
                    break;

                case RealLiteral realLiteral:
                    _output.AppendLine($"{prefix}{branchSymbol} RealLiteral: {realLiteral.Value}");
                    break;

                case StringLiteral stringLiteral:
                    _output.AppendLine($"{prefix}{branchSymbol} StringLiteral: \"{stringLiteral.Value}\"");
                    break;
                case FunctionCall funcCall:
                    _output.AppendLine($"{prefix}{branchSymbol} FunctionCall: {funcCall.Name}");
                    var funcPrefix = prefix + (isLast ? Spacer : VerticalSpacer);
                    if (funcCall.Arguments.Any())
                    {
                        _output.AppendLine($"{funcPrefix}└─ Arguments ({funcCall.Arguments.Count}):");
                        for (int i = 0; i < funcCall.Arguments.Count; i++)
                        {
                            var isLastArg = i == funcCall.Arguments.Count - 1;
                            VisualizeExpression(funcCall.Arguments[i], funcPrefix + "   ", isLastArg);
                        }
                    }
                    else
                    {
                        _output.AppendLine($"{funcPrefix}└─ Arguments: (отсутствуют)");
                    }
                    break;

                default:
                    _output.AppendLine($"{prefix}{branchSymbol} {expression.GetType().Name}");
                    break;
            }
        }

        public string CreateSummary(ExecutionNode program)
        {
            var summary = new StringBuilder();

            summary.AppendLine("AST:");
            summary.AppendLine($"   • Объявлений: {program.Declarations.Count}");

            var varDeclarations = program.Declarations.OfType<VariableDeclaration>().Count();
            var funcDeclarations = program.Declarations.OfType<FunctionDeclaration>().Count();

            if (varDeclarations > 0)
                summary.AppendLine($"     - Переменные: {varDeclarations}");
            if (funcDeclarations > 0)
                summary.AppendLine($"     - Функции: {funcDeclarations}");

            var statementCount = CountStatements(program.Statement);
            summary.AppendLine($"    Операторов: {statementCount}");

            return summary.ToString();
        }

        private int CountStatements(IStatement statement)
        {
            return statement switch
            {
                CompoundStatement compound => compound.Statements.Sum(CountStatements),
                _ => 1
            };
        }
    }
}
