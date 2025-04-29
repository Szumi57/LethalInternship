using System.Collections.Generic;

namespace LethalInternship.BehaviorTree
{
    public interface IPrintableNode
    {
        List<IBehaviourTreeNode> Children { get; }

        string Name { get; }

        string NodeType { get; }

        string NodeTypeSign { get; }
    }
}
