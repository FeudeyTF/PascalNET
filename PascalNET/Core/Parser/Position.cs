namespace PascalNET.Core.Parser
{
    internal struct Position
    {
        public int Line { get; init; }

        public int Column { get; init; }

        public Position(int line, int column)
        {
            Line = line;
            Column = column;
        }
    }
}
