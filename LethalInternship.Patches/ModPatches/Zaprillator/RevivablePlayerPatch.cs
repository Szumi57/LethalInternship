using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.ManagerProviders;
using LethalInternship.SharedAbstractions.NetworkSerializers;
using UnityEngine;

namespace LethalInternship.Patches.ModPatches.Zaprillator
{
    public class RevivablePlayerPatch
    {
        public static bool StopShockingWithGun_Prefix(RagdollGrabbableObject ____ragdoll,
                                                      ref bool ____bodyShocked,
                                                      ref GrabbableObject ____shockedBy,
                                                      float ____batteryLevel)
        {
            if (____ragdoll == null)
            {
                return true;
            }

            if (!____bodyShocked)
            {
                return false;
            }

            if (____shockedBy == null
                || !____shockedBy.IsOwner)
            {
                return false;
            }

            string name = ____ragdoll.ragdoll.gameObject.GetComponentInChildren<ScanNodeProperties>().headerText;
            IInternIdentity? internIdentity = IdentityManagerProvider.Instance.FindIdentityFromBodyName(name);
            if (internIdentity == null)
            {
                return true;
            }

            // Get the same logic as the mod at the beginning
            if (internIdentity.Alive)
            {
                PluginLoggerHook.LogError?.Invoke($"Zaprillator with LethalInternship: error when trying to revive intern \"{internIdentity.Name}\", intern is already alive! do nothing more");
                return false;
            }

            ____bodyShocked = false;
            RoundManager.Instance.FlickerLights();

            var restoreHealth = InternManagerProvider.Instance.MaxHealthPercent(Mathf.RoundToInt(____batteryLevel * 100), internIdentity.HpMax);
            ____shockedBy.UseUpBatteries();
            ____shockedBy.SyncBatteryServerRpc(0);
            ____shockedBy = null!;

            InternManagerProvider.Instance.SpawnThisInternServerRpc(internIdentity.IdIdentity,
                                                                    new SpawnInternsParamsNetworkSerializable()
                                                                    {
                                                                        ShouldDestroyDeadBody = true,
                                                                        Hp = restoreHealth,
                                                                        enumSpawnAnimation = (int)EnumSpawnAnimation.OnlyPlayerSpawnAnimation,
                                                                        SpawnPosition = ____ragdoll.ragdoll.transform.position,
                                                                        YRot = 0,
                                                                        IsOutside = !GameNetworkManager.Instance.localPlayerController.isInsideFactory
                                                                    });

            return false;
        }
    }
}
