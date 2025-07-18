using GameNetcodeStuff;
using LethalInternship.SharedAbstractions.Hooks.TooManyEmotesHooks;
using LethalInternship.SharedAbstractions.Interns;
using TooManyEmotes;

namespace LethalInternship.Patches.ModPatches.TooManyEmotes
{
    public class TooManyEmotesUtils
    {
        public static void Init()
        {
            TooManyEmotesHook.CheckAndPerformTooManyEmote = CheckAndPerformTooManyEmote;
            TooManyEmotesHook.PerformTooManyEmote = PerformTooManyEmote;
            TooManyEmotesHook.StopPerformingTooManyEmote = StopPerformingTooManyEmote;
        }

        public static void CheckAndPerformTooManyEmote(IInternAI internAI, PlayerControllerB playerToMimic)
        {
            EmoteControllerPlayer emoteControllerPlayerOfplayerToMimic = playerToMimic.gameObject.GetComponent<EmoteControllerPlayer>();
            if (emoteControllerPlayerOfplayerToMimic == null)
            {
                return;
            }
            EmoteControllerPlayer emoteControllerIntern = internAI.NpcController.Npc.gameObject.GetComponent<EmoteControllerPlayer>();
            if (emoteControllerIntern == null)
            {
                return;
            }

            // Player performing emote but not tooManyEmote so default
            if (!emoteControllerPlayerOfplayerToMimic.isPerformingEmote)
            {
                if (emoteControllerIntern.isPerformingEmote)
                {
                    emoteControllerIntern.StopPerformingEmote();
                    internAI.StopPerformTooManyEmoteInternServerRpc();
                }

                // Default emote
                internAI.NpcController.PerformDefaultEmote(playerToMimic.playerBodyAnimator.GetInteger("emoteNumber"));
                return;
            }

            // TooMany emotes
            if (emoteControllerPlayerOfplayerToMimic.performingEmote == null)
            {
                return;
            }

            if (emoteControllerIntern.isPerformingEmote
                && emoteControllerPlayerOfplayerToMimic.performingEmote.emoteId == emoteControllerIntern.performingEmote?.emoteId)
            {
                return;
            }

            // PerformEmote TooMany emote
            internAI.PerformTooManyEmoteInternServerRpc(emoteControllerPlayerOfplayerToMimic.performingEmote.emoteId);
        }

        public static void PerformTooManyEmote(INpcController npcController, int tooManyEmoteID)
        {
            EmoteControllerPlayer emoteControllerIntern = npcController.Npc.gameObject.GetComponent<EmoteControllerPlayer>();
            if (emoteControllerIntern == null)
            {
                return;
            }

            if (emoteControllerIntern.isPerformingEmote)
            {
                emoteControllerIntern.StopPerformingEmote();
            }

            UnlockableEmote unlockableEmote = EmotesManager.allUnlockableEmotes[tooManyEmoteID];
            emoteControllerIntern.PerformEmote(unlockableEmote);
        }

        public static void StopPerformingTooManyEmote(INpcController npcController)
        {
            EmoteControllerPlayer emoteControllerInternController = npcController.Npc.gameObject.GetComponent<EmoteControllerPlayer>();
            if (emoteControllerInternController != null)
            {
                emoteControllerInternController.StopPerformingEmote();
            }
        }
    }
}
