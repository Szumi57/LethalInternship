using System;
using System.Collections.Generic;

namespace LethalInternship.BehaviorTree.Nodes
{
    /// <summary>
    /// Decorator node that inverts the success/failure of its child.
    /// </summary>
    public class InverterNode : IParentBehaviourTreeNode
    {
        /// <summary>
        /// Name of the node.
        /// </summary>
        private string name;

        /// <summary>
        /// The child to be inverted.
        /// </summary>
        private IBehaviourTreeNode childNode;

        List<IBehaviourTreeNode> IPrintableNode.Children { get { return new List<IBehaviourTreeNode>() { childNode }; } }
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
