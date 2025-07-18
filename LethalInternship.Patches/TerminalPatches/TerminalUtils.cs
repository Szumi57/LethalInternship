using LethalInternship.SharedAbstractions.Hooks.TerminalHooks;

namespace LethalInternship.Patches.TerminalPatches
{
    public class TerminalUtils
    {
        public static void Init()
        {
            TerminalHook.RemovePunctuation_ReversePatch = TerminalPatch.RemovePunctuation_ReversePatch;
        }
    }
}
