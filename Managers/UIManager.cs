using LethalInternship.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Component = UnityEngine.Component;

namespace LethalInternship.Managers
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; } = null!;

        public GameObject CommandsUI = null!;
        public bool IsGroupCommandWheelOpened { get { return CommandsUI != null && CommandsUI.activeSelf; } }

        private void Awake()
        {
            Instance = this;
            Plugin.LogDebug("=============== awake UIManager =====================");
        }

        public void Init(Transform HUDContainerParent)
        {
            CommandsUI = GameObject.Instantiate(Plugin.CommandsUIPrefab, HUDContainerParent);
            //GroupCommandWheel.transform.SetAsLastSibling();
            //GroupCommandWheel.transform.SetSiblingIndex(0);
            CommandsUI.name = "CommandsUI";
            CommandsUI.SetActive(false);

            foreach (CommandButtonController commandButtonController in CommandsUI.GetComponentsInChildren<CommandButtonController>())
            {
                if (commandButtonController == null)
                {
                    continue;
                }

                Plugin.LogDebug($"commandButtonController id {commandButtonController.ID} event linkin");
                commandButtonController.OnSelected += CommandWheelButtonController_OnSelected;
            }
        }

        private void CommandWheelButtonController_OnSelected(object sender, EventArgs e)
        {
            Plugin.LogDebug($"=============== commandWheelButtonController? sender {((CommandButtonController)sender).ID}");
            Plugin.LogDebug($"=============== commandWheelButtonController? e {e}");
        }

        public void ToogleCommandWheel()
        {
            if (IsGroupCommandWheelOpened)
            {
                CommandsUI.SetActive(false);
                GameNetworkManager.Instance.localPlayerController.quickMenuManager.isMenuOpen = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                CommandsUI.SetActive(true);
                GameNetworkManager.Instance.localPlayerController.quickMenuManager.isMenuOpen = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            //Plugin.LogDebug($"pos {GameNetworkManager.Instance.localPlayerController.quickMenuManager.menuContainer.transform.position}, {GameNetworkManager.Instance.localPlayerController.quickMenuManager.menuContainer.transform.GetSiblingIndex()}");

            //Component[] components = GroupCommandWheel.GetComponentsInChildren(typeof(Component));
            //Plugin.LogDebug($"==================");
            //Plugin.LogDebug($"GroupCommandWheel component n {components.Length}");
            //foreach (Component component in components)
            //{
            //    if (component == null) continue;

            //    component.transform.SetAsLastSibling();
            //    Plugin.LogDebug($"pos {component.transform.position}, index {component.transform.GetSiblingIndex()}, active {component.gameObject.activeSelf} {component.ToString()}");
            //}

            //Plugin.LogDebug($"GroupCommandWheel {GroupCommandWheel.activeSelf}");
        }
    }
}
