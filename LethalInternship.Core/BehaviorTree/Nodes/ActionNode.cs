using System;
using System.Collections.Generic;

namespace LethalInternship.Core.BehaviorTree.Nodes
{
    /// <summary>
    /// A behaviour tree leaf node for running an action.
    /// </summary>
    public class ActionNode : IBehaviourTreeNode, IPrintableNode
    {
        /// <summary>
        /// The name of the node.
        /// </summary>
        private string name;

        /// <summary>
        /// Function to invoke for the action.
        /// </summary>
        private Func<TimeData, BehaviourTreeStatus> fn;

        public List<IPrintableNode> PrintableChildren { get { return new List<IPrintableNode>(); } }
        public string Name { get { return name; } }
        public string NodeType { get { return "action"; } }
        public string NodeTypeSign { get { return string.Empty; } }


        public ActionNode(string name, Func<TimeData, BehaviourTreeStatus> fn)
        {
            this.name=name;
            this.fn=fn;
        }

        public BehaviourTreeStatus Tick(TimeData time)
        {
            return fn(time);
        }
    }
}
