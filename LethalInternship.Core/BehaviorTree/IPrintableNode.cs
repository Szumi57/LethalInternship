using System.Collections.Generic;

namespace LethalInternship.Core.BehaviorTree
{
    public interface IPrintableNode
    {
        IReadOnlyList<IPrintableNode> PrintableChildren { get; }

        string Name { get; }

        string NodeType { get; }

        string NodeTypeSign { get; }
    }
}
