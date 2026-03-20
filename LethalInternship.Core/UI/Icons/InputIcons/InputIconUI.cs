using LethalInternship.SharedAbstractions.UI;
using UnityEngine;

namespace LethalInternship.Core.UI.Icons.InputIcons
{
    public class InputIconUI : IIconUI
    {
        public int Key => key;
        private int key;

        private GameObject iconGameObject;
        private RectTransform rectTransformCanvasOverlay;

        private InputIconUIController iconUIController;

        public InputIconUI(GameObject iconGameObject, IIconUIInfos iconUIInfos, RectTransform rectTransformCanvasOverlay)
        {
            this.iconGameObject = iconGameObject;
            this.key = iconUIInfos.GetUIKey();
            this.rectTransformCanvasOverlay = rectTransformCanvasOverlay;

            iconUIController = this.iconGameObject.GetComponentInChildren<InputIconUIController>();
            iconUIController.SetImageOnTop(iconUIInfos.IconImagesTypes);

            SetIconActive(false);
        }

        public void SetPositionUICenter()
        {
            iconUIController.PlaceOnCenterCanvas();
            SetIconActive(true);
        }

        public void SetIconActive(bool active)
        {
            iconGameObject.SetActive(active);
        }
    }
}
