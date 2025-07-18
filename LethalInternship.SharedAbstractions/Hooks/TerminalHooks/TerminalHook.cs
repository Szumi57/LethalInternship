namespace LethalInternship.SharedAbstractions.Hooks.TerminalHooks
{
    public delegate string RemovePunctuation_ReversePatchDelegate(object instance, string s);

    public class TerminalHook
    {
        public static RemovePunctuation_ReversePatchDelegate? RemovePunctuation_ReversePatch;
    }
}
