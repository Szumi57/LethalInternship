using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using ModelReplacement.AvatarBodyUpdater;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Patches.ModPatches.ModelRplcmntAPI
{
    [HarmonyPatch(typeof(AvatarUpdater))]
    public class AvatarUpdaterPatch
    {
        private static Dictionary<GameObject, Dictionary<AvatarUpdater, InternAvatarUpdater>> DictInternAvatarUpdater = null!;

        [HarmonyPatch("AssignModelReplacement")]
        [HarmonyPostfix]
        static void AssignModelReplacement_Postfix(AvatarUpdater __instance,
                                                  GameObject player,
                                                  GameObject replacement,
                                                  SkinnedMeshRenderer ___playerModelRenderer,
                                                  Vector3 ___rootPositionOffset,
                                                  ref GameObject ___player)
        {
            if (replacement == null)
            {
                return;
            }

            PlayerControllerB playerController = player.GetComponent<PlayerControllerB>();
            if (playerController != null
                && !InternManager.Instance.IsPlayerIntern(playerController))
            {
                Plugin.LogDebug("not intern");
                return;
            }

            // If dead body
            DeadBodyInfo deadBodyInfo = player.GetComponent<DeadBodyInfo>();
            if (deadBodyInfo == null)
            {
                Plugin.LogDebug("not dead body");
                return;
            }

            playerController = deadBodyInfo.playerScript;
            if (!InternManager.Instance.IsPlayerIntern(playerController))
            {
                Plugin.LogDebug("not dead body intern");
            }

            //___player = playerController.gameObject;


            return;

            if (!InternManager.Instance.IsPlayerIntern(player.GetComponent<PlayerControllerB>()))
            {
                return;
            }

            // 1st level dictionnary
            if (DictInternAvatarUpdater == null)
            {
                DictInternAvatarUpdater = new Dictionary<GameObject, Dictionary<AvatarUpdater, InternAvatarUpdater>>();
            }

            if (!DictInternAvatarUpdater.ContainsKey(player))
            {
                var newAvatarIntern = new Dictionary<AvatarUpdater, InternAvatarUpdater>
                {
                    { __instance, new InternAvatarUpdater(__instance, ___playerModelRenderer, replacement) }
                };

                DictInternAvatarUpdater.Add(player, newAvatarIntern);
                return;
            }

            // 1st level found
            Dictionary<AvatarUpdater, InternAvatarUpdater> dictPlayerInternAvatarUpdater = DictInternAvatarUpdater[player];

            // Clean dictionnary 2nd level
            List<AvatarUpdater> avatarUpdatersToClean = new List<AvatarUpdater>();
            foreach (var internAvatarUpdater in dictPlayerInternAvatarUpdater)
            {
                if (internAvatarUpdater.Value.ReplacementModelRef == null)
                {
                    avatarUpdatersToClean.Add(internAvatarUpdater.Key);
                }
            }
            foreach (var avatarUpdaterToClean in avatarUpdatersToClean)
            {
                dictPlayerInternAvatarUpdater.Remove(avatarUpdaterToClean);
            }

            // 2nd level dictionnary
            if (dictPlayerInternAvatarUpdater == null)
            {
                dictPlayerInternAvatarUpdater = new Dictionary<AvatarUpdater, InternAvatarUpdater>();
            }

            if (!dictPlayerInternAvatarUpdater.ContainsKey(__instance))
            {
                dictPlayerInternAvatarUpdater.Add(__instance, new InternAvatarUpdater(__instance, ___playerModelRenderer, replacement));
            }
            else
            {
                dictPlayerInternAvatarUpdater[__instance] = new InternAvatarUpdater(__instance, ___playerModelRenderer, replacement);
            }

            //foreach(var a in DictInternAvatarUpdater)
            //{
            //    foreach(var b in a.Value)
            //    {
            //        Plugin.LogDebug($"{a.Key}|{b.Key}|{b.Value}");
            //    }
            //}
        }

        [HarmonyPatch("UpdateModel")]
        [HarmonyPrefix]
        static bool UpdateModel_Prefix(AvatarUpdater __instance,
                                       GameObject ___player,
                                       SkinnedMeshRenderer ___playerModelRenderer,
                                       Vector3 ___rootPositionOffset)
        {
            // Cull animations ?
            PlayerControllerB playerController = ___player.GetComponent<PlayerControllerB>();
            if (playerController == null)
            {
                return true;
            }

            InternAI? internAI = InternManager.Instance.GetInternAI((int)playerController.playerClientId);
            if (internAI != null
                && (!internAI.NpcController.BodyInFOV
                    || internAI.NpcController.RankDistanceLocalPlayerInFOV > Plugin.Config.MaxModelReplacementModelAnimatedInterns.Value))
            {
                // Intern
                Transform avatarTransformFromBoneName = __instance.GetAvatarTransformFromBoneName("spine");
                Transform playerTransformFromBoneName = __instance.GetPlayerTransformFromBoneName("spine");
                avatarTransformFromBoneName.position = playerTransformFromBoneName.position + playerTransformFromBoneName.TransformVector(___rootPositionOffset);
                return false;
            }

            if (internAI == null)
            {
                DeadBodyInfo deadBodyInfo = ___player.GetComponent<DeadBodyInfo>();
                if (deadBodyInfo == null)
                {
                    return true;
                }

                Vector3 ragdollPos = deadBodyInfo.transform.position;
                internAI = InternManager.Instance.GetInternAI((int)playerController.playerClientId);
                if (internAI == null)
                {
                    return true;
                }

                // Dead body of intern
                if (internAI.RagdollInternBody.IsRagdollBodyHeld())
                {
                    // Held intern
                    internAI.UpdateRagdollModelReplacement(__instance, ___rootPositionOffset, deadBodyInfo.transform.position, ___playerModelRenderer);

                }
                else
                {
                    // Corpse on the ground
                    internAI.UpdateRagdollModelReplacement(__instance, ___rootPositionOffset, deadBodyInfo.transform.position, ___playerModelRenderer);
                }
            }

            return true;


            if (DictInternAvatarUpdater == null)
            {
                return true;
            }

            if (!DictInternAvatarUpdater.ContainsKey(___player))
            {
                return true;
            }

            if (!DictInternAvatarUpdater[___player].ContainsKey(__instance))
            {
                return true;
            }

            InternAvatarUpdater internAU = DictInternAvatarUpdater[___player][__instance];
            internAU.AvatarTransformFromBoneName.position = internAU.PlayerTransformFromBoneName.position + internAU.PlayerTransformFromBoneName.TransformVector(___rootPositionOffset);

            // Cull animations ?
            //InternAI? internAI = InternManager.Instance.GetInternAI((int)___player.GetComponent<PlayerControllerB>().playerClientId);
            //if (internAI != null
            //    && internAI.NpcController.BodyInFOV
            //    && internAI.NpcController.RankDistanceLocalPlayerInFOV < Plugin.Config.MaxModelReplacementModelAnimatedInterns.Value)
            //{
            //    return false;
            //}

            foreach (InternAvatarUpdaterBones internAUB in internAU.InternAvatarUpdaterBones)
            {
                if (internAUB == null)
                {
                    continue;
                }

                if (internAUB.AvatarTransformFromBoneName2 == null)
                {
                    continue;
                }

                internAUB.AvatarTransformFromBoneName2.rotation = internAUB.Bone.rotation;
                if (internAUB.RotationOffsetComponent != null)
                {
                    internAUB.AvatarTransformFromBoneName2.rotation *= internAUB.RotationOffsetComponent.offset;
                }
            }

            return false;
        }
    }
}
