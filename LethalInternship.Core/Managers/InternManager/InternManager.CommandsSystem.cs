using GameNetcodeStuff;
using LethalInternship.Core.CommandsSystem;
using LethalInternship.Core.CommandsSystem.Abilities;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System.Linq;
using Unity.Netcode;

namespace LethalInternship.Core.Managers
{
    public partial class InternManager
    {
        public void ExecuteOrder(Order order)
        {
            var internsOwned = order.IdentitiesToOrder
                                .Where(x => IdentityManager.Instance.IsIdentityValidToCommand(x))
                                .Select(x => x.InternAI!);
            foreach (IInternAI intern in internsOwned)
            {
                intern.AssignOrder(order);
            }
        }

        public int GetMaxDistanceCommand()
        {
            if (StartOfRound.Instance == null) return 0;
            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;
            if (localPlayer == null
                || localPlayer.isPlayerDead) return 0;

            int maxDistance;
            if (localPlayer.isInsideFactory)
            {
                maxDistance = PluginRuntimeProvider.Context.Config.InsideRangeCommands;
            }
            else
            {
                maxDistance = PluginRuntimeProvider.Context.Config.OutsideRangeCommands;
            }

            return maxDistance * maxDistance;
        }

        [ServerRpc(RequireOwnership = false)]
        public void GlobalCommandServerRpc(EnumInputAction enumInputAction)
        {
            GlobalCommandClientRpc(enumInputAction);
        }

        [ClientRpc]
        private void GlobalCommandClientRpc(EnumInputAction enumInputAction)
        {
            switch (enumInputAction)
            {
                case EnumInputAction.GoToShip:
                    IdentitySelectionService.Instance.Refresh(IdentityManager.Instance.GetIdentitiesSpawned());
                    IdentitySelectionService.Instance.SelectAll();
                    new GoToShipAbility(IdentitySelectionService.Instance.GetSelected()).Activate();
                    break;

                default:
                    PluginLoggerHook.LogError?.Invoke($"GlobalCommand action {enumInputAction} not implemented !");
                    break;
            }
        }
    }
}
