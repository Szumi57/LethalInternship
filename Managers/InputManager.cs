using GameNetcodeStuff;
using LethalInternship.AI;
using LethalInternship.Patches.NpcPatches;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LethalInternship.Managers
{
    internal class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; } = null!;


        private void Awake()
        {
            Instance = this;

            Plugin.InputActionsInstance.LeadIntern.performed += LeadIntern_performed;
            Plugin.InputActionsInstance.GiveTakeItem.performed += GiveTakeItem_performed;
            Plugin.InputActionsInstance.GrabIntern.performed += GrabIntern_performed;
            Plugin.InputActionsInstance.ReleaseInterns.performed += ReleaseInterns_performed;
            Plugin.InputActionsInstance.ChangeSuitIntern.performed += ChangeSuitIntern_performed;
        }

        public string GetKeyAction(InputAction inputAction)
        {
            int bindingIndex;
            if (StartOfRound.Instance.localPlayerUsingController)
            {
                // Gamepad
                bindingIndex = inputAction.GetBindingIndex(InputBinding.MaskByGroup("Gamepad"));
            }
            else
            {
                // kbm
                bindingIndex = inputAction.GetBindingIndex(InputBinding.MaskByGroup("KeyboardAndMouse"));
            }
            return inputAction.GetBindingDisplayString(bindingIndex);
        }

        private bool IsPerformedValid(PlayerControllerB localPlayer)
        {
            if (!localPlayer.IsOwner
                || localPlayer.isPlayerDead
                || !localPlayer.isPlayerControlled)
            {
                return false;
            }

            if (localPlayer.isGrabbingObjectAnimation
                || localPlayer.isTypingChat
                || localPlayer.inTerminalMenu
                || localPlayer.IsInspectingItem)
            {
                return false;
            }
            if (localPlayer.inAnimationWithEnemy != null)
            {
                return false;
            }
            if (localPlayer.jetpackControls || localPlayer.disablingJetpackControls)
            {
                return false;
            }
            if (StartOfRound.Instance.suckingPlayersOutOfShip)
            {
                return false;
            }

            if (localPlayer.hoveringOverTrigger != null)
            {
                if (localPlayer.hoveringOverTrigger.holdInteraction
                    || !PlayerControllerBPatch.InteractTriggerUseConditionsMet_ReversePatch(localPlayer))
                {
                    return false;
                }
            }

            return true;
        }


        private void LeadIntern_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;
            if (!IsPerformedValid(localPlayer))
            {
                return;
            }

            // Use of interact key to assign intern to player
            Ray interactRay = new Ray(localPlayer.gameplayCamera.transform.position, localPlayer.gameplayCamera.transform.forward);
            RaycastHit[] raycastHits = Physics.RaycastAll(interactRay, localPlayer.grabDistance, Const.PLAYER_MASK);
            foreach (RaycastHit hit in raycastHits)
            {
                if (hit.collider.tag != "Player")
                {
                    continue;
                }

                PlayerControllerB player = hit.collider.gameObject.GetComponent<PlayerControllerB>();
                if (player == null)
                {
                    continue;
                }
                InternAI? intern = InternManager.Instance.GetInternAI((int)player.playerClientId);
                if (intern == null)
                {
                    continue;
                }

                if (intern.OwnerClientId != localPlayer.actualClientId)
                {
                    intern.SyncAssignTargetAndSetMovingTo(localPlayer);

                    if (Plugin.Config.AutoChangeSuit.Value)
                    {
                        intern.ChangeSuitInternServerRpc(player.playerClientId, localPlayer.currentSuitID);
                    }
                }

                //HUDManager.Instance.ClearControlTips();
                //HUDManager.Instance.ChangeControlTipMultiple(new string[] { Const.TOOLTIPS_ORDER_1 });
                return;
            }
        }

        private void GiveTakeItem_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;
            if (!IsPerformedValid(localPlayer))
            {
                return;
            }

            // Make an intern drop his object
            Ray interactRay = new Ray(localPlayer.gameplayCamera.transform.position, localPlayer.gameplayCamera.transform.forward);
            RaycastHit[] raycastHits = Physics.RaycastAll(interactRay, localPlayer.grabDistance, Const.PLAYER_MASK);
            foreach (RaycastHit hit in raycastHits)
            {
                if (hit.collider.tag != "Player")
                {
                    continue;
                }

                PlayerControllerB internController = hit.collider.gameObject.GetComponent<PlayerControllerB>();
                if (internController == null)
                {
                    continue;
                }
                InternAI? intern = InternManager.Instance.GetInternAI((int)internController.playerClientId);
                if (intern == null)
                {
                    continue;
                }

                if (!intern.AreHandsFree())
                {
                    // Intern drop item
                    intern.DropItemServerRpc();
                }
                else if (localPlayer.currentlyHeldObjectServer != null)
                {
                    // To cut Discard_performed from triggering after this input
                    localPlayer.isHoldingObject = false;

                    // Intern take item from player hands
                    GrabbableObject grabbableObject = localPlayer.currentlyHeldObjectServer;
                    intern.GiveItemToInternServerRpc(localPlayer.playerClientId, grabbableObject.NetworkObject);
                }

                return;
            }
        }

        private void GrabIntern_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;
            if (!IsPerformedValid(localPlayer))
            {
                return;
            }

            Ray interactRay = new Ray(localPlayer.gameplayCamera.transform.position, localPlayer.gameplayCamera.transform.forward);
            RaycastHit[] raycastHits = Physics.RaycastAll(interactRay, localPlayer.grabDistance, Const.PLAYER_MASK);
            foreach (RaycastHit hit in raycastHits)
            {
                if (hit.collider.tag != "Player")
                {
                    continue;
                }

                PlayerControllerB player = hit.collider.gameObject.GetComponent<PlayerControllerB>();
                if (player == null)
                {
                    continue;
                }
                InternAI? intern = InternManager.Instance.GetInternAI((int)player.playerClientId);
                if (intern == null)
                {
                    continue;
                }

                intern.SyncAssignTargetAndSetMovingTo(localPlayer);
                // Grab intern
                intern.GrabInternServerRpc(localPlayer.playerClientId);

                HUDManager.Instance.ClearControlTips();
                HUDManager.Instance.ChangeControlTipMultiple(new string[] { string.Format(Const.TOOLTIP_RELEASE_INTERNS, GetKeyAction(Plugin.InputActionsInstance.ReleaseInterns)) });
                return;
            }
        }

        private void ReleaseInterns_performed(InputAction.CallbackContext obj)
        {
            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;
            if (!IsPerformedValid(localPlayer))
            {
                return;
            }

            // No intern in interact range
            // Check if we hold interns
            InternAI[] internsAIsHoldByPlayer = InternManager.Instance.GetInternsAiHoldByPlayer((int)localPlayer.playerClientId);
            if (internsAIsHoldByPlayer.Length > 0)
            {
                for (int i = 0; i < internsAIsHoldByPlayer.Length; i++)
                {
                    internsAIsHoldByPlayer[i].SyncReleaseIntern(localPlayer);
                }
            }

            HUDManager.Instance.ClearControlTips();
            //HUDManager.Instance.ChangeControlTipMultiple(new string[] { Const.TOOLTIPS_ORDER_1 });
        }

        private void ChangeSuitIntern_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;
            if (!IsPerformedValid(localPlayer))
            {
                return;
            }

            // Use of change suit key to change suit of intern
            Ray interactRay = new Ray(localPlayer.gameplayCamera.transform.position, localPlayer.gameplayCamera.transform.forward);
            RaycastHit[] raycastHits = Physics.RaycastAll(interactRay, localPlayer.grabDistance, Const.PLAYER_MASK);
            foreach (RaycastHit hit in raycastHits)
            {
                if (hit.collider.tag != "Player")
                {
                    continue;
                }

                PlayerControllerB player = hit.collider.gameObject.GetComponent<PlayerControllerB>();
                if (player == null)
                {
                    continue;
                }
                InternAI? intern = InternManager.Instance.GetInternAI((int)player.playerClientId);
                if (intern == null)
                {
                    continue;
                }


                if (intern.NpcController.Npc.currentSuitID == localPlayer.currentSuitID)
                {
                    intern.ChangeSuitInternServerRpc(intern.NpcController.Npc.playerClientId, 0);
                }
                else
                {
                    intern.ChangeSuitInternServerRpc(intern.NpcController.Npc.playerClientId, localPlayer.currentSuitID);
                }

                return;
            }
        }
    }
}
