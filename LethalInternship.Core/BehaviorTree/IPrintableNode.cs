using System.Collections.Generic;

namespace LethalInternship.Core.BehaviorTree
{
    public interface IPrintableNode
    {
        List<IPrintableNode> PrintableChildren { get; }

        string Name { get; }

        string NodeType { get; }

        string NodeTypeSign { get; }
    }
}
