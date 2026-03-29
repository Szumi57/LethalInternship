using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.Others
{
    public class UIVisibilityController : MonoBehaviour
    {
        public static UIVisibilityController Instance { get; private set; } = null!;

        public List<GameObject> ListInternUI = null!;
        public List<GameObject> SuitCommandsUI = null!;
        public List<GameObject> QuickCommandsUI = null!;
        public List<GameObject> PointerCommandsUI = null!;
        public List<GameObject> AutoDefenseCommandsUI = null!;
        public List<GameObject> CarryBehaviourCommandsUI = null!;
        public List<GameObject> GotoCommandsUI = null!;
        public List<GameObject> ScavengeCommandsUI = null!;

        public List<GameObject> OthersUI = null!;
        public List<Image> ImagesUI = null!;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void SetAllVisible()
        {
            SetGameObjectVisible(ListInternUI, visible: true);
            SetGameObjectVisible(SuitCommandsUI, visible: true);
            SetGameObjectVisible(QuickCommandsUI, visible: true);
            SetGameObjectVisible(PointerCommandsUI, visible: true);
            SetGameObjectVisible(AutoDefenseCommandsUI, visible: true);
            SetGameObjectVisible(CarryBehaviourCommandsUI, visible: true);
            SetGameObjectVisible(GotoCommandsUI, visible: true);
            SetGameObjectVisible(ScavengeCommandsUI, visible: true);

            SetGameObjectVisible(OthersUI, visible: true);
            SetImagesVisible(ImagesUI, visible: true);
        }

        public void SetOnlyListInternsAndSuitCommandsVisible()
        {
            SetGameObjectVisible(SuitCommandsUI, visible: true);
            SetGameObjectVisible(ListInternUI, visible: true);

            SetGameObjectVisible(QuickCommandsUI, visible: false);
            SetGameObjectVisible(PointerCommandsUI, visible: false);
            SetGameObjectVisible(AutoDefenseCommandsUI, visible: false);
            SetGameObjectVisible(CarryBehaviourCommandsUI, visible: false);
            SetGameObjectVisible(GotoCommandsUI, visible: false);
            SetGameObjectVisible(ScavengeCommandsUI, visible: false);

            SetGameObjectVisible(OthersUI, visible: false);
            SetImagesVisible(ImagesUI, visible: false);
        }

        public void SetOnlyListInternsVisible()
        {
            SetGameObjectVisible(ListInternUI, visible: true);

            SetGameObjectVisible(SuitCommandsUI, visible: false);
            SetGameObjectVisible(QuickCommandsUI, visible: false);
            SetGameObjectVisible(PointerCommandsUI, visible: false);
            SetGameObjectVisible(AutoDefenseCommandsUI, visible: false);
            SetGameObjectVisible(CarryBehaviourCommandsUI, visible: false);
            SetGameObjectVisible(GotoCommandsUI, visible: false);
            SetGameObjectVisible(ScavengeCommandsUI, visible: false);

            SetGameObjectVisible(OthersUI, visible: false);
            SetImagesVisible(ImagesUI, visible: false);
        }

        private void SetGameObjectVisible(List<GameObject> list, bool visible)
        {
            foreach (var go in list) go.SetActive(visible);
        }

        private void SetImagesVisible(List<Image> list, bool visible)
        {
            foreach (var img in list) img.enabled = visible;
        }
    }
}
