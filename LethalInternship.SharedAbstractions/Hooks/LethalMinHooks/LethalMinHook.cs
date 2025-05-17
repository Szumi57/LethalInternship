namespace LethalInternship.SharedAbstractions.Hooks.LethalMinHooks
{
    public delegate bool IsGrabbableObjectHeldByPikminModDelegate(GrabbableObject grabbableObject);

    public class LethalMinHook
    {
        public static IsGrabbableObjectHeldByPikminModDelegate? IsGrabbableObjectHeldByPikminMod;
    }
}
