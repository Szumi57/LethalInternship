namespace LethalInternship.SharedAbstractions.Hooks.BunkbedReviveHooks
{
    public delegate void UpdateReviveCountDelegate(int id);

    public class BunkbedReviveHook
    {
        public static UpdateReviveCountDelegate? UpdateReviveCount;
    }
}
