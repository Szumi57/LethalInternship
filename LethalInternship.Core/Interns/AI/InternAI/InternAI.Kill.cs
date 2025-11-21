using GameNetcodeStuff;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Hooks.ReviveCompanyHooks;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using Unity.Netcode;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI
{
    public partial class InternAI
    {
        #region Kill intern RPC

        public override void KillEnemy(bool destroy = false)
        {
            // The kill function works with player controller instead
            return;
        }

        /// <summary>
        /// Sync the action to kill intern between server and clients
        /// </summary>
        /// <remarks>
        /// Better to call <see cref="PlayerControllerB.KillPlayer"><c>PlayerControllerB.KillPlayer</c></see> so prefixes from other mods can activate. (ex : peepers)
        /// The base game function will be ignored because the intern playerController is not owned because not spawned
        /// </remarks>
        /// <param name="bodyVelocity"></param>
        /// <param name="spawnBody">Should a body be spawned ?</param>
        /// <param name="causeOfDeath"></param>
        /// <param name="deathAnimation"></param>
        public void SyncKillIntern(Vector3 bodyVelocity,
                                   bool spawnBody = true,
                                   CauseOfDeath causeOfDeath = CauseOfDeath.Unknown,
                                   int deathAnimation = 0,
                                   Vector3 positionOffset = default)
        {
            PluginLoggerHook.LogDebug?.Invoke($"SyncKillIntern for LOCAL client #{NetworkManager.LocalClientId}, intern object: Intern #{InternId} {NpcController.Npc.playerUsername}");

            if (NpcController.Npc.isPlayerDead)
            {
                return;
            }
            if (!NpcController.Npc.AllowPlayerDeath())
            {
                return;
            }

            if (IsServer)
            {
                KillInternSpawnBody(spawnBody);
                KillInternClientRpc(bodyVelocity, spawnBody, causeOfDeath, deathAnimation, positionOffset);
            }
            else
            {
                KillInternServerRpc(bodyVelocity, spawnBody, causeOfDeath, deathAnimation, positionOffset);
            }
        }

        /// <summary>
        /// Server side, call clients to do the action to kill intern
        /// </summary>
        /// <param name="bodyVelocity"></param>
        /// <param name="spawnBody"></param>
        /// <param name="causeOfDeath"></param>
        /// <param name="deathAnimation"></param>
        [ServerRpc]
        private void KillInternServerRpc(Vector3 bodyVelocity,
                                         bool spawnBody,
                                         CauseOfDeath causeOfDeath,
                                         int deathAnimation,
                                         Vector3 positionOffset)
        {
            KillInternSpawnBody(spawnBody);
            KillInternClientRpc(bodyVelocity, spawnBody, causeOfDeath, deathAnimation, positionOffset);
        }

        /// <summary>
        /// Server side, spawn the ragdoll of the dead body, despawn held object if no dead body to spawn
        /// (intern eaten or disappeared in some way)
        /// </summary>
        /// <param name="spawnBody">Is there a dead body to spawn following the death of the intern ?</param>
        [ServerRpc]
        private void KillInternSpawnBodyServerRpc(bool spawnBody)
        {
            KillInternSpawnBody(spawnBody);
        }

        /// <summary>
        /// Spawn the ragdoll of the dead body, despawn held object if no dead body to spawn
        /// (intern eaten or disappeared in some way)
        /// </summary>
        /// <param name="spawnBody">Is there a dead body to spawn following the death of the intern ?</param>
        private void KillInternSpawnBody(bool spawnBody)
        {
            if (!spawnBody)
            {
                for (int i = 0; i < NpcController.Npc.ItemSlots.Length; i++)
                {
                    GrabbableObject grabbableObject = NpcController.Npc.ItemSlots[i];
                    if (grabbableObject != null)
                    {
                        grabbableObject.gameObject.GetComponent<NetworkObject>().Despawn(true);
                    }
                }
            }
            else
            {
                GameObject gameObject = Instantiate(StartOfRound.Instance.ragdollGrabbableObjectPrefab, NpcController.Npc.playersManager.propsContainer);
                gameObject.GetComponent<NetworkObject>().Spawn(false);
                gameObject.GetComponent<RagdollGrabbableObject>().bodyID.Value = (int)NpcController.Npc.playerClientId;
            }
        }

        /// <summary>
        /// Client side, do the action to kill intern
        /// </summary>
        /// <param name="bodyVelocity"></param>
        /// <param name="spawnBody"></param>
        /// <param name="causeOfDeath"></param>
        /// <param name="deathAnimation"></param>
        [ClientRpc]
        private void KillInternClientRpc(Vector3 bodyVelocity,
                                         bool spawnBody,
                                         CauseOfDeath causeOfDeath,
                                         int deathAnimation,
                                         Vector3 positionOffset)
        {


            KillIntern(bodyVelocity, spawnBody, causeOfDeath, deathAnimation, positionOffset);
        }

        /// <summary>
        /// Do the action of killing the intern
        /// </summary>
        /// <param name="bodyVelocity"></param>
        /// <param name="spawnBody"></param>
        /// <param name="causeOfDeath"></param>
        /// <param name="deathAnimation"></param>
        private void KillIntern(Vector3 bodyVelocity,
                                bool spawnBody,
                                CauseOfDeath causeOfDeath,
                                int deathAnimation,
                                Vector3 positionOffset)
        {
            PluginLoggerHook.LogDebug?.Invoke(@$"KillIntern for LOCAL client #{NetworkManager.LocalClientId}, intern object: Intern #{InternId} {NpcController.Npc.playerUsername}
                            bodyVelocity {bodyVelocity}, spawnBody {spawnBody}, causeOfDeath {causeOfDeath}, deathAnimation {deathAnimation}, positionOffset {positionOffset}");
            if (NpcController.Npc.isPlayerDead)
            {
                return;
            }
            if (!NpcController.Npc.AllowPlayerDeath())
            {
                return;
            }

            // Clinging flower snake (tulip snakes)
            FlowerSnakeEnemy[] flowerSnakesArray = Object.FindObjectsByType<FlowerSnakeEnemy>(FindObjectsSortMode.None);
            foreach (var flowerSnake in flowerSnakesArray)
            {
                if (flowerSnake == null
                    || flowerSnake.clingingToPlayer != this.Npc)
                {
                    continue;
                }

                flowerSnake.StopClingingOnLocalClient();
            }

            // If ragdoll body of intern is held
            // Release the intern before killing him
            if (RagdollInternBody.IsRagdollBodyHeld())
            {
                PlayerControllerB playerHolder = RagdollInternBody.GetPlayerHolder();
                ReleaseIntern(playerHolder.playerClientId);
                TeleportIntern(playerHolder.transform.position, !playerHolder.isInsideFactory, isUsingEntrance: false);
            }

            // Reset body
            NpcController.Npc.isPlayerDead = true;
            NpcController.Npc.isPlayerControlled = false;
            NpcController.Npc.thisPlayerModelArms.enabled = false;
            NpcController.Npc.localVisor.position = NpcController.Npc.playersManager.notSpawnedPosition.position;
            InternManager.Instance.DisableInternControllerModel(NpcController.Npc.gameObject, NpcController.Npc, enable: false, disableLocalArms: false);
            NpcController.Npc.isInsideFactory = false;
            NpcController.Npc.IsInspectingItem = false;
            NpcController.Npc.inTerminalMenu = false;
            NpcController.Npc.twoHanded = false;
            NpcController.Npc.isHoldingObject = false;
            NpcController.Npc.currentlyHeldObjectServer = null;
            NpcController.Npc.carryWeight = 1f;
            NpcController.Npc.fallValue = 0f;
            NpcController.Npc.fallValueUncapped = 0f;
            NpcController.Npc.takingFallDamage = false;
            StopSinkingState();
            NpcController.Npc.sinkingValue = 0f;
            NpcController.Npc.hinderedMultiplier = 1f;
            NpcController.Npc.isMovementHindered = 0;
            NpcController.Npc.inAnimationWithEnemy = null;
            NpcController.Npc.bleedingHeavily = false;
            NpcController.Npc.setPositionOfDeadPlayer = true;
            NpcController.Npc.snapToServerPosition = false;
            NpcController.Npc.causeOfDeath = causeOfDeath;
            if (spawnBody)
            {
                NpcController.Npc.SpawnDeadBody((int)NpcController.Npc.playerClientId, bodyVelocity, (int)causeOfDeath, NpcController.Npc, deathAnimation, null, positionOffset);

                if (NpcController.Npc.deadBody != null)
                {
                    ResizeRagdoll(NpcController.Npc.deadBody.transform);
                    // Replace body position or else disappear with shotgun or knife (don't know why)
                    NpcController.Npc.deadBody.transform.position = NpcController.Npc.transform.position + Vector3.up + positionOffset;
                    // Need to be set to true (don't know why) (so many mysteries unsolved tonight)
                    NpcController.Npc.deadBody.canBeGrabbedBackByPlayers = true;
                    InternIdentity.DeadBody = NpcController.Npc.deadBody;

                    // Register body for animation culling
                    InternManager.Instance.RegisterInternBodyForAnimationCulling(NpcController.Npc.deadBody, HasInternModelReplacementAPI());
                }
            }
            NpcController.Npc.physicsParent = null;
            NpcController.Npc.overridePhysicsParent = null;
            NpcController.Npc.lastSyncedPhysicsParent = null;
            NpcController.CurrentInternPhysicsRegions.Clear();
            ReParentIntern(NpcController.Npc.playersManager.playersContainer);
            
            DropAllItems(waitBetweenItems: false);

            NpcController.Npc.DisableJetpackControlsLocally();
            NpcController.IsControllerInCruiser = false;
            isEnemyDead = true;
            InternIdentity.Hp = 0;
            if (agent != null)
            {
                agent.enabled = false;
            }
            InternIdentity.Voice.StopAudioFadeOut();
            PluginLoggerHook.LogDebug?.Invoke($"Ran kill intern function for LOCAL client #{NetworkManager.LocalClientId}, intern object: Intern #{InternId} {NpcController.Npc.playerUsername}");

            // Compat with revive company mod
            if (PluginRuntimeProvider.Context.IsModReviveCompanyLoaded)
            {
                ReviveCompanyHook.ReviveCompanySetPlayerDiedAt?.Invoke((int)Npc.playerClientId);
            }

            PointOfInterest = null;
            InternManager.Instance.CancelBatch((int)Npc.playerClientId);
        }

        #endregion
    }
}
