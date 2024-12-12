using LethalInternship.AI;
using LethalInternship.Managers;
using UnityEngine;

namespace LethalInternship.Patches.ModPatches.Zaprillator
{
    internal class RevivablePlayerPatch
    {
        public static bool StopShockingWithGun_Prefix(RagdollGrabbableObject ____ragdoll,
                                                      ref bool ____bodyShocked,
                                                      ref GrabbableObject ____shockedBy,
                                                      float ____batteryLevel)
        {
            if(____ragdoll == null)
            {
                return true;
            }

            InternAI? internAI = InternManager.Instance.GetInternAI((int)____ragdoll.ragdoll.playerScript.playerClientId);
            if (internAI == null)
            {
                return true;
            }

            if (!____bodyShocked)
            {
                return false;
            }

            ____bodyShocked = false;
            RoundManager.Instance.FlickerLights();

            if (____shockedBy == null)
            {
                return false;
            }

            var restoreHealth = internAI.MaxHealthPercent(Mathf.RoundToInt(____batteryLevel * 100));
            ____shockedBy.UseUpBatteries();
            ____shockedBy.SyncBatteryServerRpc(0);
            ____shockedBy = null!;

            InternManager.Instance.SpawnThisInternServerRpc(internAI.InternIdentity.IdIdentity,
                                                            new NetworkSerializers.SpawnInternsParamsNetworkSerializable()
                                                            {
                                                                ShouldDestroyDeadBody = true,
                                                                Hp = restoreHealth,
                                                                SpawnPosition = ____ragdoll.ragdoll.transform.position,
                                                                YRot = 0,
                                                                IsOutside = !GameNetworkManager.Instance.localPlayerController.isInsideFactory
                                                            });

            return false;
        }
    }
}
