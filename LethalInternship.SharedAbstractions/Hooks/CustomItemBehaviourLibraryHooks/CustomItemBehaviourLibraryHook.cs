namespace LethalInternship.SharedAbstractions.Hooks.CustomItemBehaviourLibraryHooks
{
    public delegate bool IsGrabbableObjectInContainerModDelegate(GrabbableObject grabbableObject);

    public class CustomItemBehaviourLibraryHook
    {

        public static IsGrabbableObjectInContainerModDelegate? IsGrabbableObjectInContainerMod;
    }
}
