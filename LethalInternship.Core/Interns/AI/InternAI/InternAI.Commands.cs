using LethalInternship.Core.Interns.AI.BT;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using Unity.Netcode;

namespace LethalInternship.Core.Interns.AI
{
    public partial class InternAI
    {
        public BTController BTController = null!;

        public IPointOfInterest? PointOfInterest = null!;
        public EnumCommandTypes CurrentCommand { get; private set; }

        private EnumCommandTypes pendingCommand;
        private EnumVoicesState voiceToPlay;

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

        public void AssignOrder(Order order)
        {
            order.ApplyTo(this);
        }

        public void SetCommandTo(IPointOfInterest pointOfInterest, bool playVoice = true)
        {
            this.PointOfInterest = pointOfInterest;

            EnumCommandTypes? newCommand = pointOfInterest.GetCommand();
            if (newCommand == null)
            {
                SetCommandToFollowPlayer();
                return;
            }

            SetCommand(newCommand.Value, playVoice ? EnumVoicesState.OrderedToGoThere : EnumVoicesState.None);

            PluginLoggerHook.LogDebug?.Invoke($"VVV PointOfInterest VVV");
            foreach (var p in this.PointOfInterest.GetListInterestPoints())
            {
                PluginLoggerHook.LogDebug?.Invoke($"Interest point {p.GetType()}");
            }

            // AI
            BTController.ResetContextNewCommandToInterestPoint(pointOfInterest);
        }

        public void SetCommandToFollowPlayer(bool playVoice = true)
        {
            if (this.targetPlayer == null)
            {
                PluginLoggerHook.LogWarning?.Invoke($"{Npc.playerUsername} no target player assigned, wait for someone to manage this intern before giving commands.");
                return;
            }

            SetCommand(EnumCommandTypes.FollowPlayer, playVoice ? EnumVoicesState.OrderedToFollow : EnumVoicesState.None);
            this.PointOfInterest = null;

            // AI
            BTController.ResetContextNewCommandFollowPlayer();
        }

        public void SetCommandToScavenging()
        {
            SetCommand(EnumCommandTypes.ScavengingMode, EnumVoicesState.NowScavenging);
            this.PointOfInterest = null;

            // AI
            BTController.ResetContextNewCommandToScavenging();
        }

        public void SetCommandToFetchItem(GrabbableObject itemToFetch)
        {
            SetCommand(EnumCommandTypes.GoFetchItem, EnumVoicesState.OrderedToGoThere);
            this.PointOfInterest = null;

            // AI
            BTController.ResetContextNewCommandGoFetchItem(itemToFetch);
        }

        private void SetCommand(EnumCommandTypes command, EnumVoicesState voiceCommand)
        {
            if (CurrentCommand == EnumCommandTypes.WaitForCommand)
            {
                PluginLoggerHook.LogDebug?.Invoke($"SetPendingCommand {command}");
                pendingCommand = command;
                voiceToPlay = voiceCommand;
            }
            else
            {
                PluginLoggerHook.LogDebug?.Invoke($"SetCurrentCommand {command}");
                CurrentCommand = command;
                PlayVoiceAfterCommand(voiceCommand);
            }
        }

        public void SetCommandToWaitForCommand(bool wait)
        {
            if (wait)
            {
                pendingCommand = CurrentCommand;
                CurrentCommand = EnumCommandTypes.WaitForCommand;
                PluginLoggerHook.LogDebug?.Invoke($"SetCommandToWaitForCommand wait true");
            }
            else
            {
                CurrentCommand = pendingCommand;
                if (CurrentCommand == EnumCommandTypes.WaitForCommand
                    || CurrentCommand == EnumCommandTypes.None)
                {
                    PluginLoggerHook.LogDebug?.Invoke($"SetCommandToWaitForCommand wait false, CurrentCommand {CurrentCommand} set to FollowPlayer");
                    CurrentCommand = EnumCommandTypes.FollowPlayer;
                }
                PluginLoggerHook.LogDebug?.Invoke($"SetCommandToWaitForCommand wait false, new command {CurrentCommand}");

                // Voice
                PlayVoiceAfterCommand(voiceToPlay);
            }
        }

        private void PlayVoiceAfterCommand(EnumVoicesState voice)
        {
            if (voice != EnumVoicesState.None)
            {
                TryPlayCurrentOrderVoiceAudio(voiceToPlay);
            }
        }

        #endregion

        public void HitTargetWithShovel(Shovel shovel)
        {
            EnemyAI? enemyAI = BTController.GetTarget();
            if (enemyAI == null)
            {
                PluginLoggerHook.LogWarning?.Invoke($"HitTargetWithShovel, no target found");
                return;
            }

            enemyAI.HitEnemyOnLocalClient(force: shovel.shovelHitForce,
                                          hitDirection: this.Npc.gameplayCamera.transform.forward,
                                          playerWhoHit: this.Npc,
                                          playHitSFX: true,
                                          hitID: 1);

            RoundManager.PlayRandomClip(shovel.shovelAudio, shovel.hitSFX, true, 1f, 0, 1000);
            RoundManager.Instance.PlayAudibleNoise(base.transform.position, 17f, 0.8f, 0, false, 0);
            this.Npc.playerBodyAnimator.SetTrigger("shovelHit");
            shovel.HitShovelServerRpc(-1);
        }

        public void HitTargetWithKnife(KnifeItem knife)
        {
            EnemyAI? enemyAI = BTController.GetTarget();
            if (enemyAI == null)
            {
                PluginLoggerHook.LogWarning?.Invoke($"HitTargetWithKnife, no target found");
                return;
            }

            enemyAI.HitEnemyOnLocalClient(force: knife.knifeHitForce,
                                          hitDirection: this.Npc.gameplayCamera.transform.forward,
                                          playerWhoHit: this.Npc,
                                          playHitSFX: true,
                                          hitID: 1);

            RoundManager.PlayRandomClip(knife.knifeAudio, knife.hitSFX, true, 1f, 0, 1000);
            RoundManager.Instance.PlayAudibleNoise(base.transform.position, 17f, 0.8f, 0, false, 0);
            knife.HitShovelServerRpc(-1);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetAutoDefenseModeServerRpc(bool autoDefense)
        {
            SetAutoDefenseModeClientRpc(autoDefense);
        }

        [ClientRpc]
        private void SetAutoDefenseModeClientRpc(bool autoDefense)
        {
            this.InternIdentity.SetAutoDefense(autoDefense);
        }
    }
}
