using HarmonyLib;
using LethalInternship.Managers;
using LethalInternship.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace LethalInternship.Patches.ObjectsPatches
{
    [HarmonyPatch(typeof(GrabbableObject))]
    internal class GrabbableObjectPatch
    {
        static MethodInfo IsObjectHeldByInternMethodInfo = SymbolExtensions.GetMethodInfo(() => PatchesUtil.IsObjectHeldByIntern((GrabbableObject)new object()));

        [HarmonyPatch("SetControlTipsForItem")]
        [HarmonyPrefix]
        static bool SetControlTipsForItem_PreFix(GrabbableObject __instance)
        {
            return !InternManager.Instance.IsObjectHeldByIntern(__instance);
        }

        [HarmonyPatch("LateUpdate")]
        [HarmonyPrefix]
        static bool LateUpdate_PreFix(ref GrabbableObject __instance)
        {
            return true;
            //if (__instance.parentObject != null)
            //{
            //    __instance.parentObject.GetComponentsInParent<Transform>().First(x => x.name.StartsWith("Player")).transform.localScale = Vector3.one;
            //    Plugin.Logger.LogDebug($"prefix rot {__instance.parentObject.rotation}, {__instance.itemProperties.rotationOffset}");
            //    Plugin.Logger.LogDebug($"prefix pos {__instance.parentObject.position}, {__instance.itemProperties.positionOffset}");
            //    Plugin.Logger.LogDebug($"prefix {__instance.parentObject.gameObject.name}, {__instance.parentObject.parent?.gameObject.name}, {__instance.parentObject.parent?.parent?.gameObject.name}");

            //    __instance.transform.rotation = __instance.parentObject.rotation;
            //    __instance.transform.Rotate(__instance.itemProperties.rotationOffset);
            //    __instance.transform.position = __instance.parentObject.position;
            //    Vector3 vector = __instance.itemProperties.positionOffset;
            //    vector = __instance.parentObject.rotation * vector;
            //    __instance.transform.position += vector;

            //    var handpos = __instance.parentObject.GetComponentsInParent<Transform>().First(x => x.name.StartsWith("hand.R")).transform.position;
            //    Plugin.Logger.LogDebug($"dist holder/hand {(__instance.parentObject.position - handpos).magnitude}");
            //    Plugin.Logger.LogDebug($"dist object/hand {(__instance.transform.position - handpos).magnitude}");
            //}
            //__instance.parentObject.GetComponentsInParent<Transform>().First(x => x.name.StartsWith("hand.R")).transform;

            //return false;
            //if (__instance.parentObject != null)
            //{

            //Plugin.Logger.LogDebug($"{__instance.itemProperties.positionOffset}, {__instance.itemProperties.positionOffset}");
            //}
        }

        [HarmonyPatch("EquipItem")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> EquipItem_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 3; i++)
            {
                if (codes[i].ToString() == "call static HUDManager HUDManager::get_Instance()"//3
                    && codes[i + 1].ToString() == "callvirt void HUDManager::ClearControlTips()"
                    && codes[i + 3].ToString() == "callvirt virtual void GrabbableObject::SetControlTipsForItem()")
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call, IsObjectHeldByInternMethodInfo),
                    new CodeInstruction(OpCodes.Brtrue_S, codes[startIndex + 4].labels.First()/*Label1*/)
                };
                codes.InsertRange(startIndex, codesToAdd);
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.ObjectsPatches.EquipItem_Transpiler could not remove check if holding player is intern");
            }

            //Plugin.Logger.LogDebug($"EquipItem ======================");
            //for (var i = 0; i < codes.Count; i++)
            //{
            //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
            //}
            //Plugin.Logger.LogDebug($"EquipItem ======================");

            return codes.AsEnumerable();
        }

        [HarmonyPatch("LateUpdate")]
        [HarmonyPostfix]
        static void LateUpdate_PostFix(ref GrabbableObject __instance)
        {
            if (__instance.parentObject != null)
            {

                //__instance.parentObject.GetComponentsInParent<Transform>().First(x => x.name.StartsWith("Player")).transform.localScale *= 0.85f;
                //Plugin.Logger.LogDebug($"prefix rot {__instance.parentObject.rotation}, {__instance.itemProperties.rotationOffset}");
                //Plugin.Logger.LogDebug($"prefix pos {__instance.parentObject.position}, {__instance.itemProperties.positionOffset}");
                //Plugin.Logger.LogDebug($"prefix {__instance.parentObject.gameObject.name}, {__instance.parentObject.parent?.gameObject.name}, {__instance.parentObject.parent?.parent?.gameObject.name}");
            }
            //if (__instance.parentObject != null)
            //{

            //Plugin.Logger.LogDebug($"{__instance.itemProperties.positionOffset}, {__instance.itemProperties.positionOffset}");
            //}
        }


    }
}
