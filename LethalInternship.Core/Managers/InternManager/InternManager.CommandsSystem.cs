using LethalInternship.Core.CommandsSystem;
using LethalInternship.Core.CommandsSystem.Abilities;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LethalInternship.Core.Managers
{
    public partial class InternManager
    {
        public void ExecuteOrder(Order order)
        {
            Debug.Log("========================================ExecuteOrder");
            foreach (var a in order.IdentitiesToOrder)
            {
                Debug.Log(a);
            }
            Debug.Log("==============================================");

            var internsOwned = order.IdentitiesToOrder
                                .Where(x => IdentityManager.Instance.IsIdentityValidToCommand(x))
                                .Select(x => x.InternAI!);
            foreach (IInternAI intern in internsOwned)
            {
                intern.AssignOrder(order);
            }
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
