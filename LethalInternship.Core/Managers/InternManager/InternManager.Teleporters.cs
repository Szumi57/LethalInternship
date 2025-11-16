using LethalInternship.Core.Interns.AI;
using LethalInternship.SharedAbstractions.Hooks.ShipTeleporterHooks;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Random = System.Random;

namespace LethalInternship.Core.Managers
{
    public partial class InternManager
    {
        private Coroutine BeamOutInternsCoroutine = null!;

        #region Teleporters

        public void TeleportOutInterns(ShipTeleporter teleporter,
                                       Random shipTeleporterSeed)
        {
            if (this.BeamOutInternsCoroutine != null)
            {
                base.StopCoroutine(this.BeamOutInternsCoroutine);
            }
            this.BeamOutInternsCoroutine = base.StartCoroutine(this.BeamOutInterns(teleporter, shipTeleporterSeed));
        }

        private IEnumerator BeamOutInterns(ShipTeleporter teleporter,
                                           Random shipTeleporterSeed)
        {
            yield return new WaitForSeconds(5f);

            if (StartOfRound.Instance.inShipPhase)
            {
                yield break;
            }

            Vector3 positionIntern;
            Vector3 teleportPos;
            foreach (InternAI internAI in AllInternAIs)
            {
                if (internAI == null
                    || !internAI.IsSpawned
                    || internAI.isEnemyDead
                    || internAI.NpcController == null
                    || internAI.NpcController.Npc.isPlayerDead
                    || !internAI.NpcController.Npc.isPlayerControlled
                    || internAI.RagdollInternBody.IsRagdollBodyHeld())
                {
                    continue;
                }

                positionIntern = internAI.NpcController.Npc.transform.position;
                if (internAI.NpcController.Npc.deadBody != null)
                {
                    positionIntern = internAI.NpcController.Npc.deadBody.bodyParts[5].transform.position;
                }

                if ((positionIntern - teleporter.teleportOutPosition.position).sqrMagnitude > 2f * 2f)
                {
                    continue;
                }

                if (RoundManager.Instance.insideAINodes.Length == 0)
                {
                    continue;
                }

                // Random pos
                teleportPos = RoundManager.Instance.insideAINodes[shipTeleporterSeed.Next(0, RoundManager.Instance.insideAINodes.Length)].transform.position;
                teleportPos = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(teleportPos, 10f, default(NavMeshHit), shipTeleporterSeed, -1);

                // Teleport intern
                ShipTeleporterHook.SetPlayerTeleporterId_ReversePatch?.Invoke(teleporter, internAI.NpcController.Npc, 2);
                internAI.TeleportIntern(teleportPos, setOutside: false, isUsingEntrance: false);
                internAI.NpcController.Npc.beamOutParticle.Play();
                teleporter.shipTeleporterAudio.PlayOneShot(teleporter.teleporterBeamUpSFX);
            }
        }

        #endregion
    }
}
