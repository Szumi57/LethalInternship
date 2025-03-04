using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Enums;
using LethalInternship.Managers;
using LethalInternship.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace LethalInternship.Patches.ModPatches.UsualScrap
{
    public class DefibrillatorScriptPatch
    {
        public static readonly FieldInfo FieldUsesLimited = AccessTools.Field(AccessTools.TypeByName("UsualScrap.Behaviors.DefibrillatorScript"), "UsesLimited");
        public static readonly FieldInfo FieldUseLimit = AccessTools.Field(AccessTools.TypeByName("UsualScrap.Behaviors.DefibrillatorScript"), "UseLimit");
        public static readonly FieldInfo FieldDisplayRenderers = AccessTools.Field(AccessTools.TypeByName("UsualScrap.Behaviors.DefibrillatorScript"), "displayRenderers");

        public static bool RevivePlayer_Prefix(GrabbableObject __instance, int PlayerID, Vector3 SpawnPosition)
        {
            Plugin.LogDebug($"attempt to revive id {PlayerID}");
            InternAI? internAI = InternManager.Instance.GetInternAI(PlayerID);
            if (internAI == null)
            {
                return true;
            }

            if (!InternManager.Instance.IsServer)
            {
                return false;
            }

            InternIdentity internIdentity = internAI.InternIdentity;
            if (internIdentity == null
                || internIdentity.Alive)
            {
                return false;
            }

            // Respawn intern
            Plugin.LogDebug($"Reviving intern {internIdentity.Name}");
            InternManager.Instance.SpawnThisInternServerRpc(internIdentity.IdIdentity,
                                                            new NetworkSerializers.SpawnInternsParamsNetworkSerializable()
                                                            {
                                                                ShouldDestroyDeadBody = true,
                                                                enumSpawnAnimation = (int)EnumSpawnAnimation.OnlyPlayerSpawnAnimation,
                                                                SpawnPosition = SpawnPosition,
                                                                YRot = internAI.NpcController.Npc.transform.rotation.y,
                                                                IsOutside = SpawnPosition.y >= -80f,
                                                            });

            // Class is internal so reflection
            // We are not in an update loop (60 times per second) so it's okay I guess
            bool usesLimited = (bool)FieldUsesLimited.GetValue(__instance);
            int useLimit = (int)FieldUseLimit.GetValue(__instance);
            if (usesLimited && useLimit > 0)
            {
                FieldUseLimit.SetValue(__instance, useLimit - 1);
                if ((int)FieldUseLimit.GetValue(__instance) <= 0)
                {
                    Renderer[] displayRenderers = (Renderer[])FieldDisplayRenderers.GetValue(__instance);
                    foreach (Renderer display in displayRenderers)
                    {
                        display.material.SetColor("_EmissiveColor", Color.red);
                    }
                }
            }

            return false;
        }

        public static void RevivePlayer_ReversePatch(object instance)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var startIndex = -1;
                List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

                for (int i = 0; i < codes.Count; i++)
                {
                    Plugin.LogDebug($"{i} {codes[i]}");
                }

                // ----------------------------------------------------------------------
                for (var i = 0; i < codes.Count - 5; i++)
                {
                    if (codes[i].ToString().StartsWith("ldarg.0 NULL") // 33
                        && codes[i + 5].ToString().StartsWith("call void GameNetcodeStuff.PlayerControllerB::LandFromJumpServerRpc(")) // 38
                    {
                        startIndex = i;
                        break;
                    }
                }
                if (startIndex > -1)
                {
                    codes[startIndex + 5].operand = PatchesUtil.SyncLandFromJumpMethod;
                    codes.Insert(startIndex + 1, new CodeInstruction(OpCodes.Ldfld, PatchesUtil.FieldInfoPlayerClientId));
                    startIndex = -1;
                }
                else
                {
                    Plugin.LogError($"LethalInternship.Patches.NpcPatches.PlayerControllerBPatch.PlayerHitGroundEffects_ReversePatch could not use jump from land method for intern");
                }

                return codes.AsEnumerable();
            }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Transpiler(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
    }
}
