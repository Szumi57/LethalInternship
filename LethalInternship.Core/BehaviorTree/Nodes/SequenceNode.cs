using System.Collections.Generic;

namespace LethalInternship.Core.BehaviorTree.Nodes
{
    /// <summary>
    /// Runs child nodes in sequence, until one fails.
    /// </summary>
    public class SequenceNode : IParentBehaviourTreeNode, IPrintableNode
    {
        /// <summary>
        /// Name of the node.
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
        public string NodeType { get { return "sequence"; } }
        public string NodeTypeSign { get { return "->"; } }

        public SequenceNode(string name)
        {
            this.name = name;
        }

        public BehaviourTreeStatus Tick(TimeData time)
        {
            foreach (var child in children)
            {
                var childStatus = child.Tick(time);
                if (childStatus != BehaviourTreeStatus.Success)
                {
                    return childStatus;
                }
            }

            return BehaviourTreeStatus.Success;
        }

        /// <summary>
        /// Add a child to the sequence.
        /// </summary>
        public void AddChild(IBehaviourTreeNode child)
        {
            children.Add(child);
        }
    }
}
