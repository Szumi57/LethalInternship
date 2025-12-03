using GameNetcodeStuff;
using LethalInternship.Core.Interns.AI.TimedTasks;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
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
