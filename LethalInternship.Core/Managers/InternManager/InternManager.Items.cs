using GameNetcodeStuff;
using LethalInternship.Core.Interns.AI.TimedTasks;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.CustomItemBehaviourLibraryHooks;
using LethalInternship.SharedAbstractions.Hooks.LethalMinHooks;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalInternship.Core.Managers
{
    public partial class InternManager
    {
        #region Items global

        private TimedGetGrabbableObjectsList getGrabbableObjectsListTimed = null!;

        /// <summary>
        /// Dictionnary of the recently dropped object on the ground.
        /// The intern will not try to grab them for a certain time (<see cref="Const.WAIT_TIME_FOR_GRAB_DROPPED_OBJECTS"><c>Const.WAIT_TIME_FOR_GRAB_DROPPED_OBJECTS</c></see>).
        /// </summary>
        public Dictionary<GrabbableObject, float> DictJustDroppedItems = new Dictionary<GrabbableObject, float>();

        public void AddToDictJustDroppedItems(GrabbableObject grabbableObject)
        {
            DictJustDroppedItems[grabbableObject] = Time.realtimeSinceStartup;
        }

        public bool IsGrabbableObjectJustDropped(GrabbableObject grabbableObject)
        {
            if (DictJustDroppedItems.TryGetValue(grabbableObject, out float justDroppedItemTime))
            {
                if (Time.realtimeSinceStartup - justDroppedItemTime < Const.WAIT_TIME_FOR_GRAB_DROPPED_OBJECTS)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Trim dictionnary if too large, trim only the dropped item since a long time
        /// </summary>
        public void TrimDictJustDroppedItems()
        {
            if (DictJustDroppedItems != null && DictJustDroppedItems.Count > 20)
            {
                PluginLoggerHook.LogDebug?.Invoke($"TrimDictJustDroppedItems Count{DictJustDroppedItems.Count}");
                var itemsToClean = DictJustDroppedItems.Where(x => Time.realtimeSinceStartup - x.Value > Const.WAIT_TIME_FOR_GRAB_DROPPED_OBJECTS)
                                                       .Select(x => x.Key)
                                                       .ToList();
                foreach (var item in itemsToClean)
                {
                    DictJustDroppedItems.Remove(item);
                }
            }
        }

        public List<GameObject> GetGrabbableObjectsList()
        {
            if (getGrabbableObjectsListTimed == null)
            {
                getGrabbableObjectsListTimed = new TimedGetGrabbableObjectsList();
            }

            return getGrabbableObjectsListTimed.GetGrabbableObjectsList();
        }

        public List<GrabbableObject> LookingForItemsToGrabInMap()
        {
            var items = new List<GrabbableObject>();
            var grabbableObjectsList = GetGrabbableObjectsList();
            for (int i = 0; i < grabbableObjectsList.Count; i++)
            {
                GameObject gameObject = grabbableObjectsList[i];
                if (gameObject == null)
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

                // Grabbable object ?
                if (!IsGrabbableObjectGrabbable(grabbableObject))
                {
                    continue;
                }

                items.Add(grabbableObject);
            }

            return items;
        }

        /// <summary>
        /// Check all conditions for deciding if an item is grabbable or not.
        /// </summary>
        /// <param name="grabbableObject">Item to check</param>
        /// <returns></returns>
        public bool IsGrabbableObjectGrabbable(GrabbableObject grabbableObject)
        {
            InternManager.Instance.TrimDictJustDroppedItems();

            if (grabbableObject == null
                || !grabbableObject.gameObject.activeSelf)
            {
                return false;
            }

            if (grabbableObject.isHeld
                || !grabbableObject.grabbable
                || grabbableObject.deactivated)
            {
                return false;
            }

            RagdollGrabbableObject? ragdollGrabbableObject = grabbableObject as RagdollGrabbableObject;
            if (ragdollGrabbableObject != null)
            {
                if (!ragdollGrabbableObject.grabbableToEnemies)
                {
                    return false;
                }
            }

            // Item just dropped, should wait a bit before grab it again
            if (InternManager.Instance.IsGrabbableObjectJustDropped(grabbableObject))
            {
                // Trim dictionnary if too large
                return false;
            }

            // Object on ship
            if (grabbableObject.isInElevator
                || grabbableObject.isInShipRoom)
            {
                return false;
            }

            // Object in cruiser vehicle
            if (grabbableObject.transform.parent != null
                && grabbableObject.transform.parent.name.StartsWith("CompanyCruiser"))
            {
                return false;
            }

            // Object in a container mod of some sort ?
            if (PluginRuntimeProvider.Context.IsModCustomItemBehaviourLibraryLoaded)
            {
                if (CustomItemBehaviourLibraryHook.IsGrabbableObjectInContainerMod?.Invoke(grabbableObject) ?? false)
                {
                    return false;
                }
            }

            // Is a pickmin (LethalMin mod) holding the object ?
            if (PluginRuntimeProvider.Context.IsModLethalMinLoaded)
            {
                if (LethalMinHook.IsGrabbableObjectHeldByPikminMod?.Invoke(grabbableObject) ?? false)
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsGrabbableObjectBlackListed(GameObject gameObjectToEvaluate)
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

            // Baby kiwi egg
            if (!PluginRuntimeProvider.Context.Config.GrabKiwiBabyItem
                && gameObjectToEvaluate.name.Contains("KiwiBabyItem"))
            {
                return true;
            }

            // Apparatus
            if (!PluginRuntimeProvider.Context.Config.GrabApparatus
                && gameObjectToEvaluate.name.Contains("LungApparatus"))
            {
                return true;
            }

            return false;
        }

        public bool ShouldShovelIgnoreIntern(Shovel shovel, Transform transform)
        {
            IInternAI? internHolder = GetInternAI((int)shovel.playerHeldBy.playerClientId);
            if (internHolder == null)
            {
                return false;
            }
            // An intern is holding the shovel

            // PlayerControllerB through EnemyAICollisionDetect?
            PlayerControllerB? internControllerHit = transform.gameObject.layer == 3 ? transform.gameObject.GetComponent<PlayerControllerB>() : null;
            if (internControllerHit != null
                && internHolder.Npc.playerClientId == internControllerHit.playerClientId)
            {
                // Ignore self
                return true;
            }

            // InternAI through EnemyAICollisionDetect ?
            EnemyAICollisionDetect? enemyAICollisionDetect = transform.gameObject.GetComponent<EnemyAICollisionDetect>();
            IInternAI? internHit = null;
            if (enemyAICollisionDetect != null)
            {
                internHit = enemyAICollisionDetect.mainScript as IInternAI;
            }
            if (internHit != null
                && internHolder == internHit)
            {
                // Ignore self
                return true;
            }

            if (true) // ignore all other interns
            {
                if (IsPlayerIntern(internControllerHit))
                {
                    return true;
                }
                else if (internHit != null)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
