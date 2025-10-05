using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.CustomItemBehaviourLibraryHooks;
using LethalInternship.SharedAbstractions.Hooks.LethalMinHooks;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Parameters;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class IsObjectToGrab : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            InternAI ai = context.InternAI;

            // Check for object to grab
            if (!ai.AreHandsFree())
            {
                return false;
            }

            GrabbableObject? grabbableObject = LookingForObjectToGrab(ai);
            if (grabbableObject == null)
            {
                return false;
            }

            // Voice
            TryPlayCurrentStateVoiceAudio(ai);

            context.TargetItem = grabbableObject;
            return true;
        }

        /// <summary>
        /// Check all object array <c>HoarderBugAI.grabbableObjectsInMap</c>, 
        /// if intern is close and can see an item to grab.
        /// </summary>
        /// <returns><c>GrabbableObject</c> if intern sees an item he can grab, else null.</returns>
        private GrabbableObject? LookingForObjectToGrab(InternAI ai)
        {
            var grabbableObjectsList = InternManager.Instance.GetGrabbableObjectsList();
            for (int i = 0; i < grabbableObjectsList.Count; i++)
            {
                GameObject gameObject = grabbableObjectsList[i];
                if (gameObject == null)
                {
                    continue;
                }

                // Object not outside when ai inside and vice versa
                Vector3 gameObjectPosition = gameObject.transform.position;
                if (ai.isOutside && gameObjectPosition.y < -100f)
                {
                    continue;
                }
                else if (!ai.isOutside && gameObjectPosition.y > -80f)
                {
                    continue;
                }

                // Object in range ?
                float sqrDistanceEyeGameObject = (gameObjectPosition - ai.eye.position).sqrMagnitude;
                if (sqrDistanceEyeGameObject > Const.INTERN_OBJECT_RANGE * Const.INTERN_OBJECT_RANGE)
                {
                    continue;
                }

                // Black listed ? 
                if (IsGrabbableObjectBlackListed(gameObject))
                {
                    continue;
                }

                // Get grabbable object infos
                GrabbableObject? grabbableObject = gameObject.GetComponent<GrabbableObject>();
                if (grabbableObject == null)
                {
                    continue;
                }

                // Object on ship
                if (grabbableObject.isInElevator
                    || grabbableObject.isInShipRoom)
                {
                    continue;
                }

                // Object in cruiser vehicle
                if (grabbableObject.transform.parent != null
                    && grabbableObject.transform.parent.name.StartsWith("CompanyCruiser"))
                {
                    continue;
                }

                // Object in a container mod of some sort ?
                if (PluginRuntimeProvider.Context.IsModCustomItemBehaviourLibraryLoaded)
                {
                    if (CustomItemBehaviourLibraryHook.IsGrabbableObjectInContainerMod?.Invoke(grabbableObject) ?? false)
                    {
                        continue;
                    }
                }

                // Is a pickmin (LethalMin mod) holding the object ?
                if (PluginRuntimeProvider.Context.IsModLethalMinLoaded)
                {
                    if (LethalMinHook.IsGrabbableObjectHeldByPikminMod?.Invoke(grabbableObject) ?? false)
                    {
                        continue;
                    }
                }

                // Grabbable object ?
                if (!ai.IsGrabbableObjectGrabbable(grabbableObject))
                {
                    continue;
                }

                // Object close to awareness distance ?
                if (sqrDistanceEyeGameObject < Const.INTERN_OBJECT_AWARNESS * Const.INTERN_OBJECT_AWARNESS)
                {
                    PluginLoggerHook.LogDebug?.Invoke($"awareness {grabbableObject.name}");
                }
                // Object visible ?
                else if (!Physics.Linecast(ai.eye.position, gameObjectPosition, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
                {
                    Vector3 to = gameObjectPosition - ai.eye.position;
                    if (Vector3.Angle(ai.eye.forward, to) < Const.INTERN_FOV)
                    {
                        // Object in FOV
                        PluginLoggerHook.LogDebug?.Invoke($"LOS {grabbableObject.name}");
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    // Object not in line of sight
                    continue;
                }

                return grabbableObject;
            }

            return null;
        }
        private bool IsGrabbableObjectBlackListed(GameObject gameObjectToEvaluate)
        {
            // Bee nest
            if (!PluginRuntimeProvider.Context.Config.GrabBeesNest
                && gameObjectToEvaluate.name.Contains("RedLocustHive"))
            {
                return true;
            }

            // Dead bodies
            if (!PluginRuntimeProvider.Context.Config.GrabDeadBodies
                && gameObjectToEvaluate.name.Contains("RagdollGrabbableObject")
                && gameObjectToEvaluate.tag == "PhysicsProp"
                && gameObjectToEvaluate.GetComponentInParent<DeadBodyInfo>() != null)
            {
                return true;
            }

            // Maneater
            if (!PluginRuntimeProvider.Context.Config.GrabManeaterBaby
                && gameObjectToEvaluate.name.Contains("CaveDwellerEnemy"))
            {
                return true;
            }

            // Wheelbarrow
            if (!PluginRuntimeProvider.Context.Config.GrabWheelbarrow
                && gameObjectToEvaluate.name.Contains("Wheelbarrow"))
            {
                return true;
            }

            // ShoppingCart
            if (!PluginRuntimeProvider.Context.Config.GrabShoppingCart
                && gameObjectToEvaluate.name.Contains("ShoppingCart"))
            {
                return true;
            }

            return false;
        }

        private void TryPlayCurrentStateVoiceAudio(InternAI ai)
        {
            // Default states, wait for cooldown and if no one is talking close
            ai.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = EnumVoicesState.FoundLoot,
                CanTalkIfOtherInternTalk = true,
                WaitForCooldown = true,
                CutCurrentVoiceStateToTalk = true,
                CanRepeatVoiceState = false,

                ShouldSync = true,
                IsInternInside = ai.NpcController.Npc.isInsideFactory,
                AllowSwearing = PluginRuntimeProvider.Context.Config.AllowSwearing
            });
        }
    }
}
