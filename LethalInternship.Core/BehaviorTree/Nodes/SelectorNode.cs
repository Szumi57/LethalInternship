using System.Collections.Generic;

namespace LethalInternship.Core.BehaviorTree.Nodes
{
    /// <summary>
    /// Selects the first node that succeeds. Tries successive nodes until it finds one that doesn't fail.
    /// </summary>
    public class SelectorNode : IParentBehaviourTreeNode, IPrintableNode
    {
        /// <summary>
        /// The name of the node.
        /// </summary>
        private string name;

        /// <summary>
        /// List of child nodes.
        /// </summary>
        private List<IBehaviourTreeNode> children = new List<IBehaviourTreeNode>();

        public List<IPrintableNode> PrintableChildren
        {
            get
            {
                var list = new List<IPrintableNode>();
                foreach (var child in children)
                {
                    if (child is IPrintableNode)
                    {
                        list.Add((IPrintableNode)child);
                    }
                }

                return list;
            }
        }
        public string Name { get { return name; } }
        public string NodeType { get { return "select"; } }
        public string NodeTypeSign { get { return "?"; } }

        public SelectorNode(string name)
        {
            this.name = name;
        }

        public BehaviourTreeStatus Tick(TimeData time)
        {
            foreach (var child in children)
            {
                var childStatus = child.Tick(time);
                if (childStatus != BehaviourTreeStatus.Failure)
                {
                    return childStatus;
                }
            }

            return BehaviourTreeStatus.Failure;
        }

        /// <summary>
        /// Add a child node to the selector.
        /// </summary>
        public void AddChild(IBehaviourTreeNode child)
        {
            children.Add(child);
        }
    }
}
