using LethalInternship.Core.Interns.AI.BT;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.Parameters;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;

namespace LethalInternship.Core.Interns.AI
{
    public partial class InternAI
    {
        public BTController BTController = null!;

        public IPointOfInterest? PointOfInterest = null!;
        public EnumCommandTypes CurrentCommand;

        #region Commands

        public IPointOfInterest? GetPointOfInterest()
        {
            if (InternManager.Instance.CheckAndClearInvalidPointOfInterest(this.PointOfInterest))
            {
                this.PointOfInterest = null;
                return null;
            }

            return this.PointOfInterest;
        }

        public void SetCommandTo(IPointOfInterest pointOfInterest)
        {
            if (!CanGiveOrder())
            {
                return;
            }

            this.PointOfInterest = pointOfInterest;

            EnumCommandTypes? newCommand = pointOfInterest.GetCommand();
            if (newCommand == null)
            {
                SetCommandToFollowPlayer();
                return;
            }
            CurrentCommand = newCommand.Value;

            PluginLoggerHook.LogDebug?.Invoke($"SetCommandTo {CurrentCommand}");
            PluginLoggerHook.LogDebug?.Invoke($"VVV PointOfInterest VVV");
            foreach (var p in this.PointOfInterest.GetListInterestPoints())
            {
                PluginLoggerHook.LogDebug?.Invoke($"Interest point {p.GetType()}");
            }

            // AI
            BTController.ResetContextNewCommandToInterestPoint(pointOfInterest);

            // Voice
            TryPlayCurrentOrderVoiceAudio(EnumVoicesState.OrderedToGoThere);
        }

        public void SetCommandToFollowPlayer()
        {
            if (!CanGiveOrder())
            {
                return;
            }

            if (this.targetPlayer == null)
            {
                PluginLoggerHook.LogWarning?.Invoke($"{Npc.playerUsername} no target player assigned, wait for someone to manage this intern before giving commands.");
                return;
            }

            PluginLoggerHook.LogDebug?.Invoke($"{Npc.playerUsername} SetCommandToFollowPlayer, before {CurrentCommand}");
            CurrentCommand = EnumCommandTypes.FollowPlayer;
            this.PointOfInterest = null;

            // AI
            BTController.ResetContextNewCommandFollowPlayer();

            // Voice
            TryPlayCurrentOrderVoiceAudio(EnumVoicesState.OrderedToFollow);
        }

        public void SetCommandToScavenging()
        {
            if (!CanGiveOrder())
            {
                return;
            }

            CurrentCommand = EnumCommandTypes.ScavengingMode;
            this.PointOfInterest = null;
            PluginLoggerHook.LogDebug?.Invoke($"SetCommandToScavengingMode");

            // AI
            BTController.ResetContextNewCommandToScavenging();

            // Voice
            //TryPlayCurrentOrderVoiceAudio(EnumVoicesState.OrderedToFollow);
        }

        private void TryPlayCurrentOrderVoiceAudio(EnumVoicesState enumVoicesState)
        {
            // Default states, wait for cooldown and if no one is talking close
            this.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = enumVoicesState,
                CanTalkIfOtherInternTalk = false,
                WaitForCooldown = false,
                CutCurrentVoiceStateToTalk = true,
                CanRepeatVoiceState = false,

                ShouldSync = true,
                IsInternInside = this.NpcController.Npc.isInsideFactory,
                AllowSwearing = PluginRuntimeProvider.Context.Config.AllowSwearing
            });
        }

        private bool CanGiveOrder()
        {
            if (!this.IsSpawned
                || this.IsEnemyDead
                || this.NpcController == null
                || this.NpcController.Npc == null
                || this.NpcController.Npc.isPlayerDead
                || !this.NpcController.Npc.isPlayerControlled
                || this.InternIdentity.Status != EnumStatusIdentity.Spawned
                || this.IsSpawningAnimationRunning())
            {
                return false;
            }

            return true;
        }

        #endregion
    }
}
