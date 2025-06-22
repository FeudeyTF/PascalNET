using PascalNET.Core.AST;

namespace PascalNET.Core.Parser
{
    internal class PositionTracker
    {
        private readonly Dictionary<INode, Position> _trackingPositions;

        public PositionTracker()
        {
            _trackingPositions = [];
        }

        public void AddPosition(INode node, int line, int column)
        {
            _trackingPositions.TryAdd(node, new Position(line, column));
        }

        public Position GetPosition(INode node)
        {
            if (_trackingPositions.TryGetValue(node, out var position))
                return position;
            return default;
        }
    }
}
