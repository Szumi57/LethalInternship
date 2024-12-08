using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using LethalInternship.Utils;
using ModelReplacement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine.Analytics;

namespace LethalInternship.Patches.ModPatches.ModelRplcmntAPI
{
    [HarmonyPatch(typeof(ModelReplacementAPI))]
    internal class ModelReplacementAPIPatch
    {
        [HarmonyPatch("SetPlayerModelReplacement")]
        [HarmonyPrefix]
        static bool SetPlayerModelReplacement_Prefix(PlayerControllerB player, Type type)
        {
            InternAI? internAI = InternManager.Instance.GetInternAI((int)player.playerClientId);
            if (internAI == null)
            {
                return true;
            }

            if (!type.IsSubclassOf(typeof(BodyReplacementBase)))
            {
                return true;
            }

            int currentSuitID = player.currentSuitID;
            string unlockableName = StartOfRound.Instance.unlockablesList.unlockables[currentSuitID].unlockableName;
            BodyReplacementBase[] bodiesReplacementBase = player.gameObject.GetComponents<BodyReplacementBase>();
            if (bodiesReplacementBase.Length == 0)
            {
                ModelReplacementAPI.Instance.Logger.LogInfo(string.Format("Suit Change detected {0} => {1}, Replacing {2}.", null, unlockableName, type));
                BodyReplacementBase bodyReplacementBase2 = (BodyReplacementBase)player.gameObject.AddComponent(type);
                bodyReplacementBase2.suitName = unlockableName;
            }
            else
            {
                foreach (BodyReplacementBase bodyReplacementBase in bodiesReplacementBase)
                {
                    if (BodyReplacementBasePatch.ListBodyReplacementOnDeadBodies.Contains(bodyReplacementBase))
                    {
                        continue;
                    }

                    if (bodyReplacementBase.GetType() == type && bodyReplacementBase.suitName == unlockableName)
                    {
                        continue;
                    }

                    ModelReplacementAPI.Instance.Logger.LogInfo(string.Format("Model Replacement Change detected {0} => {1}, changing model.", bodyReplacementBase.GetType(), type));
                    bodyReplacementBase.IsActive = false;
                    UnityEngine.Object.Destroy(bodyReplacementBase);

                    ModelReplacementAPI.Instance.Logger.LogInfo(string.Format("Suit Change detected {0} => {1}, Replacing {2}.", (bodyReplacementBase != null) ? bodyReplacementBase.suitName : null, unlockableName, type));
                    BodyReplacementBase bodyReplacementBase2 = (BodyReplacementBase)player.gameObject.AddComponent(type);
                    bodyReplacementBase2.suitName = unlockableName;
                }
            }

            return false;
        }

        [HarmonyPatch("RemovePlayerModelReplacement")]
        [HarmonyPrefix]
        static bool RemovePlayerModelReplacement_Prefix(PlayerControllerB player)
        {
            InternAI? internAI = InternManager.Instance.GetInternAI((int)player.playerClientId);
            if (internAI == null)
            {
                return true;
            }

            BodyReplacementBase component = player.gameObject.GetComponent<BodyReplacementBase>();
            if (BodyReplacementBasePatch.ListBodyReplacementOnDeadBodies.Contains(component))
            {
                return false;
            }
            return true;
        }

        //[HarmonyPatch("SetPlayerModelReplacement")]
        //[HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> SetPlayerModelReplacement_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 23; i++)
            {
                if (codes[i].ToString().StartsWith("call static bool ModelReplacement.ModelReplacementAPI::get_IsLan()") // 21
                    && codes[i + 23].ToString().StartsWith("ldarg.0 NULL")) // 44
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                Label labelToJumpTo = generator.DefineLabel();
                codes[startIndex + 23].labels.Add(labelToJumpTo);

                // Adds dummy line for label that land here
                codes.Insert(startIndex + 1, new CodeInstruction(codes[startIndex].opcode, codes[startIndex].operand));
                codes[startIndex].opcode = OpCodes.Nop;
                codes[startIndex].operand = null;
                startIndex++;

                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call, PatchesUtil.IsPlayerInternMethod),
                    new CodeInstruction(OpCodes.Brtrue, labelToJumpTo),
                };
                codes.InsertRange(startIndex, codesToAdd);

                startIndex = -1;
            }
            else
            {
                Plugin.LogError($"LethalInternship.Patches.ModPatches.ModelRplcmntAPI.ModelReplacementAPIPatch.SetPlayerModelReplacement_Transpiler could not bypass is lan and player steam id 0 when intern.");
            }

            return codes.AsEnumerable();
        }
    }
}
