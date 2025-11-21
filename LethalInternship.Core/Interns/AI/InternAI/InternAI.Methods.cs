using GameNetcodeStuff;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.CustomItemBehaviourLibraryHooks;
using LethalInternship.SharedAbstractions.Hooks.LethalMinHooks;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI
{
    public partial class InternAI
    {
        public EntranceTeleport[] EntrancesTeleportArray = null!;
        private DoorLock[] doorLocksArray = null!;
        private string stateIndicatorServer = string.Empty;
        private float timerCheckDoor;

        public bool IsAgentInValidState()
        {
            if (agent.isActiveAndEnabled
                && agent.isOnNavMesh
                && !isEnemyDead
                && !NpcController.Npc.isPlayerDead
                && !StartOfRound.Instance.shipIsLeaving)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Set the destination in <c>EnemyAI</c>, not on the agent
        /// </summary>
        /// <param name="position">the destination</param>
        public void SetDestinationToPositionInternAI(Vector3 position)
        {
            moveTowardsDestination = true;
            movingTowardsTargetPlayer = false;

            destination = position;
        }

        /// <summary>
        /// Try to set the destination on the agent, if destination not reachable, try the closest possible position of the destination
        /// </summary>
        public void UpdateDestinationToAgent(bool calculatePartialPath = false, bool checkForPath = false)
        {
            if (IsAgentInValidState()
                && agent.destination != base.destination)
            {
                agent.SetDestination(destination);
            }
        }

        public void OrderAgentAndBodyMoveToDestination(bool calculatePartialPath = false, bool checkForPath = false)
        {
            NpcController.OrderToMove();
            UpdateDestinationToAgent(calculatePartialPath, checkForPath);
        }

        public void StopMoving()
        {
            if (NpcController.HasToMove)
            {
                NpcController.OrderToStopMoving();
            }
        }

        /// <summary>
        /// Is the current client running the code is the owner of the <c>InternAI</c> ?
        /// </summary>
        /// <returns></returns>
        public bool IsClientOwnerOfIntern()
        {
            if (GameNetworkManager.Instance == null
                || GameNetworkManager.Instance.localPlayerController == null)
            {
                return false;
            }

            return OwnerClientId == GameNetworkManager.Instance.localPlayerController.actualClientId;
        }

        // Todo to remove
        public void CheckAndBringCloserTeleportIntern(float percentageOfDestination)
        {
            bool isAPlayerSeeingIntern = false;
            StartOfRound instanceSOR = StartOfRound.Instance;
            Transform thisInternCamera = NpcController.Npc.gameplayCamera.transform;
            PlayerControllerB player;
            Vector3 vectorPlayerToIntern;
            Vector3 internDestination = NpcController.Npc.thisPlayerBody.transform.position + (destination - NpcController.Npc.transform.position) * percentageOfDestination;
            Vector3 internBodyDestination = internDestination + new Vector3(0, 1f, 0);
            for (int i = 0; i < InternManager.Instance.IndexBeginOfInterns; i++)
            {
                player = instanceSOR.allPlayerScripts[i];
                if (player.isPlayerDead
                    || !player.isPlayerControlled)
                {
                    continue;
                }

                // No obsruction
                if (!Physics.Linecast(player.gameplayCamera.transform.position, thisInternCamera.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
                {
                    vectorPlayerToIntern = thisInternCamera.position - player.gameplayCamera.transform.position;
                    if (Vector3.Angle(player.gameplayCamera.transform.forward, vectorPlayerToIntern) < player.gameplayCamera.fieldOfView)
                    {
                        isAPlayerSeeingIntern = true;
                        break;
                    }
                }

                if (!Physics.Linecast(player.gameplayCamera.transform.position, internBodyDestination, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
                {
                    vectorPlayerToIntern = internBodyDestination - player.gameplayCamera.transform.position;
                    if (Vector3.Angle(player.gameplayCamera.transform.forward, vectorPlayerToIntern) < player.gameplayCamera.fieldOfView)
                    {
                        isAPlayerSeeingIntern = true;
                        break;
                    }
                }
            }

            if (!isAPlayerSeeingIntern)
            {
                TeleportIntern(internDestination);
            }
        }

        /// <summary>
        /// Check the line of sight if the intern can see the target player
        /// </summary>
        /// <param name="width">FOV of the intern</param>
        /// <param name="range">Distance max for seeing something</param>
        /// <param name="proximityAwareness">Distance where the interns "sense" the player, in line of sight or not. -1 for no proximity awareness</param>
        /// <returns>Target player <c>PlayerControllerB</c> or null</returns>
        public PlayerControllerB? CheckLOSForTarget(float width = 45f, int range = 60, int proximityAwareness = -1)
        {
            if (targetPlayer == null)
            {
                return null;
            }

            if (!PlayerIsTargetable(targetPlayer))
            {
                return null;
            }

            // Fog reduce the visibility
            if (isOutside && !enemyType.canSeeThroughFog && TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Foggy)
            {
                range = Mathf.Clamp(range, 0, 30);
            }

            // Check for target player
            Transform thisInternCamera = NpcController.Npc.gameplayCamera.transform;
            Vector3 posTargetCamera = targetPlayer.gameplayCamera.transform.position;
            if (Vector3.Distance(posTargetCamera, thisInternCamera.position) < range
                && !Physics.Linecast(thisInternCamera.position, posTargetCamera, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
            {
                // Target close enough and nothing in between to break line of sight 
                Vector3 to = posTargetCamera - thisInternCamera.position;
                if (Vector3.Angle(thisInternCamera.forward, to) < width
                    || proximityAwareness != -1 && Vector3.Distance(thisInternCamera.position, posTargetCamera) < proximityAwareness)
                {
                    // Target in FOV or proximity awareness range
                    return targetPlayer;
                }
            }

            return null;
        }

        /// <summary>
        /// Check the line of sight if the intern can see any player and take the closest.
        /// </summary>
        /// <param name="width">FOV of the intern</param>
        /// <param name="range">Distance max for seeing something</param>
        /// <param name="proximityAwareness">Distance where the interns "sense" the player, in line of sight or not. -1 for no proximity awareness</param>
        /// <param name="bufferDistance"></param>
        /// <returns>Target player <c>PlayerControllerB</c> or null</returns>
        public PlayerControllerB? CheckLOSForClosestPlayer(float width = 45f, int range = 60, int proximityAwareness = -1, float bufferDistance = 0f)
        {
            // Fog reduce the visibility
            if (isOutside && !enemyType.canSeeThroughFog && TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Foggy)
            {
                range = Mathf.Clamp(range, 0, 30);
            }

            StartOfRound instanceSOR = StartOfRound.Instance;
            Transform thisInternCamera = NpcController.Npc.gameplayCamera.transform;
            float currentClosestDistance = 1000f;
            int indexPlayer = -1;
            for (int i = 0; i < InternManager.Instance.IndexBeginOfInterns; i++)
            {
                PlayerControllerB player = instanceSOR.allPlayerScripts[i];

                if (!player.isPlayerControlled || player.isPlayerDead)
                {
                    continue;
                }

                // Target close enough ?
                Vector3 cameraPlayerPosition = player.gameplayCamera.transform.position;
                if ((cameraPlayerPosition - transform.position).sqrMagnitude > range * range)
                {
                    continue;
                }

                if (!PlayerIsTargetable(player))
                {
                    continue;
                }

                // Nothing in between to break line of sight ?
                if (Physics.Linecast(thisInternCamera.position, cameraPlayerPosition, instanceSOR.collidersAndRoomMaskAndDefault))
                {
                    continue;
                }

                Vector3 vectorInternToPlayer = cameraPlayerPosition - thisInternCamera.position;
                float distanceInternToPlayer = Vector3.Distance(thisInternCamera.position, cameraPlayerPosition);
                if ((Vector3.Angle(thisInternCamera.forward, vectorInternToPlayer) < width || proximityAwareness != -1 && distanceInternToPlayer < proximityAwareness)
                    && distanceInternToPlayer < currentClosestDistance)
                {
                    // Target in FOV or proximity awareness range
                    currentClosestDistance = distanceInternToPlayer;
                    indexPlayer = i;
                }
            }

            if (targetPlayer != null
                && indexPlayer != -1
                && targetPlayer != instanceSOR.allPlayerScripts[indexPlayer]
                && bufferDistance > 0f
                && Mathf.Abs(currentClosestDistance - Vector3.Distance(transform.position, targetPlayer.transform.position)) < bufferDistance)
            {
                return null;
            }

            if (indexPlayer < 0)
            {
                return null;
            }

            mostOptimalDistance = currentClosestDistance;
            return instanceSOR.allPlayerScripts[indexPlayer];
        }

        /// <summary>
        /// Check for, an enemy, the minimal distance from enemy to intern before panicking.
        /// </summary>
        /// <param name="enemy">Enemy to check</param>
        /// <returns>The minimal distance from enemy to intern before panicking, null if nothing to worry about</returns>
        public float? GetFearRangeForEnemies(EnemyAI enemy)
        {
            //PluginLoggerHook.LogDebug?.Invoke($"enemy \"{enemy.enemyType.enemyName}\" {enemy.enemyType.name}");
            switch (enemy.enemyType.enemyName) // using enemyName
            {
                case "Crawler":
                case "MouthDog":
                case "ForestGiant":
                case "Butler Bees":
                case "Nutcracker":
                case "Blob":
                case "ImmortalSnail":
                    return 15f;

                case "Red Locust Bees":
                case "Earth Leviathan":
                case "Clay Surgeon":
                case "Flowerman":
                case "Bush Wolf":
                case "GiantKiwi":
                    return 5f;

                case "Puffer":
                    return 2f;

                case "Centipede":
                    return 1f;

                case "Bunker Spider":
                    if (enemy.currentBehaviourStateIndex == 2)
                    {
                        // Mad
                        return 15f;
                    }
                    else
                    {
                        return null;
                    }

                case "Spring":
                    if (enemy.currentBehaviourStateIndex > 0)
                    {
                        // Mad
                        return 15f;
                    }
                    else
                    {
                        return null;
                    }

                case "Butler":
                    if (enemy.currentBehaviourStateIndex == 2)
                    {
                        // Mad
                        return 15f;
                    }
                    else
                    {
                        return null;
                    }

                case "Hoarding bug":
                    if (enemy.currentBehaviourStateIndex == 2)
                    {
                        // Mad
                        return 15f;
                    }
                    else
                    {
                        return null;
                    }

                case "Jester":
                    if (enemy.currentBehaviourStateIndex == 2)
                    {
                        // Mad
                        return 15f;
                    }
                    else
                    {
                        return null;
                    }

                case "RadMech":
                    if (enemy.currentBehaviourStateIndex > 0)
                    {
                        // Mad
                        return 15f;
                    }
                    else
                    {
                        return null;
                    }

                case "Baboon hawk":
                    if (enemy.currentBehaviourStateIndex == 2)
                    {
                        // Mad
                        return 15f;
                    }
                    else
                    {
                        return null;
                    }


                case "Maneater":
                    if (enemy.currentBehaviourStateIndex > 0)
                    {
                        // Mad
                        return 15f;
                    }
                    else
                    {
                        return null;
                    }

                default:
                    // Not dangerous enemies (at first sight)

                    // "Docile Locust Bees"
                    // "Manticoil"
                    // "Masked"
                    // "Girl"
                    // "Tulip Snake"
                    return null;
            }
        }

        public void ReParentIntern(Transform newParent)
        {
            NpcController.ReParentNotSpawnedTransform(newParent);
        }

        public string GetSizedBillboardStateIndicator()
        {
            string indicator;
            int sizePercentage = Math.Clamp((int)(100f + 2.5f * NpcController.GetSqrDistanceWithLocalPlayer(NpcController.Npc.transform.position)),
                                 100, 500);

            if (IsOwner)
            {
                //indicator = State == null ? string.Empty : State.GetBillboardStateIndicator();
                indicator = string.Empty;
            }
            else
            {
                indicator = stateIndicatorServer;
            }

            return $"<size={sizePercentage}%>{indicator}</size>";
        }

        /// <summary>
        /// Search for all the loaded ladders on the map.
        /// </summary>
        /// <returns>Array of <c>InteractTrigger</c> (ladders)</returns>
        private InteractTrigger[] RefreshLaddersList()
        {
            List<InteractTrigger> ladders = new List<InteractTrigger>();
            InteractTrigger[] interactsTrigger = Resources.FindObjectsOfTypeAll<InteractTrigger>();
            for (int i = 0; i < interactsTrigger.Length; i++)
            {
                if (interactsTrigger[i] == null)
                {
                    continue;
                }

                if (interactsTrigger[i].isLadder && interactsTrigger[i].ladderHorizontalPosition != null)
                {
                    ladders.Add(interactsTrigger[i]);
                }
            }
            return ladders.ToArray();
        }

        /// <summary>
        /// Check every ladder to see if the body of intern is close to either the bottom of the ladder (wants to go up) or the top of the ladder (wants to go down).
        /// Orders the controller to set field <c>hasToGoDown</c>.
        /// </summary>
        /// <returns>The ladder to use, null if nothing close</returns>
        public InteractTrigger? GetLadderIfWantsToUseLadder()
        {
            //InteractTrigger ladder;
            //Vector3 npcBodyPos = NpcController.Npc.thisController.transform.position;
            //for (int i = 0; i < laddersInteractTrigger.Length; i++)
            //{
            //    ladder = laddersInteractTrigger[i];
            //    Vector3 ladderBottomPos = ladder.bottomOfLadderPosition.position;
            //    Vector3 ladderTopPos = ladder.topOfLadderPosition.position;

            //    if ((ladderBottomPos - npcBodyPos).sqrMagnitude < Const.DISTANCE_NPCBODY_FROM_LADDER * Const.DISTANCE_NPCBODY_FROM_LADDER)
            //    {
            //        PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} Wants to go up on ladder");
            //        // Wants to go up on ladder
            //        NpcController.OrderToGoUpDownLadder(hasToGoDown: false);
            //        return ladder;
            //    }
            //    else if ((ladderTopPos - npcBodyPos).sqrMagnitude < Const.DISTANCE_NPCBODY_FROM_LADDER * Const.DISTANCE_NPCBODY_FROM_LADDER)
            //    {
            //        PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} Wants to go down on ladder");
            //        // Wants to go down on ladder
            //        NpcController.OrderToGoUpDownLadder(hasToGoDown: true);
            //        return ladder;
            //    }
            //}
            return null;
        }

        /// <summary>
        /// Check all doors to know if the intern is close enough to it to open it if necessary.
        /// </summary>
        /// <returns></returns>
        public DoorLock? GetDoorIfWantsToOpen()
        {
            Vector3 npcBodyPos = NpcController.Npc.thisController.transform.position;
            foreach (var door in doorLocksArray.Where(x => !x.isLocked))
            {
                if ((door.transform.position - npcBodyPos).sqrMagnitude < Const.DISTANCE_NPCBODY_FROM_DOOR * Const.DISTANCE_NPCBODY_FROM_DOOR)
                {
                    return door;
                }
            }
            return null;
        }

        /// <summary>
        /// Check the doors after some interval of ms to see if intern can open one to unstuck himself.
        /// </summary>
        /// <returns>true: a door has been opened by intern. Else false</returns>
        private bool OpenDoorIfNeeded()
        {
            if (timerCheckDoor > Const.TIMER_CHECK_DOOR)
            {
                timerCheckDoor = 0f;

                DoorLock? door = GetDoorIfWantsToOpen();
                if (door != null)
                {
                    // Prevent stuck behind open door
                    Physics.IgnoreCollision(NpcController.Npc.playerCollider, door.GetComponent<Collider>());

                    // Open door
                    door.OpenOrCloseDoor(NpcController.Npc);
                    door.OpenDoorAsEnemyServerRpc();
                    return true;
                }
            }
            timerCheckDoor += AIIntervalTime;
            return false;
        }

        /// <summary>
        /// Check ladders if intern needs to use one to follow player.
        /// </summary>
        /// <returns>true: the intern is using or is waiting to use the ladder, else false</returns>
        private bool UseLadderIfNeeded()
        {
            //if (NpcController.Npc.isClimbingLadder)
            //{
            //    return true;
            //}

            //InteractTrigger? ladder = GetLadderIfWantsToUseLadder();
            //if (ladder == null)
            //{
            //    return false;
            //}

            //// Intern wants to use ladder
            //// Removing all that for the moment
            ////if (PluginRuntimeProvider.Context.Config.TeleportWhenUsingLadders.Value)
            ////{
            ////    NpcController.Npc.transform.position = this.transform.position;
            ////    return true;
            ////}

            //// Try to use ladder
            //if (NpcController.CanUseLadder(ladder))
            //{
            //    InteractTriggerPatch.Interact_ReversePatch(ladder, NpcController.Npc.thisPlayerBody);

            //    // Set rotation of intern to face ladder
            //    NpcController.Npc.transform.rotation = ladder.ladderPlayerPositionNode.transform.rotation;
            //    NpcController.SetTurnBodyTowardsDirection(NpcController.Npc.transform.forward);
            //}
            //else
            //{
            //    // Wait to use ladder
            //    StopMoving();
            //}

            return true;
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

            // Is item too close to entrance (with config option enabled)
            if (!PluginRuntimeProvider.Context.Config.GrabItemsNearEntrances)
            {
                for (int j = 0; j < EntrancesTeleportArray.Length; j++)
                {
                    if ((grabbableObject.transform.position - EntrancesTeleportArray[j].entrancePoint.position).sqrMagnitude < Const.DISTANCE_ITEMS_TO_ENTRANCE * Const.DISTANCE_ITEMS_TO_ENTRANCE)
                    {
                        return false;
                    }
                }
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

        public void SetInternInElevator()
        {
            StartOfRound instanceSOR = StartOfRound.Instance;

            if (NpcController == null)
            {
                return;
            }

            if (RagdollInternBody != null
                && RagdollInternBody.IsRagdollBodyHeld())
            {
                return;
            }

            bool wasInHangarShipRoom = NpcController.Npc.isInHangarShipRoom;
            if (!NpcController.Npc.isInElevator
                && instanceSOR.shipBounds.bounds.Contains(NpcController.Npc.transform.position))
            {
                NpcController.Npc.isInElevator = true;
            }

            if (NpcController.Npc.isInElevator
                && !wasInHangarShipRoom
                && instanceSOR.shipInnerRoomBounds.bounds.Contains(NpcController.Npc.transform.position))
            {
                NpcController.Npc.isInHangarShipRoom = true;
            }
            else if (NpcController.Npc.isInElevator
                && !instanceSOR.shipBounds.bounds.Contains(NpcController.Npc.transform.position))
            {
                NpcController.Npc.isInElevator = false;
                NpcController.Npc.isInHangarShipRoom = false;
                wasInHangarShipRoom = false;

                if (!AreHandsFree())
                {
                    NpcController.Npc.SetItemInElevator(droppedInShipRoom: false, droppedInElevator: false, HeldItems.GetLastPickedUpItem());
                }
            }

            if (wasInHangarShipRoom != NpcController.Npc.isInHangarShipRoom
                && !NpcController.Npc.isInHangarShipRoom
                && !AreHandsFree())
            {
                NpcController.Npc.SetItemInElevator(droppedInShipRoom: false, droppedInElevator: true, HeldItems.GetLastPickedUpItem());
            }
        }

        public void HideShowLevelStickerBetaBadge(bool show)
        {
            MeshRenderer[] componentsInChildren = NpcController.Npc.gameObject.GetComponentsInChildren<MeshRenderer>();
            (from x in componentsInChildren
             where x.gameObject.name == "LevelSticker"
             select x).First().enabled = show;
            (from x in componentsInChildren
             where x.gameObject.name == "BetaBadge"
             select x).First().enabled = show;
        }

        // todo: to remove
        public void SetInternLookAt(Vector3? position = null)
        {
            if (PluginRuntimeProvider.Context.InputActionsInstance.MakeInternLookAtPosition.IsPressed())
            {
                LookAtWhatPlayerPointingAt();
            }
            else
            {
                if (position.HasValue)
                {
                    NpcController.OrderToLookAtPlayer(position.Value + new Vector3(0, 2.35f, 0));
                }
                else
                {
                    // Looking at player or forward
                    PlayerControllerB? playerToLook = CheckLOSForClosestPlayer(Const.INTERN_FOV, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
                    if (playerToLook != null)
                    {
                        NpcController.OrderToLookAtPlayer(playerToLook.playerEye.position);
                    }
                    else
                    {
                        NpcController.OrderToLookForward();
                    }
                }
            }
        }

        public void LookAtWhatPlayerPointingAt()
        {
            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;

            // Look where the target player is looking
            Ray interactRay = new Ray(localPlayer.gameplayCamera.transform.position, localPlayer.gameplayCamera.transform.forward);
            RaycastHit[] raycastHits = Physics.RaycastAll(interactRay);
            if (raycastHits.Length == 0)
            {
                NpcController.SetTurnBodyTowardsDirection(localPlayer.gameplayCamera.transform.forward);
                NpcController.OrderToLookForward();
            }
            else
            {
                // Check if looking at a player/intern
                foreach (var hit in raycastHits)
                {
                    PlayerControllerB? player = hit.collider.gameObject.GetComponent<PlayerControllerB>();
                    if (player != null
                        && player.playerClientId != StartOfRound.Instance.localPlayerController.playerClientId)
                    {
                        NpcController.OrderToLookAtPosition(hit.point);
                        NpcController.SetTurnBodyTowardsDirectionWithPosition(hit.point);
                        return;
                    }
                }

                // Check if looking too far in the distance or at a valid position
                foreach (var hit in raycastHits)
                {
                    if (hit.distance < 0.1f)
                    {
                        NpcController.SetTurnBodyTowardsDirection(localPlayer.gameplayCamera.transform.forward);
                        NpcController.OrderToLookForward();
                        return;
                    }

                    PlayerControllerB? player = hit.collider.gameObject.GetComponent<PlayerControllerB>();
                    if (player != null && player.playerClientId == StartOfRound.Instance.localPlayerController.playerClientId)
                    {
                        continue;
                    }

                    // Look at position
                    NpcController.OrderToLookAtPosition(hit.point);
                    NpcController.SetTurnBodyTowardsDirectionWithPosition(hit.point);
                    break;
                }
            }
        }
    }
}
