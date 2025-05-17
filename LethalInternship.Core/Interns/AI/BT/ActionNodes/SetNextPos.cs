using LethalInternship.Core.BehaviorTree;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class SetNextPos
    {
        public BehaviourTreeStatus Action(InternAI ai, Vector3 pos)
        {
            ai.NextPos = pos;
            return BehaviourTreeStatus.Success;
        }
    }
}
