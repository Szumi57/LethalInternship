using LethalInternship.Core.Interns.AI.BT;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using Unity.Netcode;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI
{
    public partial class InternAI
    {
        public BTController BTController = null!;

        public IPointOfInterest? PointOfInterest = null!;
        public EnumCommandTypes CurrentCommand { get; private set; }
        public EnumCommandTypes PendingCommand { get; private set; }
        public EnumTempCommandFeedback TempCommandFeedback { get; private set; }

        private EnumVoicesState voiceToPlay;

        private float tempCommandFeedbackTimer;

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

            //Debug.Log($"{Npc.playerUsername} SetCommandToFollowPlayer {Environment.StackTrace}");
            SetCommand(EnumCommandTypes.FollowPlayer, playVoice ? EnumVoicesState.OrderedToFollow : EnumVoicesState.None);
            this.PointOfInterest = null;

            // AI
            BTController.ResetContextNewCommandFollowPlayer();
        }

        public void SetCommandToScavengingToShip()
        {
            SetCommand(EnumCommandTypes.ScavengingToShip, EnumVoicesState.NowScavenging);
            this.PointOfInterest = null;

            // AI
            BTController.ResetContextNewCommandToScavenging();
        }
        public void SetCommandToScavengingToCruiser()
        {
            SetCommand(EnumCommandTypes.ScavengingToCruiser, EnumVoicesState.NowScavenging);
            this.PointOfInterest = null;

            // AI
            BTController.ResetContextNewCommandToScavenging();
        }
        public void SetCommandToScavengingToGatheringPoint()
        {
            SetCommand(EnumCommandTypes.ScavengingToGatheringPoint, EnumVoicesState.NowScavenging);
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

        public void SetCommandToAttackEnemy(EnemyAI enemy)
        {
            SetCommand(EnumCommandTypes.Kill, EnumVoicesState.None);
            this.PointOfInterest = null;

            // AI
            BTController.ResetContextAttackEnemy(enemy);
        }

        public void SetCommandToDropToShip()
        {
            Transform? shipTransform = InternManager.Instance.ShipTransform;
            if (shipTransform == null)
            {
                PluginLoggerHook.LogError?.Invoke($"{Npc.playerUsername} SetCommandToDropToShip shipTransform not found !");
                return;
            }
            if (this.AreHandsFree())
            {
                PluginLoggerHook.LogDebug?.Invoke($"{Npc.playerUsername} SetCommandToDropToShip but no items held = SetCommandToFollowPlayer");
                SetCommandToFollowPlayer(playVoice: false);
                return;
            }

            // SetCommand DropAllItemsToShip
            SetCommand(EnumCommandTypes.DropAllItemsToShip, EnumVoicesState.NowScavenging);
            this.PointOfInterest = null;

            // AI
            IPointOfInterest pointOfInterest = InternManager.Instance.GetPointOfInterestOrNewShipPoint(shipTransform);
            BTController.ResetContextNewCommandDropToPos(pointOfInterest);
        }
        public void SetCommandToDropToGatheringPoint()
        {
            if (InternManager.Instance.GatheringPoint == null)
            {
                PluginLoggerHook.LogError?.Invoke($"{Npc.playerUsername} SetCommandToDropToGatheringPoint no gathering point set !");
                return;
            }
            if (this.AreHandsFree())
            {
                PluginLoggerHook.LogDebug?.Invoke($"{Npc.playerUsername} SetCommandToDropToGatheringPoint but no items held = SetCommandToFollowPlayer");
                SetCommandToFollowPlayer(playVoice: false);
                return;
            }

            // SetCommand DropAllItemsOnGatheringPoint
            SetCommand(EnumCommandTypes.DropAllItemsOnGatheringPoint, EnumVoicesState.NowScavenging);
            this.PointOfInterest = null;

            // AI
            BTController.ResetContextNewCommandDropToPos(InternManager.Instance.GatheringPoint);
        }
        public void SetCommandToDropToCruiser()
        {
            SetCommand(EnumCommandTypes.DropAllItemsInCruiser, EnumVoicesState.NowScavenging);
            this.PointOfInterest = null;

            // AI
            BTController.ResetContextNewCommandDropToCruiser();
        }

        public void SetCommandToUnloadFromCruiser()
        {
            SetCommand(EnumCommandTypes.UnloadCruiser, EnumVoicesState.NowScavenging);
            this.PointOfInterest = null;

            // AI
            BTController.ResetContextNewCommandUnloadFromCruiser();
        }
        public void SetCommandToUnloadFromGatheringPoint()
        {
            if (InternManager.Instance.GatheringPoint == null)
            {
                PluginLoggerHook.LogError?.Invoke($"{Npc.playerUsername} SetCommandToUnloadFromGatheringPoint no gathering point set !");
                return;
            }

            SetCommand(EnumCommandTypes.UnloadGatheringPoint, EnumVoicesState.NowScavenging);
            this.PointOfInterest = null;

            // AI
            BTController.ResetContextNewCommandUnloadFromPos(InternManager.Instance.GatheringPoint);
        }


        private void SetCommand(EnumCommandTypes command, EnumVoicesState voiceCommand)
        {
            if (CurrentCommand == EnumCommandTypes.WaitForCommand)
            {
                PluginLoggerHook.LogDebug?.Invoke($"{Npc.playerUsername} SetPendingCommand {command}");
                PendingCommand = command;
                voiceToPlay = voiceCommand;
            }
            else
            {
                PluginLoggerHook.LogDebug?.Invoke($"{Npc.playerUsername} SetCurrentCommand {command}");
                CurrentCommand = command;
                PlayVoiceAfterCommand(voiceCommand);

                internIdentity.OnCommandChanged?.Invoke(internIdentity);
            }
        }

        public void SetCommandToWaitForCommand(bool wait)
        {
            if (wait)
            {
                PendingCommand = CurrentCommand;
                CurrentCommand = EnumCommandTypes.WaitForCommand;
                //PluginLoggerHook.LogDebug?.Invoke($"SetCommandToWaitForCommand wait true");
            }
            else
            {
                CurrentCommand = PendingCommand;
                if (CurrentCommand == EnumCommandTypes.WaitForCommand
                    || CurrentCommand == EnumCommandTypes.None)
                {
                    //PluginLoggerHook.LogDebug?.Invoke($"SetCommandToWaitForCommand wait false, CurrentCommand {CurrentCommand} set to FollowPlayer");
                    CurrentCommand = EnumCommandTypes.FollowPlayer;
                }
                //PluginLoggerHook.LogDebug?.Invoke($"SetCommandToWaitForCommand wait false, new command {CurrentCommand}");

                // Voice
                PlayVoiceAfterCommand(voiceToPlay);

                internIdentity.OnCommandChanged?.Invoke(internIdentity);
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

        private void CheckTempCommandFeedbackTimer()
        {
            tempCommandFeedbackTimer += Time.deltaTime;
            if (tempCommandFeedbackTimer > 5f)
            {
                tempCommandFeedbackTimer = 0f;
                TempCommandFeedback = EnumTempCommandFeedback.None;
            }
        }

        public void SetCommandFeedback(EnumTempCommandFeedback commandFeedback)
        {
            TempCommandFeedback = commandFeedback;
            tempCommandFeedbackTimer = 0f;
        }

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

        public void OnCollisionWithCruiser()
        {
            this.BTController.ResetContext();

            VehicleController? vehicleController = InternManager.Instance.VehicleController;
            if (vehicleController == null)
                return;

            if (this.npcController.IsControllerInCruiser)
                return;

            if (this.CurrentCommand == EnumCommandTypes.GoToVehicle)
            {
                this.EnterCruiser(vehicleController);
                return;
            }

            if (this.CurrentCommand == EnumCommandTypes.ScavengingToCruiser
                && !this.AreFreeSlotsAvailable())
            {
                this.EnterCruiser(vehicleController);
                return;
            }

            if (this.CurrentCommand == EnumCommandTypes.UnloadCruiser
                && this.BTController.GetTargetItem() != null)
            {
                this.EnterCruiser(vehicleController);
                return;
            }

            if (this.CurrentCommand == EnumCommandTypes.DropAllItemsInCruiser)
            {
                this.EnterCruiser(vehicleController);
                return;
            }
        }
    }
}
