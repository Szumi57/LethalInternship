using CustomItemBehaviourLibrary.AbstractItems;
using LethalInternship.SharedAbstractions.Hooks.CustomItemBehaviourLibraryHooks;

namespace LethalInternship.Patches.ModPatches.CustomItemBehaviourLibrary
{
    public class CustomItemBehaviourLibraryUtils
    {
        public static void Init()
        {
            CustomItemBehaviourLibraryHook.IsGrabbableObjectInContainerMod = IsGrabbableObjectInContainerMod;
        }

        public static bool IsGrabbableObjectInContainerMod(GrabbableObject grabbableObject)
        {
            return ContainerBehaviour.CheckIfItemInContainer(grabbableObject);
        }
    }
}
