using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Managers;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class CheckForItemsInCruiser : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            context.TargetItem = FirstItemInCruiser();
            return BehaviourTreeStatus.Success;
        }

        private GrabbableObject? FirstItemInCruiser()
        {
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

                // Object not in cruiser vehicle
                if (grabbableObject.transform.parent == null
                    || !grabbableObject.transform.parent.name.StartsWith("CompanyCruiser"))
                {
                    continue; // not in cruiser
                }

                if (grabbableObject.itemProperties.itemName.StartsWith("clipboard"))
                {
                    // black listed for unloading from cruiser
                    continue;
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
