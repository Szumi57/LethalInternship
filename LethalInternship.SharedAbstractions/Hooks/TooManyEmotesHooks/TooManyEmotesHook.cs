using GameNetcodeStuff;
using LethalInternship.SharedAbstractions.Interns;

namespace LethalInternship.SharedAbstractions.Hooks.TooManyEmotesHooks
{
    public delegate void CheckAndPerformTooManyEmoteDelegate(IInternAI internAI, PlayerControllerB playerToMimic);
    public delegate void PerformTooManyEmoteDelegate(INpcController npcController, int tooManyEmoteID);
    public delegate void StopPerformingTooManyEmoteDelegate(INpcController npcController);

    public class TooManyEmotesHook
    {
        public static CheckAndPerformTooManyEmoteDelegate? CheckAndPerformTooManyEmote;
        public static PerformTooManyEmoteDelegate? PerformTooManyEmote;
        public static StopPerformingTooManyEmoteDelegate? StopPerformingTooManyEmote;
    }
}
