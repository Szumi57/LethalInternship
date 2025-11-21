using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using Unity.Netcode;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI
{

    public partial class InternAI
    {
        public float TimeSinceTeleporting = 0f;

        #region TeleportIntern RPC

        /// <summary>
        /// Teleport intern and send to server to call client to sync
        /// </summary>
        /// <param name="pos">Position destination</param>
        /// <param name="setOutside">Is the teleport destination outside of the facility</param>
        /// <param name="isUsingEntrance">Is the intern actually using entrance to teleport ?</param>
        public void SyncTeleportIntern(Vector3 pos, bool setOutside, bool isUsingEntrance)
        {
            if (!IsOwner)
            {
                return;
            }
            TeleportIntern(pos, setOutside, isUsingEntrance);
            TeleportInternServerRpc(pos, setOutside, isUsingEntrance);
        }
        /// <summary>
        /// Server side, call clients to sync teleport intern
        /// </summary>
        /// <param name="pos">Position destination</param>
        /// <param name="setOutside">Is the teleport destination outside of the facility</param>
        /// <param name="isUsingEntrance">Is the intern actually using entrance to teleport ?</param>
        [ServerRpc]
        private void TeleportInternServerRpc(Vector3 pos, bool setOutside, bool isUsingEntrance)
        {
            TeleportInternClientRpc(pos, setOutside, isUsingEntrance);
        }
        /// <summary>
        /// Client side, teleport intern on client, only for not the owner
        /// </summary>
        /// <param name="pos">Position destination</param>
        /// <param name="setOutside">Is the teleport destination outside of the facility</param>
        /// <param name="isUsingEntrance">Is the intern actually using entrance to teleport ?</param>
        [ClientRpc]
        private void TeleportInternClientRpc(Vector3 pos, bool setOutside, bool isUsingEntrance)
        {
            if (IsOwner)
            {
                return;
            }
            TeleportIntern(pos, setOutside, isUsingEntrance);
        }

        /// <summary>
        /// Teleport the intern.
        /// </summary>
        /// <param name="pos">Position destination</param>
        /// <param name="setOutside">Is the teleport destination outside of the facility</param>
        /// <param name="isUsingEntrance">Is the intern actually using entrance to teleport ?</param>
        public void TeleportIntern(Vector3 pos, bool? setOutside = null, bool isUsingEntrance = false)
        {
            // teleport body
            TeleportAgentAIAndBody(pos);

            // Set AI outside or inside dungeon
            if (!setOutside.HasValue)
            {
                setOutside = pos.y >= -80f;
            }

            NpcController.Npc.isInsideFactory = !setOutside.Value;
            if (isOutside != setOutside.Value)
            {
                SetEnemyOutside(setOutside.Value);
            }

            // Using main entrance or fire exits ?
            if (isUsingEntrance)
            {
                NpcController.Npc.thisPlayerBody.RotateAround(NpcController.Npc.thisPlayerBody.transform.position, Vector3.up, 180f);
                TimeSinceTeleporting = Time.timeSinceLevelLoad;
                EntranceTeleport entranceTeleport = RoundManager.FindMainEntranceScript(setOutside.Value);
                if (entranceTeleport.doorAudios != null && entranceTeleport.doorAudios.Length != 0)
                {
                    entranceTeleport.entrancePointAudio.PlayOneShot(entranceTeleport.doorAudios[0]);
                }
            }
        }

        /// <summary>
        /// Teleport the brain and body of intern
        /// </summary>
        /// <param name="pos"></param>
        private void TeleportAgentAIAndBody(Vector3 pos)
        {
            Vector3 navMeshPosition = RoundManager.Instance.GetNavMeshPosition(pos, default, 2.7f);
            serverPosition = navMeshPosition;
            NpcController.Npc.transform.position = navMeshPosition;

            if (agent == null
                || !agent.enabled)
            {
                transform.position = navMeshPosition;
            }
            else
            {
                agent.enabled = false;
                transform.position = navMeshPosition;
                agent.enabled = true;
            }

            // For CullFactory mod
            HeldItems.ShowHideAllItemsMeshes(show: true);
        }

        public void SyncTeleportInternVehicle(Vector3 pos, bool enteringVehicle, NetworkBehaviourReference networkBehaviourReferenceVehicle)
        {
            if (!IsOwner)
            {
                return;
            }
            TeleportInternVehicle(pos, enteringVehicle, networkBehaviourReferenceVehicle);
            TeleportInternVehicleServerRpc(pos, enteringVehicle, networkBehaviourReferenceVehicle);
        }

        [ServerRpc]
        private void TeleportInternVehicleServerRpc(Vector3 pos, bool enteringVehicle, NetworkBehaviourReference networkBehaviourReferenceVehicle)
        {
            TeleportInternVehicleClientRpc(pos, enteringVehicle, networkBehaviourReferenceVehicle);
        }
        [ClientRpc]
        private void TeleportInternVehicleClientRpc(Vector3 pos, bool enteringVehicle, NetworkBehaviourReference networkBehaviourReferenceVehicle)
        {
            if (IsOwner)
            {
                return;
            }
            TeleportInternVehicle(pos, enteringVehicle, networkBehaviourReferenceVehicle);
        }

        private void TeleportInternVehicle(Vector3 pos, bool enteringVehicle, NetworkBehaviourReference networkBehaviourReferenceVehicle)
        {
            if (enteringVehicle)
            {
                if (agent != null)
                {
                    agent.enabled = false;
                }
                NpcController.Npc.transform.position = pos;
                StateControllerMovement = EnumStateControllerMovement.Fixed;
            }
            else
            {
                TeleportIntern(pos);
                StateControllerMovement = EnumStateControllerMovement.FollowAgent;
            }

            NpcController.IsControllerInCruiser = enteringVehicle;

            if (NpcController.IsControllerInCruiser)
            {
                if (networkBehaviourReferenceVehicle.TryGet(out VehicleController vehicleController))
                {
                    // Attach intern to vehicle
                    PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} enters vehicle");
                    ReParentIntern(vehicleController.transform);
                }

                StopSinkingState();
            }
            else
            {
                PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} exits vehicle");
                ReParentIntern(NpcController.Npc.playersManager.playersContainer);
            }
        }

        #endregion
    }
}
