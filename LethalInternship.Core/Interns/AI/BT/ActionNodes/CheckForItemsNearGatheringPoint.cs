using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Managers;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class CheckForItemsNearGatheringPoint : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            context.TargetItem = FirstItemNearGatheringPoint();
            return BehaviourTreeStatus.Success;
        }

        private GrabbableObject? FirstItemNearGatheringPoint()
        {
            if (InternManager.Instance.GatheringPoint == null)
                return null;

            var grabbableObjectsList = InternManager.Instance.GetGrabbableObjectsList();
            for (int i = 0; i < grabbableObjectsList.Count; i++)
            {
                GameObject gameObject = grabbableObjectsList[i];
                if (gameObject == null)
                {
                    continue;
                }

                // Black listed ? 
                if (InternManager.Instance.IsGrabbableObjectBlackListed(gameObject))
                {
                    continue;
                }

                // Get grabbable object infos
                GrabbableObject? grabbableObject = gameObject.GetComponent<GrabbableObject>();
                if (grabbableObject == null)
                {
                    continue;
                }

                // Object not near gathering point
                Vector3 gatheringPointPos = InternManager.Instance.GatheringPoint.GetPoint();
                if ((gatheringPointPos - grabbableObject.transform.position).sqrMagnitude > 5f * 5f)
                {
                    continue; // not in cruiser
                }

                // Grabbable object ?
                if (!InternManager.Instance.IsGrabbableObjectGrabbable(grabbableObject, forcePickUp: true))
                {
                    continue;
                }

                return grabbableObject;
            }

            return null;
        }
    }
}
