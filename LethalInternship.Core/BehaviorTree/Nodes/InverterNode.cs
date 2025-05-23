using System;
using System.Collections.Generic;

namespace LethalInternship.Core.BehaviorTree.Nodes
{
    /// <summary>
    /// Decorator node that inverts the success/failure of its child.
    /// </summary>
    public class InverterNode : IParentBehaviourTreeNode, IPrintableNode
    {
        /// <summary>
        /// Name of the node.
        /// </summary>
        private string name;

        /// <summary>
        /// The child to be inverted.
        /// </summary>
        private IBehaviourTreeNode childNode;

        public List<IPrintableNode> PrintableChildren
        {
            get
            {
                if (childNode is IPrintableNode)
                {
                    return new List<IPrintableNode>() { (IPrintableNode)childNode };
                }

                return new List<IPrintableNode>();
            }
        }
        public string Name { get { return name; } }
        public string NodeType { get { return "inverter"; } }
        public string NodeTypeSign { get { return "<->"; } }

        public InverterNode(string name)
        {
            this.name = name;
        }

        public BehaviourTreeStatus Tick(TimeData time)
        {
            if (childNode == null)
            {
                throw new ApplicationException("InverterNode must have a child node!");
            }

            var result = childNode.Tick(time);
            if (result == BehaviourTreeStatus.Failure)
            {
                return BehaviourTreeStatus.Success;
            }
            else if (result == BehaviourTreeStatus.Success)
            {
                return BehaviourTreeStatus.Failure;
            }
            else
            {
                return result;
            }
        }

        /// <summary>
        /// Add a child to the parent node.
        /// </summary>
        public void AddChild(IBehaviourTreeNode child)
        {
            if (this.childNode != null)
            {
                throw new ApplicationException("Can't add more than a single child to InverterNode!");
            }

            this.childNode = child;
        }

        public List<IBehaviourTreeNode> Children()
        {
            return new List<IBehaviourTreeNode>() { childNode };
        }
    }
}
