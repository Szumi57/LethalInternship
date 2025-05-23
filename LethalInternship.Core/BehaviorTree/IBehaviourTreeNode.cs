namespace LethalInternship.Core.BehaviorTree
{
    /// <summary>
    /// Interface for behaviour tree nodes.
    /// </summary>
    public interface IBehaviourTreeNode// : IPrintableNode
    {
        /// <summary>
        /// Update the time of the behaviour tree.
        /// </summary>
        BehaviourTreeStatus Tick(TimeData time);
    }
}
