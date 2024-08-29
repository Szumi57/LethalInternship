using HarmonyLib;
using System;
using System.Runtime.CompilerServices;

namespace LethalInternship.Patches.ObjectsPatches
{
    [HarmonyPatch(typeof(GrabbableObject))]
    internal class GrabbableObjectPatch
    {
        [HarmonyReversePatch]
        [HarmonyPatch(nameof(GrabbableObject.Update))]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void GrabbableObject_Update_ReversePatch(RagdollGrabbableObject instance) => throw new NotImplementedException("Stub LethalInternship.Patches.NpcPatches.GrabbableObjectPatch.GrabbableObject_Update_ReversePatch");
    }
}
